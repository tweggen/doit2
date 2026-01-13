using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace DoitBlazor.Models;

[Table("todo_configs")]
public class UserConfig
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("properties", TypeName = "jsonb")]
    public string? PropertiesJson { get; set; }
    
    [Column("inserted_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    // Helper property to work with properties as a dictionary
    [NotMapped]
    public Dictionary<string, object>? Properties
    {
        get => string.IsNullOrEmpty(PropertiesJson) 
            ? null 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(PropertiesJson);
        set => PropertiesJson = value == null 
            ? null 
            : JsonSerializer.Serialize(value);
    }
}
