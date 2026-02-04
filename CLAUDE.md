# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

"Geoffrey" — a Blazor Server task management app (C# port of an Elixir/Phoenix app). Uses .NET 9.0, PostgreSQL with EF Core, and ASP.NET Core Identity with OAuth (Google, GitHub).

## Build & Run Commands

```bash
# All commands run from DoitBlazor/ directory
cd DoitBlazor

dotnet restore          # Restore packages
dotnet build            # Build
dotnet watch run        # Dev with hot reload (https://localhost:7001)
dotnet run              # Dev without hot reload
dotnet run --environment Production  # Production (https://localhost:7002)

# Database
dotnet ef migrations add <Name>   # Create migration
dotnet ef database update          # Apply migrations (also auto-runs in dev)

# Docker
docker build -t doit-blazor -f DoitBlazor/Dockerfile .
docker run -p 8080:8080 -p 8081:8081 doit-blazor
```

No test project exists yet.

## Architecture

Single-project solution (`DoitBlazor.sln` → `DoitBlazor/`). Blazor Server with Interactive Server rendering over SignalR.

**Key layers:**
- `Components/Pages/` — Routable Blazor pages (`Index.razor` for todos, `Persons.razor` for contacts)
- `Services/` — Business logic: `TodoService` (CRUD), `ActionLogService` (undo/redo), `TodoItemChangeDetector` (change tracking)
- `Models/` — EF Core entities: `TodoItem`, `Person`, `Dependency`, `Note`, `Tag`, `UserConfig`, `ActionLog`, `ApplicationUser`
- `Data/ApplicationDbContext.cs` — EF Core context; tables use snake_case names for Elixir DB compatibility
- `Configuration/AppConfig.cs` — Brand name ("Geoffrey") and tagline

**Data flow:** Blazor Component → Service (DI) → ApplicationDbContext → PostgreSQL

**Undo/Redo system:** `ActionLogService` records changes via `TodoItemChangeDetector`, stores snapshots in `action_logs` table, supports compaction.

## Database

PostgreSQL. Connection string in `appsettings.Development.json` (`Host=localhost;Database=doit_blazor_dev;Username=postgres;Password=admin`). Schema is compatible with the original Elixir app. Tables: `todo_items`, `todo_persons`, `todo_deps`, `todo_notes`, `todo_tags`, `todo_configs`, `action_logs`, plus ASP.NET Identity tables.

## Key Relationships

- `TodoItem` has Author (Person), Contact (Person), and Owner (User)
- `Person` belongs to OwningUser and optionally links to a User account
- `Dependency` links Demanding ↔ Required TodoItems
- All user-facing data is scoped by authenticated user
