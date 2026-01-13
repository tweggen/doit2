using DoitBlazor.Models;

namespace DoitBlazor.Services;

public interface ITodoService
{
    // TodoItem operations
    Task<List<TodoItem>> GetUserItemsAsync(int userId);
    Task<TodoItem?> GetItemAsync(int id);
    Task<TodoItem> CreateItemAsync(TodoItem item);
    Task<TodoItem> UpdateItemAsync(TodoItem item);
    Task DeleteItemAsync(int id);
    Task<TodoItem> ToggleItemStatusAsync(int id);
    
    // Person operations
    Task<List<Person>> GetUserPersonsAsync(int userId);
    Task<Person?> GetPersonAsync(int id);
    Task<Person?> FindPersonForUserAsync(int userId);
    Task<Person> EnsureUserHasPersonAsync(int userId, string email, string? userName);
    Task<Person> CreatePersonAsync(Person person);
    Task<Person> UpdatePersonAsync(Person person);
    Task DeletePersonAsync(int id);
    
    // Dependency operations
    Task<Dependency> CreateDependencyAsync(Dependency dependency);
    Task DeleteDependencyAsync(int id);
    
    // Note operations
    Task<List<Note>> GetUserNotesAsync(int userId);
    Task<Note> CreateNoteAsync(Note note);
    Task<Note> UpdateNoteAsync(Note note);
    Task DeleteNoteAsync(int id);
    
    // Tag operations
    Task<List<Tag>> GetUserTagsAsync(int userId);
    Task<Tag> CreateTagAsync(Tag tag);
    Task DeleteTagAsync(int id);
    
    // Config operations
    Task<UserConfig?> GetUserConfigAsync(int userId);
    Task<UserConfig> UpdateUserConfigAsync(UserConfig config);
}
