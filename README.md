# DoitBlazor

A C# Blazor Server port of the Doit todo list application, originally built with Elixir/Phoenix.

## Overview

This is a modern, real-time todo list application built with:
- **ASP.NET Core 9.0** - Web framework
- **Blazor Server** - Interactive UI with real-time updates via SignalR
- **Entity Framework Core** - ORM for PostgreSQL
- **ASP.NET Core Identity** - User authentication
- **OAuth** - Google and GitHub authentication

## Features

- ✅ Todo item management with due dates
- 👥 Person/contact management
- 🔗 Item dependencies
- 📝 Notes and tags system
- 🔐 User authentication (Email/Password, Google, GitHub)
- ⚙️ User configurations
- 🔄 Real-time updates

## Database Schema

The application uses PostgreSQL and maintains compatibility with the original Elixir schema:

- `users` - User accounts with Identity framework
- `todo_items` - Todo tasks with status, due dates, content
- `todo_persons` - Contacts/assignees
- `todo_deps` - Dependencies between items
- `todo_notes` - Notes about persons
- `todo_tags` - Tags for categorizing notes
- `todo_configs` - User configuration stored as JSON

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL 12+
- (Optional) Visual Studio 2022 or VS Code with C# extension

### Installation

1. **Restore NuGet packages:**
   ```bash
   cd DoitBlazor
   dotnet restore
   ```

2. **Configure database connection:**
   
   Update `appsettings.json` with your PostgreSQL connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=doit_dev;Username=your_user;Password=your_password"
   }
   ```

3. **Configure OAuth (optional):**
   
   Add your OAuth credentials to `appsettings.json`:
   ```json
   "Authentication": {
     "Google": {
       "ClientId": "your-google-client-id",
       "ClientSecret": "your-google-client-secret"
     },
     "GitHub": {
       "ClientId": "your-github-client-id",
       "ClientSecret": "your-github-client-secret"
     }
   }
   ```

4. **Create and apply migrations:**
   
   If starting fresh with a new database:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   
   If migrating from the existing Elixir app, your existing database should work with minimal changes.

5. **Run the application:**
   ```bash
   dotnet run
   ```
   
   Navigate to `https://localhost:5001` (or the port shown in the console)

## Project Structure

```
DoitBlazor/
├── Models/              # Entity models (User, TodoItem, Person, etc.)
├── Data/                # DbContext and migrations
├── Services/            # Business logic layer
├── Components/
│   ├── Pages/          # Routable Blazor pages
│   ├── Layout/         # Layout components
│   └── Shared/         # Shared components
└── wwwroot/            # Static files (CSS, JS)
```

## Migrating Data from Elixir App

If you have an existing Elixir/Phoenix Doit database:

1. The C# models are designed to be compatible with your existing schema
2. You may need to adjust column names slightly (check `ApplicationDbContext.cs`)
3. Identity tables will be added by EF Core migrations
4. Run `dotnet ef database update` to add the Identity tables to your existing database

## Development

### Adding New Migrations

```bash
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Running in Development Mode

```bash
dotnet watch run
```

This enables hot reload for code changes.

## Differences from Elixir Version

### Architecture
- **Elixir Contexts** → **C# Services** - Business logic layer
- **Phoenix LiveView** → **Blazor Server** - Real-time interactive UI
- **Ecto** → **Entity Framework Core** - ORM
- **Ecto.Changeset** → **Data Annotations + FluentValidation** - Validation

### Benefits of C# Port
- Strong typing throughout the application
- Rich IDE support with IntelliSense
- Excellent debugging tools
- Native Windows development experience
- Large .NET ecosystem
- Built-in dependency injection
- SignalR for real-time features

## Authentication

The app supports three authentication methods:

1. **Email/Password** - Traditional registration and login
2. **Google OAuth** - Sign in with Google account
3. **GitHub OAuth** - Sign in with GitHub account

All authentication is handled by ASP.NET Core Identity with external provider integration.

## Todo: Next Steps

Here are some features you might want to add:

- [ ] Implement item editing functionality
- [ ] Add filtering and sorting options
- [ ] Implement the notes and tags UI
- [ ] Add dependency management UI
- [ ] Implement file attachments
- [ ] Add email notifications for due items
- [ ] Create API endpoints for mobile apps
- [ ] Add unit tests

## Contributing

This is a personal project port. Feel free to fork and customize for your own needs!

## License

This project maintains the same license as the original Elixir version.
