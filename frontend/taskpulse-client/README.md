# TaskPulse Client

Angular 15 frontend for [TaskPulse](../../README.md).

## Structure

- `src/app/core/`: auth service (login, register, current user), JWT interceptor that attaches the token to every request, auth and role guards, shared models
- `src/app/features/auth/`: login screen built with Reactive Forms, inline validation and "remember me"
- `src/app/features/dashboard/`: dashboard that loads live stats from the API
- `src/environments/`: API base URL (`https://localhost:7070/api` in development)

## Run

Start the backend first, then:

```bash
npm install
npm start        # http://localhost:4200
```

The API's CORS policy allows `http://localhost:4200`.

## Test

```bash
npm test
```
