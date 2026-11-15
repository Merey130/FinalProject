# StudyFlow API (requires login)

Register and sign in through `/register` and `/login`. The server sets an HttpOnly session cookie. API requests from the same browser automatically send the cookie.

- `GET /api/health` — public configuration health.
- `GET /api/me` — current account.
- `GET /api/tasks` — current user's tasks.
- `POST /api/tasks` — `{ "title":"Math homework", "subject":"Mathematics", "deadline":"2026-11-22" }`
- `PATCH /api/tasks/1` — `{ "isDone": true }`
- `DELETE /api/tasks/1`
- `GET /api/exams` — current user's mock exams and subject scores.
- `POST /api/exams` — `{ "title":"Mock exam 1", "takenOn":"2026-11-15", "subjects":[{"subject":"Mathematics","score":40,"maxScore":50}] }`
- `DELETE /api/exams/1`
- `GET /api/progress`

The backend forwards the authenticated user's Supabase JWT to PostgREST. RLS is enforced by PostgreSQL. Never use a service-role or secret API key in this application.
