using Microsoft.EntityFrameworkCore;
using DoitBlazor.Data;
using DoitBlazor.Models;

namespace DoitBlazor.Services;

public class TodoService : ITodoService
{
    private readonly ApplicationDbContext _context;
    
    public TodoService(ApplicationDbContext context)
    {
        _context = context;
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
        return item;
    }
    
    public async Task<TodoItem> UpdateItemAsync(TodoItem item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.TodoItems.Update(item);
        await _context.SaveChangesAsync();
        return item;
    }
    
    public async Task DeleteItemAsync(int id)
    {
        var item = await _context.TodoItems.FindAsync(id);
        if (item != null)
        {
            _context.TodoItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<TodoItem> ToggleItemStatusAsync(int id)
    {
        var item = await _context.TodoItems.FindAsync(id);
        if (item != null)
        {
            item.Status = item.Status == 0 ? 1 : 0;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return item!;
    }
    
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
