# ruru web app

The React frontend for ruru. See the [project README](../../README.md) for the full setup, including the API.

## Scripts

| Command | What it does |
| --- | --- |
| `npm run dev` | Start the dev server; `/api` is proxied to `http://localhost:5266` |
| `npm run build` | Typecheck, then build to `dist/` |
| `npm run typecheck` | Typecheck only |
| `npm run lint` | Run ESLint |
| `npm run preview` | Serve the production build locally |

## Structure

```
src/
├── App.tsx          # hash router: #/, #/login, #/signup, #/app?tab=workout|meals
├── pages/           # Landing, Auth, Tracker
├── components.tsx   # shared UI (Logo, ThemeToggle, ...)
├── services/        # API calls; http.ts holds the shared fetch helper
├── models/          # TypeScript types for API and app data
└── data.ts          # local sample exercises/foods used by the tracker
```
