# TaskPulse: Full-Stack Task & Project Management System

A full-stack Task and Project Management System where users create and manage
projects, add team members, create and assign tasks, track progress, comment
on tasks, upload attachments, receive notifications, and monitor performance
through dashboards and reports.

**Current status:** backend-first build. The Angular frontend hasn't been
started yet. The API is being built and tested via Swagger/Postman first,
then the frontend will follow.

## Technology Stack

| Layer          | Technology                                  |
|----------------|----------------------------------------------|
| Frontend       | Angular 15+, TypeScript, RxJS, Reactive Forms (not started yet) |
| Backend        | .NET 8, ASP.NET Core Web API                  |
| ORM            | Entity Framework Core (Code-First)            |
| Database       | Microsoft SQL Server                          |
| Auth           | JWT (JSON Web Tokens)                         |
| API Docs       | Swagger / OpenAPI                             |
| API Testing    | Postman (during backend-only development)     |
| Charts         | Chart.js or equivalent (later, dashboard phase) |

## Project Architecture

Current repo layout (backend-only for now):

```
TaskPulse.API/                      ← repo root (this is the solution folder)
├── TaskPulse.API.slnx
└── TaskPulse.API/                  ← the actual project folder (.csproj lives here)
    ├── Controllers/     # HTTP request/response only
    ├── Services/        # Business logic (added as CRUD is built out)
    ├── Repositories/    # Data access (added as CRUD is built out)
    ├── DTOs/            # API request/response shapes
    ├── Entities/        # EF Core entity classes
    ├── Data/            # AppDbContext
    ├── Migrations/      # EF Core migrations
    ├── Middleware/      # Global exception handling, etc.
    ├── Helpers/
    ├── Validators/
    └── Program.cs
```

Once the Angular app is started, a `frontend/taskpulse-client/` folder will
be added as a sibling of `TaskPulse.API/` at the repo root, with the usual
`core/`, `shared/`, `features/` structure.

## User Roles & Permissions

| Role    | Can do |
|---------|--------|
| Admin   | Create/edit/disable/delete users, view & manage all projects, manage project members, view activity logs, view system-wide statistics |
| Manager | Create/edit projects, manage project members, create/assign/edit/delete tasks (where authorized), view project reports, monitor progress |
| Member  | View assigned projects/tasks, update task status, add comments, upload attachments |

Authorization is enforced on the backend via `[Authorize(Roles = "...")]`.
The frontend (once built) restricts UI access for UX, but the API is the
actual gate.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Developer Edition, or a full instance)
- EF Core CLI tool: `dotnet tool install --global dotnet-ef`
- [Postman](https://www.postman.com/) for exercising the API during
  backend-only development
- (Later, once frontend work starts) Node.js 18+, npm, Angular CLI

## Backend Setup

1. **Configure the connection string** in
   `TaskPulse.API/appsettings.json` (or an `appsettings.Development.json`
   you create locally, which is gitignored):

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=TaskPulseDb;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

   Adjust `Server=` for a named instance (e.g. `.\SQLEXPRESS`) or use
   SQL auth (`User Id=...;Password=...;`) if that's your setup.

2. **Apply EF Core migrations** (creates the database and all 8 tables):

   ```bash
   cd TaskPulse.API
   dotnet ef database update
   ```

   (Or in Visual Studio's Package Manager Console, with `TaskPulse.API` set
   as the default project: `Update-Database`.)

3. **Run the API:**

   ```bash
   dotnet run
   ```

   Swagger UI is available at `https://localhost:<port>/swagger` once
   running. Every endpoint is testable from there, including with a Bearer
   token pasted in after logging in. A Postman collection can also be kept
   alongside this repo (or exported/committed under a `postman/` folder) for
   quicker repeat testing while the frontend doesn't exist yet.

### Creating migrations after entity changes

Whenever an entity class changes (new field, new relationship):

```bash
dotnet ef migrations add <DescriptiveName>
dotnet ef database update
```

## Frontend Setup

*Not started yet.* Once the Angular app is scaffolded (roadmap Phase 5),
this section will cover `cd frontend/taskpulse-client`, `npm install`,
`ng serve`, and the API base URL configuration.

## Default / Test Login Credentials

No seed data exists yet. To get a first account:

1. Register via `POST /api/auth/register` (through Swagger or Postman).
   This creates a `Member` by default.
2. To get an `Admin` account for testing, register normally, then manually
   update that user's `Role` column to `Admin` directly in the database
   (or add a one-time seed script under `Data/`, recommended before
   demoing, since test credentials should be documented here).

*(This section should be filled in with real seeded credentials once a
seed/data script is added, e.g. `admin@taskpulse.com` / a known password,
before release.)*

## Running Tests

**Backend** (minimum 10 unit tests required: auth, project CRUD, task CRUD,
task filtering, authorization, invalid requests):

```bash
cd TaskPulse.API
dotnet test
```

**Frontend**: not applicable yet; will cover `ng test` once the Angular app
exists (minimum 5 tests required: login form validation, project form
validation, task form validation, task filtering, dashboard stats).

## API Overview

Full endpoint list and filtering/pagination/sorting query parameters are
documented in Swagger once the API is running. Key resource groups:

- `/api/auth`: register, login, me
- `/api/users`: CRUD (Admin only)
- `/api/projects`: CRUD, `/api/projects/{id}/members`
- `/api/projects/{projectId}/tasks`, `/api/tasks/{id}`: supports
  `?search=&status=&priority=&assignedUserId=&sortBy=&sortOrder=&page=&pageSize=`
- `/api/tasks/{taskId}/comments`
- `/api/notifications`
- `/api/dashboard/summary`, `/api/dashboard/project/{projectId}`

## Status

- [x] Database design, ER diagram, EF Core entities
- [x] JWT authentication (register/login/me)
- [ ] Backend CRUD for Projects/Tasks/Members/Comments/Attachments/
      Notifications/ActivityLogs (in progress, DTOs being designed now)
- [ ] Angular frontend (not started)
- [ ] Unit tests
- [ ] Final polish

## License

Personal project.
