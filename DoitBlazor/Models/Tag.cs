using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoitBlazor.Models;

[Table("todo_tags")]
public class Tag
{
    [Key]
    public int Id { get; set; }
    
    public int Status { get; set; }
    
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Required]
    public string TagName { get; set; } = string.Empty;
    
    [Column("inserted_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
}
