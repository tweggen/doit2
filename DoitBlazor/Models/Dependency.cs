using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoitBlazor.Models;

[Table("todo_deps")]
public class Dependency
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int Relation { get; set; }
    
    [Required]
    [Column("demanding_id")]
    public int DemandingId { get; set; }
    
    [Required]
    [Column("required_id")]
    public int RequiredId { get; set; }
    
    [Column("inserted_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("DemandingId")]
    public virtual TodoItem? DemandingItem { get; set; }
    
    [ForeignKey("RequiredId")]
    public virtual TodoItem? RequiredItem { get; set; }
}
