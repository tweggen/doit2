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

# Docker (run from repo root)
docker build -t doit-blazor -f DoitBlazor/Dockerfile .
docker run -p 8080:8080 -p 8081:8081 doit-blazor
```

No test project exists yet.

## Architecture

Single-project solution (`DoitBlazor.sln` → `DoitBlazor/`). Blazor Server with Interactive Server rendering (`@rendermode InteractiveServer` set per-page, not app-level).

**Key layers:**
- `Components/Pages/` — Routable Blazor pages (`Index.razor` for todos, `Persons.razor` for contacts)
- `Services/` — Business logic via DI (scoped lifetime)
- `Models/` — EF Core entities with `[Table("snake_case")]` and `[Column("snake_case")]` attributes for Elixir DB compatibility
- `Data/ApplicationDbContext.cs` — EF Core context with snake_case table/column mappings
- `Areas/Identity/` — Scaffolded ASP.NET Identity UI pages (Login, Register) with custom styling
- `Configuration/AppConfig.cs` — Brand name ("Geoffrey") and tagline

**Data flow:** Blazor Component → Service (DI) → ApplicationDbContext → PostgreSQL

## Service Layer

**TodoService** is the main service, handling CRUD for TodoItems, Persons, Dependencies, Notes, Tags, and UserConfig. All queries are scoped by authenticated `userId`.

**ActionLogService** implements undo/redo:
- Records field-level changes as JSON via `TodoItemChangeDetector` (static comparison helper)
- Uses "lazy capture": stores old values immediately, captures new values only at undo time to enable redo
- Compaction: max 100 undoable actions per entity; actions older than 7 days compacted into daily summaries (marked read-only)
- Compaction must be called explicitly via `CompactOldActionsAsync()` (no background job)

## Database

PostgreSQL. Connection string in `appsettings.Development.json` (`Host=localhost;Database=doit_blazor_dev;Username=postgres;Password=admin`).

**Dev auto-setup:** In development, `Program.cs` automatically creates the database if missing and runs pending migrations on startup.

**Identity uses integer primary keys** (`IdentityUser<int>`, `IdentityRole<int>`).

**All relationships use `DeleteBehavior.Restrict`** — no cascading deletes.

**Status field pattern:** `Status` (int) is used across entities as soft-delete/completion flag (0 = active, 1 = completed/deleted).

## Key Relationships

- `TodoItem` has Author (Person), Contact (Person), and Owner (User)
- `Person` belongs to OwningUser and optionally links to a User account
- **Person auto-creation:** `EnsureUserHasPersonAsync()` creates a Person record for every new User on first page load
- `Dependency` links Demanding ↔ Required TodoItems
- `UserConfig` stores per-user settings as JSONB (`PropertiesJson` column with Dictionary helper)

## UI Patterns

- **No CSS isolation** — all styles in a single `wwwroot/css/app.css` using CSS variables for theming
- **Modals** for add/edit with inline quick-add contact form (saves/restores parent form state)
- **Inline date editing** — click date in list to edit without opening modal
- **Keyboard shortcuts** — Enter, Escape, Ctrl+Z/Y in Index.razor
- **Color-coded due dates** — red (overdue), yellow (today), green (future) via `TodoItem.GetDueState()`
- **Late percentage indicator** — shows % of active items overdue
- **PWA-ready** — manifest.json, apple-touch-icon, safe-area-insets
- **Landing page** for unauthenticated users with custom SVG design

## Unimplemented Features

Backend service methods exist but no UI yet for: **Dependencies**, **Notes**, **Tags**.
