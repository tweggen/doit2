using DoitBlazor.Models;

namespace DoitBlazor.Services;

/// <summary>
/// Service for recording, retrieving, and managing action history.
/// Supports undo/redo operations and history compaction.
/// </summary>
public interface IActionLogService
{
    /// <summary>
    /// Record a new action. This should be called after the change has been applied
    /// to the current state tables.
    /// </summary>
    /// <param name="userId">User performing the action</param>
    /// <param name="entityType">Type of entity (use EntityTypes constants)</param>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="actionType">Type of action (use ActionTypes constants)</param>
    /// <param name="changes">Dictionary of field changes</param>
    /// <param name="description">Optional human-readable description</param>
    /// <returns>The created ActionLog entry</returns>
    Task<ActionLog> RecordAsync(
        int userId,
        string entityType,
        int entityId,
        string actionType,
        Dictionary<string, FieldChange> changes,
        string? description = null);
    
    /// <summary>
    /// Record a Create action with the full entity state
    /// </summary>
    Task<ActionLog> RecordCreateAsync<T>(int userId, string entityType, int entityId, T entity, string? description = null);
    
    /// <summary>
    /// Record a Delete action with the full entity state (for restore on undo)
    /// </summary>
    Task<ActionLog> RecordDeleteAsync<T>(int userId, string entityType, int entityId, T entity, string? description = null);
    
    /// <summary>
    /// Undo the most recent undoable action for a specific entity.
    /// Returns the action that was undone, or null if nothing to undo.
    /// Note: This only marks the action as undone - the caller must apply the reverse change.
    /// </summary>
    Task<ActionLog?> UndoAsync(int userId, string entityType, int entityId);
    
    /// <summary>
    /// Redo the most recently undone action for a specific entity.
    /// Returns the action that was redone, or null if nothing to redo.
    /// Note: This only marks the action as active - the caller must apply the change.
    /// </summary>
    Task<ActionLog?> RedoAsync(int userId, string entityType, int entityId);
    
    /// <summary>
    /// Check if undo is available for an entity
    /// </summary>
    Task<bool> CanUndoAsync(int userId, string entityType, int entityId);
    
    /// <summary>
    /// Check if redo is available for an entity
    /// </summary>
    Task<bool> CanRedoAsync(int userId, string entityType, int entityId);
    
    /// <summary>
    /// Get the action history for an entity, ordered by most recent first.
    /// Includes both active and undone actions.
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="includeUndone">Whether to include undone actions</param>
    /// <param name="limit">Maximum number of actions to return</param>
    Task<List<ActionLog>> GetHistoryAsync(
        string entityType, 
        int entityId, 
        bool includeUndone = true,
        int limit = 100);
    
    /// <summary>
    /// Clear the redo stack for an entity (called when a new action is recorded)
    /// </summary>
    Task ClearRedoStackAsync(int userId, string entityType, int entityId);
    
    /// <summary>
    /// Compact old actions (older than 7 days) into daily summaries.
    /// Also enforces the 100-action limit per entity.
    /// Should be called periodically (e.g., daily background job or on-demand).
    /// </summary>
    Task CompactOldActionsAsync(int userId);
    
    /// <summary>
    /// Get a summary of available undo/redo state for an entity
    /// </summary>
    Task<UndoRedoState> GetUndoRedoStateAsync(int userId, string entityType, int entityId);
}

/// <summary>
/// Represents a single field change. 
/// "Old" is always stored at record time.
/// "New" is captured at undo time (for redo support).
/// </summary>
public class FieldChange
{
    public object? Old { get; set; }
    public object? New { get; set; }
    
    public FieldChange() { }
    
    public FieldChange(object? oldValue)
    {
        Old = oldValue;
        New = null; // Captured later at undo time
    }
    
    public FieldChange(object? oldValue, object? newValue)
    {
        Old = oldValue;
        New = newValue;
    }
}

/// <summary>
/// Summary of undo/redo availability for an entity
/// </summary>
public class UndoRedoState
{
    public bool CanUndo { get; set; }
    public bool CanRedo { get; set; }
    public string? UndoDescription { get; set; }
    public string? RedoDescription { get; set; }
    public int UndoCount { get; set; }
    public int RedoCount { get; set; }
}
