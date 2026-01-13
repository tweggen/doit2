using Microsoft.AspNetCore.Identity;

namespace DoitBlazor.Models;

public class ApplicationUser : IdentityUser<int>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Person> OwnedPersons { get; set; } = new List<Person>();
    public virtual ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public virtual UserConfig? Config { get; set; }
}
