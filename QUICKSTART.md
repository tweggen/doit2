# Quick Start Guide for DoitBlazor

## First Time Setup

### Step 1: Verify Prerequisites

Make sure you have:
- .NET 8.0 SDK installed (`dotnet --version`)
- PostgreSQL running locally or accessible
- Your favorite IDE (Visual Studio, Rider, or VS Code)

### Step 2: Database Setup

1. **If using existing Elixir database:**
   - Update `appsettings.json` with your existing database connection
   - The schema is compatible - just run migrations to add Identity tables
   
2. **If starting fresh:**
   - Create a new PostgreSQL database:
     ```sql
     CREATE DATABASE doit_dev;
     ```
   - Update `appsettings.json` with connection string

### Step 3: Restore and Build

Open PowerShell in the DoitBlazor directory:

```powershell
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build
```

### Step 4: Database Migrations

```powershell
# Install EF Core tools if not already installed
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate --project DoitBlazor

# Apply migrations to database
dotnet ef database update --project DoitBlazor
```

### Step 5: Run the Application

```powershell
cd DoitBlazor
dotnet run
```

The application will start and show you the URL (typically `https://localhost:5001`)

### Step 6: Create Your First User

1. Navigate to the application URL
2. Click "Register" or "Log in"
3. Create an account with email/password
4. Start adding tasks!

## Troubleshooting

### Connection String Issues

If you get database connection errors:
- Verify PostgreSQL is running
- Check username/password in `appsettings.json`
- Ensure the database exists
- Check firewall settings

### Migration Issues

If migrations fail:
- Ensure you're in the correct directory
- Try: `dotnet ef database drop` (warning: deletes data!) then `dotnet ef database update`
- Check that your connection string is correct

### Port Already in Use

If port 5001 is already in use, you can change it in `Properties/launchSettings.json`

## Next Steps

1. **Configure OAuth (Optional)**
   - Get Google OAuth credentials from Google Cloud Console
   - Get GitHub OAuth credentials from GitHub Developer Settings
   - Add them to `appsettings.json`

2. **Customize**
   - Modify colors and styling in `wwwroot/css/app.css`
   - Add your own features
   - Adjust the models to fit your workflow

3. **Deploy**
   - Consider Azure App Service, AWS, or a Linux VPS
   - Don't forget to set environment variables for production!

## Common Commands

```powershell
# Run with hot reload (development)
dotnet watch run

# Create a new migration after model changes
dotnet ef migrations add MigrationName

# Update database with latest migrations
dotnet ef database update

# Run tests (when you add them)
dotnet test

# Publish for deployment
dotnet publish -c Release -o ./publish
```

## Getting Help

- Check the README.md for more detailed information
- ASP.NET Core docs: https://docs.microsoft.com/aspnet/core
- Blazor docs: https://docs.microsoft.com/aspnet/core/blazor
- Entity Framework Core docs: https://docs.microsoft.com/ef/core
