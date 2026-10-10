# TaskPulse

The backend API for TaskPulse, a task and project management platform built with **ASP.NET Core (.NET 10)**. Teams create projects, assign and track tasks, route sensitive changes through an approval workflow, chat, and get AI-generated insights on project health, all behind role-based access.

**Frontend:** the Next.js client lives in its own repo, [TaskPulse.FrontEnd](https://github.com/ibrahim-amir/TaskPulse.FrontEnd).

![.NET](https://img.shields.io/badge/.NET-10-512BD4) ![SQL Server](https://img.shields.io/badge/SQL%20Server-EF%20Core-CC2927) ![Tests](https://img.shields.io/badge/tests-117%20passing-2ea44f)

## Features

**Authentication & accounts**
- JWT access tokens with refresh tokens and "remember me" sessions
- Email confirmation, forgot/reset password and change password flows (SMTP via MailKit)
- Passwords hashed with BCrypt; refresh tokens stored hashed
- Roles: **Admin**, **Manager**, **Member**, enforced on every endpoint

**Projects & tasks**
- Projects with member management
- Tasks with assignment, status, priority, subtasks, to-do checklists and file attachments
- Search, filtering, sorting and pagination on list endpoints
- Comments on tasks and an activity log of every change

**Approval workflow**
- Sensitive actions (deleting or updating projects/tasks, removing members, completing or reassigning tasks, assigning a busy user, duplicating tasks) create an approval request instead of executing immediately
- Each action type has its own handler (strategy pattern), so new approval types plug in without touching existing code

**Collaboration**
- Notifications and direct conversations, including image messages
- User profiles with avatars and availability status
- Attendance tracking

**Dashboards & reports**
- Summary and per-project dashboards, progress reports and user performance stats
- Background service that monitors deadlines and notifies users about tasks due soon or overdue

**AI insights (Google Gemini)**
- AI task suggestions, project summaries and risk reports
- Team member assessments
- Project-aware chat and an in-app help assistant

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), C# |
| Data | Entity Framework Core (code-first migrations), SQL Server |
| Validation | FluentValidation |
| Auth | JWT bearer + refresh tokens, BCrypt |
| Email | MailKit (SMTP) |
| Logging | Serilog (rolling file logs) |
| AI | Google Gemini REST API |
| API docs | Swagger / OpenAPI |
| Testing | xUnit integration tests with `WebApplicationFactory` |

## Architecture

```
TaskPulse.API/                  solution root
├── TaskPulse.API/              Web API project
│   ├── Controllers/            HTTP layer only
│   ├── Services/               business logic (interfaces + implementations)
│   │   ├── ApprovalHandlers/   one handler per approval action type
│   │   └── BackgroundServices/ deadline monitor
│   ├── Repository/             data access per aggregate
│   ├── Entities/               EF Core entities
│   ├── DTO/                    request/response models
│   ├── Validation/             input + business-rule validators
│   ├── ExceptionHandling/      global and custom exception handlers
│   ├── Helper/                 authorization, paging, file storage, hashing
│   ├── Data/                   AppDbContext
│   └── Migrations/
└── TaskPulse.API.Tests/        xUnit integration tests
```

Requests flow **Controller → Service → Repository → EF Core**. Responses use a common response envelope, and errors are handled centrally by a global exception handler.

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server or LocalDB
- EF Core CLI: `dotnet tool install --global dotnet-ef`

### 1. Configure secrets

Secrets are never stored in `appsettings.json`. Set them with .NET User Secrets from the `TaskPulse.API/TaskPulse.API` folder:

```bash
dotnet user-secrets set "Jwt:Key" "<a long random string, 32+ characters>"
dotnet user-secrets set "Gemini:ApiKey" "<your Gemini API key>"        # optional, AI features
dotnet user-secrets set "Smtp:Username" "<your Gmail address>"         # optional, real emails
dotnet user-secrets set "Smtp:AppPassword" "<your Gmail app password>" # optional, real emails
```

Without the Gemini key, AI endpoints are skipped. Without SMTP credentials, emails are written to the log instead of being sent.

The default connection string points at LocalDB (`(localdb)\mssqllocaldb`). Change `ConnectionStrings:DefaultConnection` if you use another instance.

### 2. Create the database and run the API

```bash
cd TaskPulse.API
dotnet ef database update
dotnet run --launch-profile https
```

Swagger UI: `https://localhost:7070/swagger`

### 3. Run the frontend

Clone [TaskPulse.FrontEnd](https://github.com/ibrahim-amir/TaskPulse.FrontEnd) and follow its README. Set `NEXT_PUBLIC_API_BASE_URL=https://localhost:7070` in its `.env.local`. The API's CORS policy already allows `http://localhost:3000`.

## Running Tests

```bash
dotnet test
```

117 integration tests cover authentication, users, projects, tasks, files and checklists, comments, notifications, activity logs, dashboards and the approval workflow. They run against the real API pipeline using `WebApplicationFactory`.

## API Overview

Every endpoint is documented and testable in Swagger once the API is running. Main areas:

| Area | Covers |
|---|---|
| Auth | register, login, refresh, confirm email, password reset |
| Users | profiles, roles, status (admin) |
| Projects | projects and members |
| Tasks | tasks, subtasks, checklists, attachments, comments |
| Approvals | approval requests and decisions |
| Notifications | notifications and conversations |
| Dashboard | summaries, reports, performance |
| Activity logs | audit trail |
| Help | AI help assistant |

## Author

**Muhammad Ibrahim**, BS Computer Science, University of Central Punjab, Lahore
