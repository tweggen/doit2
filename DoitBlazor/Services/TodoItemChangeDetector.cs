using DoitBlazor.Models;

namespace DoitBlazor.Services;

/// <summary>
/// Helper class for detecting changes between TodoItem states
/// </summary>
public static class TodoItemChangeDetector
{
    /// <summary>
    /// Compare two TodoItems and return a dictionary of field changes
    /// </summary>
    public static Dictionary<string, FieldChange> DetectChanges(TodoItem original, TodoItem updated)
    {
        var changes = new Dictionary<string, FieldChange>();
        
        if (original.Caption != updated.Caption)
        {
            changes["Caption"] = new FieldChange(original.Caption, updated.Caption);
        }
        
        if (original.Content != updated.Content)
        {
            changes["Content"] = new FieldChange(original.Content, updated.Content);
        }
        
        if (original.Due != updated.Due)
        {
            changes["Due"] = new FieldChange(
                original.Due?.ToString("yyyy-MM-dd"), 
                updated.Due?.ToString("yyyy-MM-dd"));
        }
        
        if (original.Status != updated.Status)
        {
            changes["Status"] = new FieldChange(original.Status, updated.Status);
        }
        
        if (original.ContactId != updated.ContactId)
        {
            changes["ContactId"] = new FieldChange(original.ContactId, updated.ContactId);
        }
        
        if (original.AuthorId != updated.AuthorId)
        {
            changes["AuthorId"] = new FieldChange(original.AuthorId, updated.AuthorId);
        }
        
        return changes;
    }
    
    /// <summary>
    /// Apply changes from an action log entry to a TodoItem (for undo - applies old values)
    /// </summary>
    public static void ApplyUndo(TodoItem item, Dictionary<string, FieldChange> changes)
    {
        foreach (var (field, change) in changes)
        {
            ApplyFieldValue(item, field, change.Old);
        }
    }
    
    /// <summary>
    /// Apply changes from an action log entry to a TodoItem (for redo - applies new values)
    /// </summary>
    public static void ApplyRedo(TodoItem item, Dictionary<string, FieldChange> changes)
    {
        foreach (var (field, change) in changes)
        {
            ApplyFieldValue(item, field, change.New);
        }
    }
    
    private static void ApplyFieldValue(TodoItem item, string field, object? value)
    {
        switch (field)
        {
            case "Caption":
                item.Caption = value?.ToString() ?? "";
                break;
                
            case "Content":
                item.Content = value?.ToString();
                break;
                
            case "Due":
                if (value == null || string.IsNullOrEmpty(value.ToString()))
                {
                    item.Due = null;
                }
                else if (DateOnly.TryParse(value.ToString(), out var date))
                {
                    item.Due = date;
                }
                break;
                
            case "Status":
                if (value != null && int.TryParse(value.ToString(), out var status))
                {
                    item.Status = status;
                }
                break;
                
            case "ContactId":
                if (value != null && int.TryParse(value.ToString(), out var contactId))
                {
                    item.ContactId = contactId;
                }
                break;
                
            case "AuthorId":
                if (value != null && int.TryParse(value.ToString(), out var authorId))
                {
                    item.AuthorId = authorId;
                }
                break;
        }
    }
    
    /// <summary>
    /// Generate a human-readable description of status change
    /// </summary>
    public static string GetStatusDescription(int oldStatus, int newStatus)
    {
        var oldName = oldStatus == 0 ? "active" : "completed";
        var newName = newStatus == 0 ? "active" : "completed";
        return $"Changed status from {oldName} to {newName}";
    }
}
