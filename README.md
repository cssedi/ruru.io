# ruru

A health and fitness app for tracking workouts and the nutrition from your meals, on one screen.

The repo has two parts:

| Part | Stack | Path |
| --- | --- | --- |
| Web app | React 19, TypeScript, Vite | [`frontend/ruru-frontend`](frontend/ruru-frontend) |
| API | ASP.NET Core (.NET 10), EF Core, PostgreSQL, ASP.NET Identity, JWT | [`backend/ruru-api`](backend/ruru-api) ([architecture](backend/ruru-api/README.md)) |

## Project status

- **Auth:** register and log in are wired end to end (React form, then API, then Identity).
- **Tracker:** the workout and meals screens run on local sample data (`src/data.ts`). The workout endpoints exist in the API but aren't called from the frontend yet.
- **Nutrition:** frontend only for now; no API or database tables yet.

## Repository layout

```
ruru.io/
├── backend/ruru-api/
│   ├── Program.cs            # service registration, auth, CORS, Swagger
│   ├── Migrations/           # EF Core migrations
│   └── src/
│       ├── controller/       # API controllers (Auth, Workout)
│       ├── services/         # business logic
│       ├── repositories/     # data access
│       ├── model/            # EF Core entities (User, Workout, Exercise, WorkoutExercise)
│       ├── data/             # RuruDbContext
│       ├── DTOs/             # request/response shapes
│       └── common/           # shared types (Result)
├── frontend/ruru-frontend/   # React web app
└── docs/rurugileERD.drawio   # database ERD (open with diagrams.net)
```

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 20.19+ or 22.12+
- PostgreSQL running locally
- EF Core CLI: `dotnet tool install --global dotnet-ef`

### 1. Run the API

```bash
cd backend/ruru-api

# Keep secrets out of appsettings.json and in .NET user secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ruru;Username=<user>;Password=<password>"
dotnet user-secrets set "Api:Key" "<a long random signing key>"

dotnet ef database update            # create/upgrade the database
dotnet run --launch-profile http     # serves http://localhost:5266
```

In Development, Swagger UI is at <http://localhost:5266/swagger> and the OpenAPI document at `/openapi/v1.json`.

On startup the API creates any missing roles from the `UserRole` enum (`Admin`, `User`).

### 2. Run the web app

```bash
cd frontend/ruru-frontend
npm install
npm run dev
```

Vite forwards requests on `/api` to the API at `http://localhost:5266` (see `vite.config.ts`), so start the API first.

## API endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/Auth/register` | Create an account |
| `POST` | `/api/Auth/login` | Sign in and receive a token |
| `GET` | `/api/Workout/GetWorkouts/{userId}` | List a user's workouts |
| `POST` | `/api/Workout/CreateWorkout` | Create a workout |

Swagger has the full request and response shapes.

## Database migrations

Run these from `backend/ruru-api`:

```bash
dotnet ef migrations add <Name>    # after changing entities in src/model
dotnet ef database update
```

## Conventions

- **Frontend routing** is a small hash router in `src/App.tsx`: `#/`, `#/login`, `#/signup`, `#/app?tab=workout|meals`.
- **Frontend forms** use React 19 actions (`useActionState`); see `src/pages/Auth.tsx`.
