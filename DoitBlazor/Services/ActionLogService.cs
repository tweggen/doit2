using System.Text.Json;
using DoitBlazor.Data;
using DoitBlazor.Models;
using Microsoft.EntityFrameworkCore;

namespace DoitBlazor.Services;

/// <summary>
/// Implementation of IActionLogService using Entity Framework Core
/// </summary>
public class ActionLogService : IActionLogService
{
    private readonly ApplicationDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Configuration
    private const int MaxUndoableActions = 100;
    private const int CompactionAgeDays = 7;

    public ActionLogService(ApplicationDbContext context)
    {
        _context = context;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<ActionLog> RecordAsync(
        int userId,
        string entityType,
        int entityId,
        string actionType,
        Dictionary<string, FieldChange> changes,
        string? description = null)
    {
        // Clear redo stack when recording a new action
        await ClearRedoStackAsync(userId, entityType, entityId);
        
        var action = new ActionLog
        {
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            ActionType = actionType,
            Changes = JsonSerializer.Serialize(changes, _jsonOptions),
            Timestamp = DateTime.UtcNow,
            Description = description ?? GenerateDescription(actionType, changes)
        };
        
        _context.ActionLogs.Add(action);
        await _context.SaveChangesAsync();
        
        // Enforce max actions limit
        await EnforceActionLimitAsync(userId, entityType, entityId);
        
        return action;
    }

    public async Task<ActionLog> RecordCreateAsync<T>(
        int userId, 
        string entityType, 
        int entityId, 
        T entity, 
        string? description = null)
    {
        var changes = new Dictionary<string, FieldChange>
        {
            ["_entity"] = new FieldChange(null, entity)
        };
        
        return await RecordAsync(
            userId, 
            entityType, 
            entityId, 
            ActionTypes.Create, 
            changes,
            description ?? $"Created {entityType.ToLower()}");
    }

    public async Task<ActionLog> RecordDeleteAsync<T>(
        int userId, 
        string entityType, 
        int entityId, 
        T entity, 
        string? description = null)
    {
        var changes = new Dictionary<string, FieldChange>
        {
            ["_entity"] = new FieldChange(entity, null)
        };
        
        return await RecordAsync(
            userId, 
            entityType, 
            entityId, 
            ActionTypes.Delete, 
            changes,
            description ?? $"Deleted {entityType.ToLower()}");
    }

    public async Task<ActionLog?> UndoAsync(int userId, string entityType, int entityId)
    {
        // Find the most recent active (not undone), non-compacted action
        var action = await _context.ActionLogs
            .Where(a => a.UserId == userId 
                     && a.EntityType == entityType 
                     && a.EntityId == entityId
                     && a.UndoneAt == null
                     && !a.IsCompacted)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();
        
        if (action == null)
            return null;
        
        // Mark as undone
        action.UndoneAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return action;
    }

    public async Task<ActionLog?> RedoAsync(int userId, string entityType, int entityId)
    {
        // Find the earliest undone action
        var action = await _context.ActionLogs
            .Where(a => a.UserId == userId 
                     && a.EntityType == entityType 
                     && a.EntityId == entityId
                     && a.UndoneAt != null
                     && !a.IsCompacted)
            .OrderBy(a => a.UndoneAt)
            .FirstOrDefaultAsync();
        
        if (action == null)
            return null;
        
        // Mark as active again
        action.UndoneAt = null;
        await _context.SaveChangesAsync();
        
        return action;
    }

    public async Task<bool> CanUndoAsync(int userId, string entityType, int entityId)
    {
        return await _context.ActionLogs
            .AnyAsync(a => a.UserId == userId 
                        && a.EntityType == entityType 
                        && a.EntityId == entityId
                        && a.UndoneAt == null
                        && !a.IsCompacted);
    }

    public async Task<bool> CanRedoAsync(int userId, string entityType, int entityId)
    {
        return await _context.ActionLogs
            .AnyAsync(a => a.UserId == userId 
                        && a.EntityType == entityType 
                        && a.EntityId == entityId
                        && a.UndoneAt != null
                        && !a.IsCompacted);
    }

    public async Task<List<ActionLog>> GetHistoryAsync(
        string entityType, 
        int entityId, 
        bool includeUndone = true,
        int limit = 100)
    {
        var query = _context.ActionLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId);
        
        if (!includeUndone)
        {
            query = query.Where(a => a.UndoneAt == null);
        }
        
        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task ClearRedoStackAsync(int userId, string entityType, int entityId)
    {
        // Delete all undone actions (they become invalid when a new action is recorded)
        var undoneActions = await _context.ActionLogs
            .Where(a => a.UserId == userId 
                     && a.EntityType == entityType 
                     && a.EntityId == entityId
                     && a.UndoneAt != null)
            .ToListAsync();
        
        if (undoneActions.Any())
        {
            _context.ActionLogs.RemoveRange(undoneActions);
            await _context.SaveChangesAsync();
        }
    }

    public async Task CompactOldActionsAsync(int userId)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-CompactionAgeDays);
        
        // Get all entities with old, non-compacted actions for this user
        var entitiesWithOldActions = await _context.ActionLogs
            .Where(a => a.UserId == userId 
                     && a.Timestamp < cutoffDate 
                     && !a.IsCompacted
                     && a.UndoneAt == null)
            .Select(a => new { a.EntityType, a.EntityId })
            .Distinct()
            .ToListAsync();
        
        foreach (var entity in entitiesWithOldActions)
        {
            await CompactEntityActionsAsync(userId, entity.EntityType, entity.EntityId, cutoffDate);
        }
    }

    public async Task<UndoRedoState> GetUndoRedoStateAsync(int userId, string entityType, int entityId)
    {
        var undoAction = await _context.ActionLogs
            .Where(a => a.UserId == userId 
                     && a.EntityType == entityType 
                     && a.EntityId == entityId
                     && a.UndoneAt == null
                     && !a.IsCompacted)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();
        
        var redoAction = await _context.ActionLogs
            .Where(a => a.UserId == userId 
                     && a.EntityType == entityType 
                     && a.EntityId == entityId
                     && a.UndoneAt != null
                     && !a.IsCompacted)
            .OrderBy(a => a.UndoneAt)
            .FirstOrDefaultAsync();
        
        var undoCount = await _context.ActionLogs
            .CountAsync(a => a.UserId == userId 
                          && a.EntityType == entityType 
                          && a.EntityId == entityId
                          && a.UndoneAt == null
                          && !a.IsCompacted);
        
        var redoCount = await _context.ActionLogs
            .CountAsync(a => a.UserId == userId 
                          && a.EntityType == entityType 
                          && a.EntityId == entityId
                          && a.UndoneAt != null
                          && !a.IsCompacted);
        
        return new UndoRedoState
        {
            CanUndo = undoAction != null,
            CanRedo = redoAction != null,
            UndoDescription = undoAction?.Description,
            RedoDescription = redoAction?.Description,
            UndoCount = undoCount,
            RedoCount = redoCount
        };
    }

    #region Private Helper Methods

    private async Task EnforceActionLimitAsync(int userId, string entityType, int entityId)
    {
        var activeActionCount = await _context.ActionLogs
            .CountAsync(a => a.UserId == userId 
                          && a.EntityType == entityType 
                          && a.EntityId == entityId
                          && a.UndoneAt == null
                          && !a.IsCompacted);
        
        if (activeActionCount > MaxUndoableActions)
        {
            // Compact the oldest actions to get under the limit
            var actionsToCompact = activeActionCount - MaxUndoableActions;
            var oldestActions = await _context.ActionLogs
                .Where(a => a.UserId == userId 
                         && a.EntityType == entityType 
                         && a.EntityId == entityId
                         && a.UndoneAt == null
                         && !a.IsCompacted)
                .OrderBy(a => a.Timestamp)
                .Take(actionsToCompact)
                .ToListAsync();
            
            // Mark them as compacted (they become read-only history)
            foreach (var action in oldestActions)
            {
                action.IsCompacted = true;
            }
            
            await _context.SaveChangesAsync();
        }
    }

    private async Task CompactEntityActionsAsync(
        int userId, 
        string entityType, 
        int entityId, 
        DateTime cutoffDate)
    {
        // Get old actions grouped by day
        var oldActions = await _context.ActionLogs
            .Where(a => a.UserId == userId 
                     && a.EntityType == entityType 
                     && a.EntityId == entityId
                     && a.Timestamp < cutoffDate
                     && !a.IsCompacted
                     && a.UndoneAt == null)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();
        
        if (!oldActions.Any())
            return;
        
        // Group by day
        var actionsByDay = oldActions
            .GroupBy(a => a.Timestamp.Date)
            .Where(g => g.Count() > 1) // Only compact days with multiple actions
            .ToList();
        
        foreach (var dayGroup in actionsByDay)
        {
            var actionsInDay = dayGroup.OrderBy(a => a.Timestamp).ToList();
            
            // Merge all changes for the day
            var mergedChanges = MergeChanges(actionsInDay);
            
            // Create compacted action
            var compactedAction = new ActionLog
            {
                UserId = userId,
                EntityType = entityType,
                EntityId = entityId,
                ActionType = ActionTypes.Update,
                Changes = JsonSerializer.Serialize(mergedChanges, _jsonOptions),
                Timestamp = dayGroup.Key.AddHours(23).AddMinutes(59), // End of day
                IsCompacted = true,
                Description = $"Changes on {dayGroup.Key:MMM dd, yyyy} ({actionsInDay.Count} edits)"
            };
            
            // Remove old actions
            _context.ActionLogs.RemoveRange(actionsInDay);
            
            // Add compacted action
            _context.ActionLogs.Add(compactedAction);
        }
        
        // Mark remaining single-action days as compacted
        var singleActionDays = oldActions
            .GroupBy(a => a.Timestamp.Date)
            .Where(g => g.Count() == 1)
            .SelectMany(g => g)
            .ToList();
        
        foreach (var action in singleActionDays)
        {
            action.IsCompacted = true;
        }
        
        await _context.SaveChangesAsync();
    }

    private Dictionary<string, FieldChange> MergeChanges(List<ActionLog> actions)
    {
        var merged = new Dictionary<string, FieldChange>();
        
        foreach (var action in actions)
        {
            var changes = JsonSerializer.Deserialize<Dictionary<string, FieldChange>>(
                action.Changes, _jsonOptions);
            
            if (changes == null) continue;
            
            foreach (var (field, change) in changes)
            {
                if (merged.ContainsKey(field))
                {
                    // Keep the original "old" value (from first action)
                    // "new" is only populated at undo time, so we don't need to track it during compaction
                }
                else
                {
                    merged[field] = new FieldChange(change.Old);
                }
            }
        }
        
        return merged;
    }

    private string GenerateDescription(string actionType, Dictionary<string, FieldChange> changes)
    {
        if (actionType == ActionTypes.Create)
            return "Created";
        
        if (actionType == ActionTypes.Delete)
            return "Deleted";
        
        // For updates, list the changed fields
        var fieldNames = changes.Keys
            .Where(k => !k.StartsWith("_"))
            .Select(k => FormatFieldName(k))
            .ToList();
        
        if (fieldNames.Count == 0)
            return "Updated";
        
        if (fieldNames.Count == 1)
            return $"Changed {fieldNames[0]}";
        
        if (fieldNames.Count == 2)
            return $"Changed {fieldNames[0]} and {fieldNames[1]}";
        
        return $"Changed {fieldNames[0]}, {fieldNames[1]} and {fieldNames.Count - 2} more";
    }

    private string FormatFieldName(string fieldName)
    {
        // Convert PascalCase to lowercase with spaces
        return string.Concat(fieldName.Select((c, i) => 
            i > 0 && char.IsUpper(c) ? " " + char.ToLower(c) : char.ToLower(c).ToString()));
    }

    #endregion
}
