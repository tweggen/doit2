using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoitBlazor.Models;

[Table("todo_notes")]
public class Note
{
    [Key]
    public int Id { get; set; }
    
    public int Status { get; set; }
    
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("person_id")]
    public int? PersonId { get; set; }
    
    [Required]
    [Column("tag_id")]
    public int TagId { get; set; }
    
    public string? Content { get; set; }
    
    [Column("inserted_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    [ForeignKey("PersonId")]
    public virtual Person? Person { get; set; }
    
    [ForeignKey("TagId")]
    public virtual Tag? Tag { get; set; }
}
