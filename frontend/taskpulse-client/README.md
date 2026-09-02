# taskpulse-client — Angular frontend starter

What's here (Phase 4 of the build roadmap — Angular foundation):

- `src/app/core/` — `services/auth.service.ts` (login/register/me, token +
  user stored in localStorage), `interceptors/auth.interceptor.ts` (attaches
  the JWT to every request), `guards/auth.guard.ts` + `guards/role.guard.ts`,
  and the shared `models/`.
- `src/app/features/auth/login/` — a working login screen: Reactive Forms,
  validation, inline error messages, a disabled/loading submit button, and
  a "remember me" checkbox.
- `src/app/features/dashboard/` — a dashboard shell that calls
  `GET /api/dashboard/summary` on load. Nothing is hardcoded — once that
  backend endpoint exists, the numbers just show up.
- `src/app/shared/` — empty `components/`, `pipes/`, `directives/` folders,
  ready for reusable pieces as you build out projects/tasks/etc.
- `src/environments/` — `apiUrl` pointed at `https://localhost:5001/api` in
  dev; update this to match wherever `dotnet run` actually serves the API.

This was scaffolded with the real Angular CLI (`ng new --routing
--style=scss`) and verified with `ng build` before being handed to you, so
`angular.json` / `tsconfig*.json` / `package.json` are all stock, valid
Angular 15 config — nothing hand-typed or guessed.

## Run it

```bash
npm install
npm start        # ng serve — http://localhost:4200
```

Make sure the backend (`TaskPulse.API`) is running first — the login screen
has nothing to talk to otherwise. Its CORS config already allows
`http://localhost:4200` by default (see the backend's `appsettings.json`).

## What's next (Phase 5 of the roadmap)

`app-routing.module.ts` currently only knows `/login` and `/dashboard`.
Next: split each area into its own feature —
`features/projects` (list, create/edit, details, members),
`features/tasks` (Kanban board with drag-and-drop, create/edit, details
with comments + attachments) — and convert each into a lazy-loaded module
as it grows. Add Chart.js (`npm install chart.js ng2-charts`) for the two
required dashboard charts (§17).
