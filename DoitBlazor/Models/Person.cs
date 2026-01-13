using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoitBlazor.Models;

[Table("todo_persons")]
public class Person
{
    [Key]
    public int Id { get; set; }
    
    public int Status { get; set; }
    
    [MaxLength(160)]
    public string? Email { get; set; }
    
    [Required]
    [MaxLength(160)]
    public string FamilyName { get; set; } = string.Empty;
    
    [MaxLength(160)]
    public string? GivenName { get; set; }
    
    [Required]
    [Column("owning_user_id")]
    public int OwningUserId { get; set; }
    
    [Column("user_id")]
    public int? UserId { get; set; }
    
    [Column("inserted_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("OwningUserId")]
    public virtual ApplicationUser? OwningUser { get; set; }
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    public virtual ICollection<TodoItem> AuthoredItems { get; set; } = new List<TodoItem>();
    public virtual ICollection<TodoItem> ContactedItems { get; set; } = new List<TodoItem>();
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
}
