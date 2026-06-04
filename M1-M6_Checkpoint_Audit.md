# M1-M6 Checkpoint Audit

Date: 2026-06-03

## Summary Table

| Milestone | Status | Evidence | Issues |
| --------- | ------ | -------- | ------ |
| M1 - Backend Foundation | PASS | `dotnet build --no-restore` passed; `dotnet ef database update --project Learnify.Infrastructure --startup-project Learnify.Web` reported no pending migrations; startup migration log succeeded; entities, DbContext, repositories, and UnitOfWork compile. | NuGet vulnerability warnings remain. |
| M2 - Auth/JWT/API Security | PASS | Live API register/login/refresh passed; protected `/api/courses` returned 401 without JWT and worked with JWT; `Program.cs` has JWT bearer auth plus `UseAuthentication()` before `UseAuthorization()`. | None. |
| M3 - Frontend Integration/UI | PASS | `npm run build` in `Learnify.Client` passed; Login/Register compile; `PrivateRoute` guards protected routes; Axios base URL is `VITE_API_BASE_URL` fallback `http://localhost:5073`; interceptor refreshes once via `_retry`, retries, and clears auth on failed refresh. | None. |
| M4 - Background Services | PASS | `dotnet test --no-restore` passed; NotificationWorker and EmailNotificationService compile; runtime startup reached hosted services and HTTP binding, showing workers did not block startup. | None. |
| M5 - AI Integration | PASS_WITH_CAVEAT | Gemini is default; default model is `gemini-3.5-flash`; no API key is committed in appsettings; live `/api/ai/provider`, `/api/ai/summarize`, and `/api/ai/flashcards` passed; Ollama remains `http://127.0.0.1:8080`. | Local LLaMA server was offline, so local generation was not verified. |
| M6R - Smart Learning Core Closeout | PASS_WITH_CAVEAT | Live new-user empty courses, course create/edit/detail/delete, persistent AI settings GET/PUT, safe API-key response, `.txt/.md` upload, and Gemini generation passed; static UI inspection confirmed settings page, global AI badge, exact empty text, course detail route, and flashcard viewer controls. | PDF text extraction remains partial. |

## Verification Commands

- `dotnet build --no-restore` - PASS, 0 errors, 19 NuGet vulnerability warnings.
- `dotnet test --no-restore` - PASS by exit code.
- `dotnet ef database update --project Learnify.Infrastructure --startup-project Learnify.Web` - PASS, database already up to date.
- `dotnet run --project Learnify.Web --no-build --launch-profile http` - started/held until timeout from repo root; rerun from `Learnify.Web` showed migrations and hosted services started, then failed only because an existing API process already occupied `http://127.0.0.1:5073`.
- `npm run build` from `Learnify.Client` - PASS by exit code.

## Runtime Tests

- Register test user - PASS.
- Login - PASS, JWT issued.
- Refresh token - PASS, new JWT issued.
- Protected endpoint without JWT - PASS, `/api/courses` returned 401.
- Protected endpoint with JWT - PASS.
- New user courses = 0 - PASS.
- Course create/edit/detail/delete - PASS.
- `/api/ai/provider` - PASS, `Gemini` / `gemini-3.5-flash`.
- Gemini summarize - PASS, generated summary returned.
- Gemini flashcards - PASS, 3 cards returned with populated `question` and `answer`.
- `.txt` upload through `/api/notes/upload` - PASS, `Note.Content` persisted.
- `.md` upload through `/api/notes/upload` - PASS, `Note.Content` persisted.
- `GET /api/user/ai-settings` - PASS, no raw API key returned.
- `PUT /api/user/ai-settings` - PASS.
- Saved Ollama settings with BaseUrl exactly `http://127.0.0.1:8080` - PASS.
- Local LLaMA reachability - PASS_WITH_CAVEAT, server not reachable at `http://127.0.0.1:8080`.

## Regressions Found

None.

## Documentation Mismatches

- `Work_Done.md` had stale Last Updated text and still marked M6 as in progress.
- `Work_Done.md` still listed fixed M6 items as known issues: seed courses, outdated Gemini model, missing course add/delete UI, and missing per-user AI provider settings.
- `FEATURE_GAPS.md` was effectively empty, so M6R statuses were missing.
- `README.md` milestone badge still said M6 in progress.
- `README.md` Dashboard feature rows said `/dashboard`, welcome message, and quick actions were not started, but those exist in `Learnify.Client/src/pages/Dashboard.tsx` and routes.

## Fixes Applied

- Updated `Work_Done.md` M1-M6 checkpoint status and M6 known issues/caveats.
- Rebuilt `FEATURE_GAPS.md` with compact M6R-related statuses.
- Updated `README.md` feature status for M6R badge and existing Dashboard basics.
- Added this audit report.

## Remaining Caveats

- Codex shell could access local SQL Server and the local API. The already-running API process caused one foreground `dotnet run` to fail with address-in-use after startup checks had succeeded.
- Local LLaMA/Ollama server is offline at `http://127.0.0.1:8080`.
- PDF extraction remains partial; no real PDF text extraction was found.
- NuGet vulnerability warnings remain for AutoMapper, Azure.Identity, Microsoft.Data.SqlClient, Microsoft.Extensions.Caching.Memory, System.Formats.Asn1, and System.Text.Json.

## Decision

READY_FOR_M7
