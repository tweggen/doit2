using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DoitBlazor.Models;

namespace DoitBlazor.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Person> Persons { get; set; }
    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<Dependency> Dependencies { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<UserConfig> UserConfigs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Person relationships
        modelBuilder.Entity<Person>()
            .HasOne(p => p.OwningUser)
            .WithMany(u => u.OwnedPersons)
            .HasForeignKey(p => p.OwningUserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Person>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure TodoItem relationships
        modelBuilder.Entity<TodoItem>()
            .HasOne(t => t.Author)
            .WithMany(p => p.AuthoredItems)
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<TodoItem>()
            .HasOne(t => t.Contact)
            .WithMany(p => p.ContactedItems)
            .HasForeignKey(t => t.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure Dependency relationships
        modelBuilder.Entity<Dependency>()
            .HasOne(d => d.DemandingItem)
            .WithMany(t => t.DemandingFromDeps)
            .HasForeignKey(d => d.DemandingId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Dependency>()
            .HasOne(d => d.RequiredItem)
            .WithMany(t => t.RequiredByDeps)
            .HasForeignKey(d => d.RequiredId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure UserConfig relationship (one-to-one)
        modelBuilder.Entity<UserConfig>()
            .HasOne(c => c.User)
            .WithOne(u => u.Config)
            .HasForeignKey<UserConfig>(c => c.UserId);
        
        // Add index on due date for TodoItems (matching your Elixir migration)
        modelBuilder.Entity<TodoItem>()
            .HasIndex(t => t.Due);
        
        // Configure Identity table names to match your existing schema if needed
        modelBuilder.Entity<ApplicationUser>().ToTable("users");
        modelBuilder.Entity<IdentityRole<int>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("role_claims");
    }
}
