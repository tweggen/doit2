using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DoitBlazor.Data;
using DoitBlazor.Models;

namespace DoitBlazor.Services;

public class TodoService : ITodoService
{
    private readonly ApplicationDbContext _context;
    private readonly IActionLogService _actionLogService;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public TodoService(ApplicationDbContext context, IActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    // TodoItem operations
    public async Task<List<TodoItem>> GetUserItemsAsync(int userId)
    {
        return await _context.TodoItems
            .Include(t => t.Author)
            .Include(t => t.Contact)
            .Include(t => t.DemandingFromDeps)
            .Include(t => t.RequiredByDeps)
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Due)
            .ThenBy(t => t.Status)
            .ToListAsync();
    }
    
    public async Task<TodoItem?> GetItemAsync(int id)
    {
        return await _context.TodoItems
            .Include(t => t.Author)
            .Include(t => t.Contact)
            .Include(t => t.DemandingFromDeps)
            .Include(t => t.RequiredByDeps)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
    
    public async Task<TodoItem> CreateItemAsync(TodoItem item)
    {
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        _context.TodoItems.Add(item);
        await _context.SaveChangesAsync();
        
        // Record the create action
        await _actionLogService.RecordCreateAsync(
            item.UserId,
            EntityTypes.TodoItem,
            item.Id,
            CreateItemSnapshot(item),
            $"Created \"{item.Caption}\"");
        
        return item;
    }
    
    public async Task<TodoItem> UpdateItemAsync(TodoItem item)
    {
        // Get the existing item (tracked) to detect changes and update
        var existingItem = await _context.TodoItems.FindAsync(item.Id);
        
        if (existingItem == null)
            throw new InvalidOperationException($"TodoItem with Id {item.Id} not found");
        
        // Detect changes before applying updates
        var changes = TodoItemChangeDetector.DetectChanges(existingItem, item);
        
        // Apply the updates to the tracked entity
        existingItem.Caption = item.Caption;
        existingItem.Content = item.Content;
        existingItem.Due = item.Due;
        existingItem.Status = item.Status;
        existingItem.ContactId = item.ContactId;
        existingItem.AuthorId = item.AuthorId;
        existingItem.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        // Record the action only if there were changes
        if (changes.Any())
        {
            await _actionLogService.RecordAsync(
                existingItem.UserId,
                EntityTypes.TodoItem,
                existingItem.Id,
                ActionTypes.Update,
                changes);
        }
        
        return existingItem;
    }
    
    public async Task DeleteItemAsync(int id)
    {
        var item = await _context.TodoItems.FindAsync(id);
        if (item != null)
        {
            // Record the delete action before removing
            await _actionLogService.RecordDeleteAsync(
                item.UserId,
                EntityTypes.TodoItem,
                item.Id,
                CreateItemSnapshot(item),
                $"Deleted \"{item.Caption}\"");
            
            _context.TodoItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<TodoItem> ToggleItemStatusAsync(int id)
    {
        var item = await _context.TodoItems.FindAsync(id);
        if (item != null)
        {
            var oldStatus = item.Status;
            var newStatus = item.Status == 0 ? 1 : 0;
            
            item.Status = newStatus;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            // Record the status change (only old value)
            var changes = new Dictionary<string, FieldChange>
            {
                ["Status"] = new FieldChange(oldStatus)
            };
            
            var description = newStatus == 1 
                ? $"Completed \"{item.Caption}\"" 
                : $"Reopened \"{item.Caption}\"";
            
            await _actionLogService.RecordAsync(
                item.UserId,
                EntityTypes.TodoItem,
                item.Id,
                ActionTypes.Update,
                changes,
                description);
        }
        return item!;
    }
    
    // TodoItem undo/redo operations
    public async Task<bool> UndoItemAsync(int userId, int itemId)
    {
        var action = await _actionLogService.UndoAsync(userId, EntityTypes.TodoItem, itemId);
        if (action == null)
            return false;
        
        var item = await _context.TodoItems.FindAsync(itemId);
        if (item == null)
        {
            // Item was deleted - we need to recreate it
            if (action.ActionType == ActionTypes.Delete)
            {
                var snapshot = DeserializeChanges(action.Changes);
                if (snapshot.TryGetValue("_entity", out var entityChange) && entityChange.Old != null)
                {
                    var restoredItem = DeserializeItemSnapshot(entityChange.Old);
                    if (restoredItem != null)
                    {
                        restoredItem.Id = itemId; // Keep the same ID
                        _context.TodoItems.Add(restoredItem);
                        await _context.SaveChangesAsync();
                        return true;
                    }
                }
            }
            return false;
        }
        
        // Capture current state as "new" before applying undo (for redo support)
        var changes = DeserializeChanges(action.Changes);
        CaptureCurrentStateAsNew(item, changes);
        
        // Update the action with captured "new" values
        action.Changes = JsonSerializer.Serialize(changes, _jsonOptions);
        await _context.SaveChangesAsync();
        
        // Apply the undo (restore old values)
        TodoItemChangeDetector.ApplyUndo(item, changes);
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<bool> RedoItemAsync(int userId, int itemId)
    {
        var action = await _actionLogService.RedoAsync(userId, EntityTypes.TodoItem, itemId);
        if (action == null)
            return false;
        
        var item = await _context.TodoItems.FindAsync(itemId);
        
        // Handle redo of delete
        if (action.ActionType == ActionTypes.Delete && item != null)
        {
            _context.TodoItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
        
        // Handle redo of create
        if (action.ActionType == ActionTypes.Create)
        {
            var snapshot = DeserializeChanges(action.Changes);
            if (snapshot.TryGetValue("_entity", out var entityChange) && entityChange.New != null)
            {
                var newItem = DeserializeItemSnapshot(entityChange.New);
                if (newItem != null)
                {
                    newItem.Id = itemId;
                    _context.TodoItems.Add(newItem);
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            return false;
        }
        
        if (item == null)
            return false;
        
        // Apply the redo (reapply the changes)
        var changes = DeserializeChanges(action.Changes);
        TodoItemChangeDetector.ApplyRedo(item, changes);
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<UndoRedoState> GetItemUndoRedoStateAsync(int userId, int itemId)
    {
        return await _actionLogService.GetUndoRedoStateAsync(userId, EntityTypes.TodoItem, itemId);
    }
    
    public async Task<List<ActionLog>> GetItemHistoryAsync(int itemId, int limit = 50)
    {
        return await _actionLogService.GetHistoryAsync(EntityTypes.TodoItem, itemId, includeUndone: false, limit: limit);
    }
    
    #region Private Helper Methods
    
    /// <summary>
    /// Captures the current item state as "new" values in the changes dictionary.
    /// Called at undo time to enable redo.
    /// </summary>
    private void CaptureCurrentStateAsNew(TodoItem item, Dictionary<string, FieldChange> changes)
    {
        foreach (var field in changes.Keys.ToList())
        {
            changes[field].New = field switch
            {
                "Caption" => item.Caption,
                "Content" => item.Content,
                "Due" => item.Due?.ToString("yyyy-MM-dd"),
                "Status" => item.Status,
                "ContactId" => item.ContactId,
                "AuthorId" => item.AuthorId,
                _ => null
            };
        }
    }
    
    private object CreateItemSnapshot(TodoItem item)
    {
        return new
        {
            item.Caption,
            item.Content,
            Due = item.Due?.ToString("yyyy-MM-dd"),
            item.Status,
            item.ContactId,
            item.AuthorId,
            item.UserId
        };
    }
    
    private Dictionary<string, FieldChange> DeserializeChanges(string changesJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, FieldChange>>(changesJson, _jsonOptions) 
                   ?? new Dictionary<string, FieldChange>();
        }
        catch
        {
            return new Dictionary<string, FieldChange>();
        }
    }
    
    private TodoItem? DeserializeItemSnapshot(object? snapshot)
    {
        if (snapshot == null) return null;
        
        try
        {
            var json = snapshot is JsonElement element 
                ? element.GetRawText() 
                : JsonSerializer.Serialize(snapshot, _jsonOptions);
            
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions);
            if (data == null) return null;
            
            var item = new TodoItem
            {
                Caption = data.TryGetValue("caption", out var caption) ? caption.GetString() ?? "" : "",
                Content = data.TryGetValue("content", out var content) ? content.GetString() : null,
                Status = data.TryGetValue("status", out var status) ? status.GetInt32() : 0,
                ContactId = data.TryGetValue("contactId", out var contactId) ? contactId.GetInt32() : 0,
                AuthorId = data.TryGetValue("authorId", out var authorId) ? authorId.GetInt32() : 0,
                UserId = data.TryGetValue("userId", out var userId) ? userId.GetInt32() : 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            if (data.TryGetValue("due", out var due) && due.ValueKind == JsonValueKind.String)
            {
                var dueStr = due.GetString();
                if (!string.IsNullOrEmpty(dueStr) && DateOnly.TryParse(dueStr, out var dueDate))
                {
                    item.Due = dueDate;
                }
            }
            
            return item;
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    // Person operations
    public async Task<List<Person>> GetUserPersonsAsync(int userId)
    {
        return await _context.Persons
            .Where(p => p.OwningUserId == userId)
            .OrderBy(p => p.FamilyName)
            .ThenBy(p => p.GivenName)
            .ToListAsync();
    }
    
    public async Task<Person?> GetPersonAsync(int id)
    {
        return await _context.Persons.FindAsync(id);
    }
    
    public async Task<Person?> FindPersonForUserAsync(int userId)
    {
        return await _context.Persons
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }
    
    public async Task<Person> EnsureUserHasPersonAsync(int userId, string email, string? userName)
    {
        // Check if user already has a Person record
        var existingPerson = await FindPersonForUserAsync(userId);
        if (existingPerson != null)
        {
            return existingPerson;
        }
        
        // Extract name from email or use userName
        var emailParts = email.Split('@');
        var namePart = userName ?? emailParts[0];
        
        // Create new Person for this user
        var newPerson = new Person
        {
            UserId = userId,
            OwningUserId = userId,
            Email = email,
            FamilyName = namePart, // Use email username or userName as family name
            GivenName = "",
            Status = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Persons.Add(newPerson);
        await _context.SaveChangesAsync();
        
        return newPerson;
    }
    
    public async Task<Person> CreatePersonAsync(Person person)
    {
        person.CreatedAt = DateTime.UtcNow;
        person.UpdatedAt = DateTime.UtcNow;
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }
    
    public async Task<Person> UpdatePersonAsync(Person person)
    {
        person.UpdatedAt = DateTime.UtcNow;
        _context.Persons.Update(person);
        await _context.SaveChangesAsync();
        return person;
    }
    
    public async Task DeletePersonAsync(int id)
    {
        var person = await _context.Persons.FindAsync(id);
        if (person != null)
        {
            // Prevent deletion of a person that is linked to a user account
            if (person.UserId.HasValue)
            {
                throw new InvalidOperationException("Cannot delete a contact that is linked to a user account.");
            }
            
            _context.Persons.Remove(person);
            await _context.SaveChangesAsync();
        }
    }
    
    // Dependency operations
    public async Task<Dependency> CreateDependencyAsync(Dependency dependency)
    {
        dependency.CreatedAt = DateTime.UtcNow;
        dependency.UpdatedAt = DateTime.UtcNow;
        _context.Dependencies.Add(dependency);
        await _context.SaveChangesAsync();
        return dependency;
    }
    
    public async Task DeleteDependencyAsync(int id)
    {
        var dependency = await _context.Dependencies.FindAsync(id);
        if (dependency != null)
        {
            _context.Dependencies.Remove(dependency);
            await _context.SaveChangesAsync();
        }
    }
    
    // Note operations
    public async Task<List<Note>> GetUserNotesAsync(int userId)
    {
        return await _context.Notes
            .Include(n => n.Person)
            .Include(n => n.Tag)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<Note> CreateNoteAsync(Note note)
    {
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }
    
    public async Task<Note> UpdateNoteAsync(Note note)
    {
        note.UpdatedAt = DateTime.UtcNow;
        _context.Notes.Update(note);
        await _context.SaveChangesAsync();
        return note;
    }
    
    public async Task DeleteNoteAsync(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note != null)
        {
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
        }
    }
    
    // Tag operations
    public async Task<List<Tag>> GetUserTagsAsync(int userId)
    {
        return await _context.Tags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.TagName)
            .ToListAsync();
    }
    
    public async Task<Tag> CreateTagAsync(Tag tag)
    {
        tag.CreatedAt = DateTime.UtcNow;
        tag.UpdatedAt = DateTime.UtcNow;
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }
    
    public async Task DeleteTagAsync(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }
    
    // Config operations
    public async Task<UserConfig?> GetUserConfigAsync(int userId)
    {
        return await _context.UserConfigs
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }
    
    public async Task<UserConfig> UpdateUserConfigAsync(UserConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        
        var existing = await _context.UserConfigs
            .FirstOrDefaultAsync(c => c.UserId == config.UserId);
            
        if (existing == null)
        {
            config.CreatedAt = DateTime.UtcNow;
            _context.UserConfigs.Add(config);
        }
        else
        {
            existing.PropertiesJson = config.PropertiesJson;
            existing.UpdatedAt = config.UpdatedAt;
        }
        
        await _context.SaveChangesAsync();
        return config;
    }
}
