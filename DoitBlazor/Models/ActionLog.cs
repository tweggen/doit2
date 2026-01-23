using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoitBlazor.Models;

/// <summary>
/// Records all changes to entities for undo/redo and history tracking.
/// Each action represents a single user operation that may affect multiple fields.
/// </summary>
[Table("action_logs")]
public class ActionLog
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The user who performed this action
    /// </summary>
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }
    
    /// <summary>
    /// Type of entity affected: "TodoItem", "Person", etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("entity_type")]
    public string EntityType { get; set; } = "";
    
    /// <summary>
    /// ID of the affected entity
    /// </summary>
    [Required]
    [Column("entity_id")]
    public int EntityId { get; set; }
    
    /// <summary>
    /// Type of action: "Create", "Update", "Delete"
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("action_type")]
    public string ActionType { get; set; } = "";
    
    /// <summary>
    /// JSON object containing the changes.
    /// Format: { "FieldName": { "old": oldValue, "new": newValue }, ... }
    /// For Create: { "_entity": { full object } }
    /// For Delete: { "_entity": { full object } }
    /// </summary>
    [Required]
    [Column("changes")]
    public string Changes { get; set; } = "{}";
    
    /// <summary>
    /// When this action was performed
    /// </summary>
    [Required]
    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// If true, this action is part of a daily summary and cannot be undone individually
    /// </summary>
    [Column("is_compacted")]
    public bool IsCompacted { get; set; } = false;
    
    /// <summary>
    /// If set, this action has been undone. The value is when it was undone.
    /// Null means the action is currently active.
    /// </summary>
    [Column("undone_at")]
    public DateTime? UndoneAt { get; set; }
    
    /// <summary>
    /// Optional description for display in history
    /// </summary>
    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }
    
    // Navigation property
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
}

/// <summary>
/// Constants for ActionType values
/// </summary>
public static class ActionTypes
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
}

/// <summary>
/// Constants for EntityType values
/// </summary>
public static class EntityTypes
{
    public const string TodoItem = "TodoItem";
    public const string Person = "Person";
}
