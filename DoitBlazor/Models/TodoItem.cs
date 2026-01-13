using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoitBlazor.Models;

[Table("todo_items")]
public class TodoItem
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }
    
    public int Status { get; set; }
    
    public DateOnly? Due { get; set; }
    
    [Required]
    [MaxLength(160)]
    public string Caption { get; set; } = string.Empty;
    
    [MaxLength(2030)]
    public string? Content { get; set; }
    
    [Required]
    [Column("author_id")]
    public int AuthorId { get; set; }
    
    [Required]
    [Column("contact_id")]
    public int ContactId { get; set; }
    
    [Column("inserted_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    [ForeignKey("AuthorId")]
    public virtual Person? Author { get; set; }
    
    [ForeignKey("ContactId")]
    public virtual Person? Contact { get; set; }
    
    public virtual ICollection<Dependency> DemandingFromDeps { get; set; } = new List<Dependency>();
    public virtual ICollection<Dependency> RequiredByDeps { get; set; } = new List<Dependency>();
    
    // Helper method for due state (ported from Elixir)
    public int GetDueState()
    {
        if (Status != 0 || Due == null)
            return 0;
            
        var today = DateOnly.FromDateTime(DateTime.Now);
        var comparison = Due.Value.CompareTo(today);
        
        if (comparison < 0)
        {
            // Past due
            return Due.Value.Day != today.Day ? 2 : 1;
        }
        else
        {
            // Future or today
            return Due.Value.Day != today.Day ? 0 : 1;
        }
    }
}
