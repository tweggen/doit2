# Database Migration Guide

## Migrating from Elixir/Phoenix Doit to C# Blazor Doit

This guide helps you migrate your existing Elixir/Phoenix Doit database to work with the new C# Blazor application.

## Overview

The good news: **Your existing data can be preserved!** The C# models are designed to be compatible with your existing PostgreSQL schema.

## Pre-Migration Checklist

- [ ] Backup your existing database
- [ ] Note your current PostgreSQL connection details
- [ ] Verify you can connect to the database
- [ ] Review the schema differences below

## Schema Compatibility

### Existing Tables (No Changes Needed)

These tables remain unchanged and will work as-is:

- `todo_items` - Your tasks
- `todo_persons` - Your contacts
- `todo_deps` - Item dependencies
- `todo_notes` - Notes
- `todo_tags` - Tags
- `todo_configs` - User configurations

### Identity Tables (Will Be Added)

The C# application uses ASP.NET Core Identity for authentication. These new tables will be added:

- `users` (replaces/extends the original `users` table from Phoenix)
- `roles`
- `user_roles`
- `user_claims`
- `user_logins`
- `user_tokens`
- `role_claims`

## Migration Options

### Option 1: Use Existing Database (Recommended)

This approach adds Identity tables to your existing database while preserving all data.

1. **Update connection string in `appsettings.json`:**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=doit_dev;Username=postgres;Password=your_password"
   }
   ```

2. **Generate and apply migrations:**
   ```bash
   dotnet ef migrations add AddIdentityTables
   dotnet ef database update
   ```

3. **Map existing users:**
   You may need to manually migrate user records from the Phoenix `users` table to the new Identity `users` table.

### Option 2: Fresh Start

Start with a clean database if you don't need to preserve data:

1. **Create new database:**
   ```sql
   CREATE DATABASE doit_blazor_dev;
   ```

2. **Update connection string** to point to new database

3. **Run migrations:**
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. **Manually migrate important data** from old database if needed

## Column Name Mapping

The C# application uses these column naming conventions:

| Elixir/Phoenix | C# Property | Database Column |
|----------------|-------------|-----------------|
| `inserted_at` | `CreatedAt` | `inserted_at` |
| `updated_at` | `UpdatedAt` | `updated_at` |
| `user_id` | `UserId` | `user_id` |
| `author_id` | `AuthorId` | `author_id` |
| `contact_id` | `ContactId` | `contact_id` |

All mappings are configured in `ApplicationDbContext.cs` to match your existing schema.

## User Authentication Migration

### Existing Users (Phoenix)

If you have users in your existing database:

1. **Phoenix stores passwords using `bcrypt`**
2. **ASP.NET Identity uses different password hashing by default**

You have two options:

**Option A: Users Re-register**
- Simplest approach
- Users create new accounts in the Blazor app
- Old data can be associated with new user accounts manually

**Option B: Migrate Password Hashes** (Advanced)
- Install `BCrypt.Net-Next` NuGet package
- Create a custom `IPasswordHasher` implementation
- Gradually migrate users to Identity's hashing

### OAuth Tokens

If you were using GitHub/Google OAuth:
- OAuth tokens don't transfer directly
- Users need to re-authenticate with OAuth in the new app
- Connection to existing person records can be maintained via email

## Step-by-Step Migration Process

### 1. Backup Everything

```bash
pg_dump -h localhost -U postgres doit_dev > doit_backup_$(date +%Y%m%d).sql
```

### 2. Test Connection

Update `appsettings.json` and test:
```bash
dotnet ef database drop --force  # Only if testing!
dotnet ef database update
```

### 3. Add Identity Tables

```bash
# This adds Identity tables without touching existing tables
dotnet ef migrations add AddIdentitySupport
dotnet ef database update
```

### 4. Migrate User Data (SQL Script)

Create a simple SQL script to migrate basic user info:

```sql
-- Create a temporary mapping of old users to new Identity users
-- You'll need to adjust this based on your needs

INSERT INTO "AspNetUsers" (
    "Id",
    "UserName", 
    "NormalizedUserName",
    "Email",
    "NormalizedEmail",
    "EmailConfirmed",
    "PasswordHash",
    "SecurityStamp",
    "ConcurrencyStamp",
    "PhoneNumber",
    "PhoneNumberConfirmed",
    "TwoFactorEnabled",
    "LockoutEnd",
    "LockoutEnabled",
    "AccessFailedCount"
)
SELECT 
    id,
    email,
    UPPER(email),
    email,
    UPPER(email),
    CASE WHEN confirmed_at IS NOT NULL THEN TRUE ELSE FALSE END,
    '',  -- Empty password hash - users will need to reset
    CAST(gen_random_uuid() AS TEXT),
    CAST(gen_random_uuid() AS TEXT),
    NULL,
    FALSE,
    FALSE,
    NULL,
    TRUE,
    0
FROM users;

-- Note: Users will need to use "Forgot Password" to set new passwords
```

### 5. Test the Application

1. Start the app: `dotnet run`
2. Try logging in with an existing user (may need password reset)
3. Verify your todo items load correctly
4. Check that persons are accessible
5. Test creating new items

### 6. Verify Data Integrity

```sql
-- Check item counts match
SELECT COUNT(*) FROM todo_items;

-- Check person associations
SELECT COUNT(*) FROM todo_persons;

-- Verify user connections
SELECT u.email, COUNT(t.id) as item_count 
FROM users u 
LEFT JOIN todo_items t ON t.user_id = u.id 
GROUP BY u.id, u.email;
```

## Troubleshooting

### "Column does not exist" errors

- Check `ApplicationDbContext.OnModelCreating()` for column mappings
- Verify table names match with `.ToTable()` configurations

### Foreign key constraint errors

- Ensure user IDs from old users table match new Identity user IDs
- Check that person/item relationships are intact

### Authentication not working

- Users with migrated accounts may need to reset passwords
- Consider starting with fresh user registrations for testing

## Rollback Plan

If you need to rollback:

1. **Restore from backup:**
   ```bash
   psql -h localhost -U postgres -d doit_dev < doit_backup_YYYYMMDD.sql
   ```

2. **Remove Identity tables** (if needed):
   ```sql
   DROP TABLE IF EXISTS "AspNetUsers", "AspNetRoles", "AspNetUserRoles", 
                        "AspNetUserClaims", "AspNetUserLogins", 
                        "AspNetUserTokens", "AspNetRoleClaims" CASCADE;
   ```

## Post-Migration

After successful migration:

- [ ] Test all features thoroughly
- [ ] Have users verify their data
- [ ] Update any external integrations
- [ ] Archive the Elixir application
- [ ] Update documentation

## Need Help?

- Review the EF Core migration docs: https://docs.microsoft.com/ef/core/managing-schemas/migrations/
- Check ASP.NET Identity docs: https://docs.microsoft.com/aspnet/core/security/authentication/identity
- PostgreSQL backup/restore: https://www.postgresql.org/docs/current/backup.html
