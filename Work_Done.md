## Audit Results â€” 2026-06-03

### Summary
| Category | Complete | Partial | Not Started |
|----------|----------|---------|-------------|
| Auth & Users | 3 | 1 | 3 |
| Course Management | 3 | 2 | 3 |
| Notes & Content | 4 | 2 | 4 |
| AI Features | 3 | 2 | 6 |
| Flashcard System | 3 | 0 | 6 |
| Quiz System | 6 | 0 | 11 |
| Study Planner | 0 | 0 | 6 |
| Progress & Analytics | 0 | 0 | 6 |
| Achievement System | 0 | 0 | 5 |
| Dashboard | 2 | 1 | 5 |
| Settings | 0 | 1 | 6 |
| Infrastructure/UI | 2 | 3 | 2 |
| **TOTAL** | **26** | **12** | **52** |

> Audit performed by scanning actual file content â€” not milestone plans.

---

## Runtime Verification Update - 2026-06-03

### SQL Server Startup/Auth Root Cause
- `Learnify.Web` uses `ConnectionStrings:DefaultConnection` and applies EF migrations at startup via `Database.Migrate()`.
- LocalDB is not usable on this machine: `sqllocaldb` only reported a broken `v11.0` automatic instance, and creating `MSSQLLocalDB` failed.
- SQL Server Express is installed and Windows-auth only (`IsIntegratedSecurityOnly = 1`), with the app database already present as `LearnPlatformDb`.
- Base config without `Encrypt=False` can fail with a local encryption error; Development config now provides an explicit SQLEXPRESS connection with `TrustServerCertificate=True;Encrypt=False`.
- Sandboxed command execution can still trigger `Failed to generate SSPI context`; running `dotnet run` normally outside the sandbox successfully starts the API and applies migrations.

### Files Changed For Runtime + Local Provider
- `Learnify.Web/appsettings.json` - removed committed Gemini API key, kept Gemini default/model, changed Ollama BaseUrl to `http://127.0.0.1:8080`.
- `Learnify.Web/appsettings.Development.json` - added explicit SQLEXPRESS development connection string and local Ollama BaseUrl/model.
- `Learnify.Web/Program.cs` - added development console/debug logging, safer DB migration diagnostics, and `UseAuthentication()`.
- `Learnify.Web/Controllers/AiController.cs` - added normalized provider settings and `POST /api/ai/provider/test`.
- `Learnify.Infrastructure/DependencyInjection.cs`, `AiProviderFactory.cs`, `UserAiSettingsStore.cs`, `AiSettings.cs` - default Ollama BaseUrl now `http://127.0.0.1:8080`; added separate `LocalOpenAI` provider support for OpenAI-compatible llama.cpp servers.
- `Learnify.Infrastructure/AI/Providers/LocalOpenAiProvider.cs` - added local `/v1/chat/completions` provider with optional bearer token support.
- `Learnify.Infrastructure/Migrations/20260603230545_AddLocalOpenAiProvider.cs` - added nullable `UserAiSettings.LocalOpenAiBaseUrl`.
- `Learnify.Client/src/services/aiService.ts` - normalized backend `provider` response to frontend `activeProvider`.
- `Learnify.Client/src/components/AI/AiProviderBadge.tsx` - distinguishes `Ollama` from `Local OpenAI / llama.cpp` and shows local base URL when selected.
- `Learnify.Client/src/components/AI/AiProviderSettings.tsx` - adds separate `Local OpenAI-Compatible / llama.cpp` option with `http://127.0.0.1:8080` and `Qwen3.6-35B-A3B-UD-Q4_K_M.gguf` defaults.
- `Learnify.Client/src/pages/Settings.tsx`, `App.tsx`, `Navbar.tsx` - added protected `/settings` route and nav link.

### Verification Results
- `dotnet build --no-restore` - passed with 0 errors; existing package/security warnings remain.
- `npm run build` in `Learnify.Client` - passed with 0 errors.
- `dotnet test --no-restore` - passed.
- `dotnet ef migrations add AddLocalOpenAiProvider --project Learnify.Infrastructure --startup-project Learnify.Web` - passed.
- `dotnet ef database update --project Learnify.Infrastructure --startup-project Learnify.Web` - applied `20260603230545_AddLocalOpenAiProvider`.
- `dotnet run --project Learnify.Web --no-build --launch-profile http` - backend started successfully outside the command sandbox and listened on `http://localhost:5073`.
- Auth-protected endpoints verified: unauthenticated `GET /api/ai/provider` and `GET /api/courses` returned 401.
- Registration/login verified with a fresh test user; JWT was issued.
- New-user empty courses state verified: `GET /api/courses` returned 0 courses.
- Course create/edit/delete verified through live API.
- `GET /api/ai/provider` verified default Gemini response: `Gemini` / `gemini-3.5-flash`.
- `POST /api/ai/summarize` verified through Learnify API using Gemini; returned a generated summary.
- `POST /api/ai/flashcards` verified through Learnify API using Gemini; returned 3 cards with populated `question`/`answer` fields after the parser/prompt alignment fix.
- Persistent `GET/PUT /api/user/ai-settings` verified: default Gemini returns `gemini-3.5-flash`, saved Gemini never returns the raw key, saved Ollama keeps `http://127.0.0.1:8080`.
- Direct `POST /api/notes/upload` verified with `.txt` multipart upload: returned 201 and stored extracted text in `Note.Content`.
- Local LLaMA retest 2026-06-04: added separate protocol support for `Ollama` (`/api/*`) and `LocalOpenAI` (`/v1/*`).
- Direct local server probe confirmed `http://127.0.0.1:8080/v1/models` and `/v1/chat/completions` work with `Qwen3.6-35B-A3B-UD-Q4_K_M.gguf`.
- Ollama remains separate: saved provider `Ollama` still uses `/api/tags` and `/api/generate`.
- LocalOpenAI settings verified: saved provider `LocalOpenAI`, model `Qwen3.6-35B-A3B-UD-Q4_K_M.gguf`, BaseUrl `http://127.0.0.1:8080`, and raw API keys are not returned.
- LocalOpenAI runtime generation verified through Learnify: summarize, flashcards, quiz generation, and generated quiz submission all passed.

### Local Setup Commands
```bash
dotnet user-secrets set "AiSettings:Gemini:ApiKey" "YOUR_KEY" --project Learnify.Web
dotnet ef database update --project Learnify.Infrastructure --startup-project Learnify.Web
dotnet run --project Learnify.Web --launch-profile http
```

---

# Work_Done.md â€” LearnifyAI Progress Diary

> Last Updated: 2026-06-03 (Asia/Dhaka, UTC+6:00)

---

## Verification Status

### M1-M6 Checkpoint Audit - 2026-06-03 - READY_FOR_M7

| Milestone | Status | Evidence |
|-----------|--------|----------|
| M1 - Backend Foundation | PASS | `dotnet build --no-restore` passed; EF database update reported no pending migrations; runtime startup applied migrations successfully. |
| M2 - Auth/JWT/API Security | PASS | Live API register, login, refresh, JWT-protected access, and unauthenticated 401 checks passed. |
| M3 - Frontend Integration/UI | PASS | `npm run build` in `Learnify.Client` passed; static inspection confirms protected routes and single-retry Axios refresh handling. |
| M4 - Background Services | PASS | `dotnet test --no-restore` passed; NotificationWorker/EmailNotificationService compile and hosted services did not block HTTP startup. |
| M5 - AI Integration | PASS_WITH_CAVEAT | Gemini default `gemini-3.5-flash`, provider status, summarize, and flashcards passed through Learnify API; a direct probe previously showed `http://127.0.0.1:8080` serving an OpenAI-compatible llama.cpp API, and Learnify now has a separate `LocalOpenAI` provider for that protocol. |
| M6R - Smart Learning Core Closeout | PASS_WITH_CAVEAT | New-user empty courses, course CRUD/detail, AI settings, `.txt/.md` note upload, global AI badge, settings UI, and flashcard viewer controls verified; PDF extraction remains partial. |

### Milestone 1 â€” Backend Foundation â€” âœ… CONFIRMED FUNCTIONAL
- `Learnify.Core/Entities/BaseEntity.cs` â€” Guid Id, CreatedAt, UpdatedAt âœ…
- `Learnify.Core/Entities/User.cs` â€” FullName, Email, PasswordHash, PasswordSalt, IsActive âœ…
- `Learnify.Core/Entities/Course.cs` â€” Title, Description, UserId FK âœ…
- `Learnify.Core/Entities/Note.cs` â€” Content, CourseId FK âœ…
- `Learnify.Core/Entities/Lesson.cs` â€” Title, Content, CourseId FK âœ…
- `Learnify.Infrastructure/Data/ApplicationDbContext.cs` â€” DbSet + Configurations âœ…
- `Learnify.Infrastructure/Repositories/EfRepository.cs` â€” Generic CRUD âœ…
- `Learnify.Infrastructure/Repositories/CourseRepository.cs` âœ…
- `Learnify.Infrastructure/Repositories/NoteRepository.cs` âœ…
- `Learnify.Infrastructure/Repositories/UserRepository.cs` âœ…
- `Learnify.Infrastructure/Repositories/LessonRepository.cs` âœ…
- `Learnify.Infrastructure/UnitOfWork/UnitOfWork.cs` âœ…
- `Learnify.Core/Interfaces/IRepository.cs` âœ…
- `Learnify.Core/Interfaces/IUserRepository.cs` âœ…
- `Learnify.Core/Interfaces/ICourseRepository.cs` âœ…
- `Learnify.Core/Interfaces/INoteRepository.cs` âœ…
- `Learnify.Core/Interfaces/ILessonRepository.cs` âœ…
- `Learnify.Core/Interfaces/IUnitOfWork.cs` âœ…
- `Learnify.Infrastructure/Migrations/` â€” EF Core migrations present âœ…

### Milestone 2 â€” Auth/JWT/API Security â€” âœ… CONFIRMED FUNCTIONAL
- `Learnify.Application/Interfaces/IAuthService.cs` âœ…
- `Learnify.Application/Services/AuthService.cs` â€” Register/Login/Refresh âœ…
- `Learnify.Application/DTOs/AuthDTOs.cs` â€” LoginDTO, RegisterDTO, AuthResponseDTO âœ…
- `Learnify.Web/Controllers/AuthController.cs` â€” `/api/auth/register`, `/login`, `/refresh` âœ…
- `Learnify.Web/Config/JwtSettings.cs` âœ…
- `Learnify.Web/Program.cs` â€” JWT bearer auth middleware âœ…
- `Learnify.Application/ApiResponse.cs` â€” Standardized API response wrapper âœ…
- `Learnify.Web/Middleware/GlobalExceptionMiddleware.cs` âœ…
- `Learnify.Web/Validators/UserValidator.cs` â€” FluentValidation âœ…
- `Learnify.Web/Controllers/UsersController.cs` âœ…
- `Learnify.Web/Controllers/CoursesController.cs` âœ…
- `Learnify.Web/Controllers/LessonsController.cs` âœ…

### Milestone 3 â€” Full-Stack Integration & UI â€” âœ… CONFIRMED FUNCTIONAL
- `Learnify.Client/.env` â€” VITE_API_BASE_URL configured âœ…
- `Learnify.Client/src/services/api.ts` â€” Axios instance with JWT interceptors âœ…
- `Learnify.Client/src/services/authService.ts` â€” Login/Register/Logout âœ…
- `Learnify.Client/src/store/authStore.ts` â€” Zustand auth state âœ…
- `Learnify.Client/src/components/PrivateRoute.tsx` â€” Route guard âœ…
- `Learnify.Client/src/pages/Login.tsx` âœ…
- `Learnify.Client/src/pages/Register.tsx` âœ…
- `Learnify.Client/src/pages/Courses.tsx` â€” Dashboard with course grid âœ…
- `Learnify.Client/src/App.tsx` â€” React Router v7, protected routes âœ…
- Build verified: tsc + vite build â€” 0 errors âœ…

### Milestone 3 â€” QA Verified â€” âœ…
- PrivateRoute: no infinite redirect loops âœ…
- authStore: `isAuthenticated` properly set on login/logout âœ…
- Courses.tsx: loading, empty, and error states all handled âœ…
- JSON camelCase naming policy fixed in `Program.cs` âœ…
- Backend build: 0 errors, 20 warnings (nuget vulnerability â€” not blocking) âœ…
- Frontend build: 0 errors, 86 modules transformed âœ…

### Milestone 4 â€” Background Services â€” âœ… CONFIRMED FUNCTIONAL
- `Learnify.Core/Interfaces/INotificationService.cs` âœ…
- `Learnify.Infrastructure/Services/EmailNotificationService.cs` â€” Channel<T> queue âœ…
- `Learnify.Web/Workers/NotificationWorker.cs` â€” HostedService âœ…
- `Learnify.Tests/NotificationWorkerTests.cs` â€” 7 tests, all passing âœ…
- `Learnify.Application/DTOs/NotificationMessage.cs` âœ…
- CORS: `LearnifyPolicy` for `http://localhost:5173` âœ…
- NotificationWorker: fire-and-forget â€” does NOT block HTTP thread âœ…
- Build: 0 errors âœ… Â· Tests: Passed 7 / Failed 0 âœ…

### Milestone 5 â€” AI Integration (Multi-Provider) â€” âœ… CONFIRMED FUNCTIONAL
- `Learnify.Core/Interfaces/IAiProvider.cs` â€” Strategy contract âœ…
- `Learnify.Core/Interfaces/IAiService.cs` â€” Summarize, Flashcards, StudyTips âœ…
- `Learnify.Core/Models/AiRequestOptions.cs` âœ…
- `Learnify.Core/Models/FlashcardResult.cs` âœ…
- `Learnify.Application/Settings/AiSettings.cs` âœ…
- `Learnify.Infrastructure/AI/AiProviderFactory.cs` â€” Strategy pattern âœ…
- `Learnify.Infrastructure/AI/Providers/GeminiAiProvider.cs` â€” (`gemini-3.5-flash`) âœ…
- `Learnify.Infrastructure/AI/Providers/OpenAiProvider.cs` âœ…
- `Learnify.Infrastructure/AI/Providers/OllamaAiProvider.cs` âœ…
- `Learnify.Infrastructure/AI/Providers/ClaudeAiProvider.cs` âœ…
- `Learnify.Infrastructure/AI/AiService.cs` âœ…
- `Learnify.Infrastructure/DependencyInjection.cs` â€” all 4 providers registered âœ…
- `Learnify.Web/Controllers/AiController.cs` â€” 5 endpoints, all `[Authorize]` âœ…
- `Learnify.Client/src/services/aiService.ts` âœ…
- `Learnify.Client/src/components/AI/NoteSummarizer.tsx` âœ…
- `Learnify.Client/src/components/AI/FlashcardViewer.tsx` âœ…
- `Learnify.Client/src/components/AI/StudyTips.tsx` âœ…
- `Learnify.Client/src/components/AI/AiProviderBadge.tsx` âœ…
- `Learnify.Client/src/pages/Notes/NoteDetail.tsx` âœ…
- `Learnify.Web/appsettings.json` â€” AiSettings block, ActiveProvider = "Gemini" âœ…

---

## Milestone 6 - Smart Learning Core - M6R VERIFIED

### 6.1 â€” Remove Seed Data (New User Empty State)

**Goal:** A newly registered user must see 0 courses â€” no pre-seeded demo data.  
**Problem:** `ApplicationDbContext.OnModelCreating()` (or a seeder class) inserts 3 sample courses at startup. All new users see these courses, which is incorrect.

- [x] Remove all `HasData()` calls seeding `Course` records from `ApplicationDbContext.cs`
- [x] Search for and delete any `DataSeeder.cs`, `SeedData.cs`, or `DbInitializer.cs` that inserts course or user records (preserve schema-only migrations)
- [x] Remove any seeder invocations from `Learnify.Web/Program.cs`
- [x] Update empty-state message in `Courses.tsx` to: *"You have no courses yet. Click 'New Course' to get started."*
  - Runtime verified 2026-06-03: `Courses.tsx` contains the exact empty-state text.
- [x] Verify: register a new account -> Courses page shows empty state, 0 courses
  - Runtime verified 2026-06-03: live `GET /api/courses` for a fresh user returned 0 courses.

---

### 6.2 â€” Course Management (Add & Delete)

**Goal:** Users can create new courses and delete existing ones from the dashboard.

**Backend:**
- [x] Add `POST /api/courses` endpoint â€” create course for the currently authenticated user (read `UserId` from JWT claims, not request body)
- [x] Add `DELETE /api/courses/{id}` endpoint â€” delete course only if `UserId` matches authenticated user (return 403 if not owner)
- [x] Add `CreateCourseDto` with `Title` (required, max 100 chars) and `Description` (optional, max 500 chars)
- [x] Return `201 Created` with created course on POST; `204 No Content` on DELETE
- [x] Add ownership validation â€” a user cannot delete another user's course

**Frontend:**
- [x] Add "New Course" button to the top-right of `Courses.tsx`
- [x] On click, show a modal dialog with Title and Description inputs
- [x] On submit, call `POST /api/courses`, close modal, refresh course list
- [x] Add a delete icon/button on each course card
- [x] On delete click, show a confirmation dialog: *"Delete [Course Name]? This cannot be undone."*
- [x] On confirm, call `DELETE /api/courses/{id}`, remove card from list without full page reload

---

### 6.3 â€” Upgrade Default Gemini Model to `gemini-3.5-flash`

**Goal:** Replace the outdated Gemini Flash model with `gemini-3.5-flash` as the default model.

- [x] Update `Learnify.Infrastructure/AI/Providers/GeminiAiProvider.cs` â€” use configured model `"gemini-3.5-flash"`
- [x] Update `Learnify.Web/appsettings.json` â€” set `"Model": "gemini-3.5-flash"` under `AiSettings:Gemini`
- [x] Update `Learnify.Client/src/components/AI/AiProviderBadge.tsx` â€” update display label for Gemini to show `"Gemini 3.5 Flash"`
- [x] Verify: call `/api/ai/provider` endpoint and confirm response shows `"gemini-3.5-flash"`
  - Runtime verified 2026-06-03: live `GET /api/ai/provider` returned provider `Gemini`, model `gemini-3.5-flash`.
  - Runtime verified 2026-06-03: live `POST /api/ai/summarize` returned a Gemini-generated summary through the Learnify API.

> **Runtime note:** Direct Google Gemini REST probe with `gemini-3.5-flash` returned HTTP 200 on 2026-06-03; Learnify summarize and flashcard calls also passed when the API process had outbound network access.

---

### 6.4 â€” Per-User AI Provider Settings (Backend)

**Goal:** Each user can store their preferred AI provider and API key in their account, replacing server-wide config.

- [x] Create `Learnify.Core/Entities/UserAiSettings.cs` â€” properties: `Id`, `UserId` (FK), `ActiveProvider` (string), `ApiKey` (string, nullable), `CustomModel` (string, nullable), `OllamaBaseUrl` (string, nullable)
- [x] Add `DbSet<UserAiSettings>` to `ApplicationDbContext` + EF configuration (one-to-one with User)
- [x] Create and apply EF Core migration: `AddUserAiSettings`
- [x] Create `IUserAiSettingsRepository` and `UserAiSettingsRepository`
- [x] Create `UserAiSettingsController` with:
  - `GET /api/user/ai-settings` â€” return current user's AI settings (return default Gemini config if none saved)
  - `PUT /api/user/ai-settings` â€” save/update user's AI settings
- [x] Update `AiProviderFactory` to accept per-request `UserAiSettings` override (fall back to `appsettings.json` if no user settings)
- [x] Update `AiController` to resolve the requesting user's settings and pass them to the factory
- [x] **Security:** Never return the raw `ApiKey` value in GET responses â€” return only provider name and whether a key is set (`HasApiKey: true/false`)
  - Runtime verified 2026-06-03: default Gemini, saved Gemini, saved Ollama/local LLaMA, and safe no-raw-key responses all passed.

---

### 6.5 â€” Per-User AI Provider Settings (Frontend)

**Goal:** Settings page where users pick their AI provider and optionally enter their own API key.

- [x] Create `Learnify.Client/src/pages/Settings.tsx`
- [x] Add route `/settings` to `App.tsx` (protected)
- [x] Add "Settings" link to nav/header
- [x] Settings page contains an "AI Provider" card with:
  - Dropdown to choose provider: Gemini / OpenAI / Claude / Ollama
  - Text input for API key (masked, placeholder: *"Enter your API key"*) â€” hidden for Ollama
  - Text input for Ollama Base URL (only shown when Ollama selected)
  - "Save Settings" button
- [x] On load, call `GET /api/user/ai-settings` and populate fields
- [x] On save, call `PUT /api/user/ai-settings`
- [x] Show success/error message on save
- [x] Display a badge in the global protected navigation showing the currently active provider
  - Runtime note: `/settings` persists provider settings; local LLaMA switching was verified with `http://127.0.0.1:8080`.

---

### 6.6 â€” Note File Upload System

- [x] Add `POST /api/notes/upload` endpoint â€” accept `.txt` and `.md` files (max 2MB)
- [x] Extract plain text from uploaded file; store as `Note.Content`
- [x] Add direct upload UI to `NotesList.tsx` alongside the existing AI analyze upload flow
- [x] Runtime verified 2026-06-03: `.txt` and `.md` multipart uploads returned 201 and persisted uploaded text.
  - PDF parsing remains partial through `POST /api/notes/analyze-upload`; no real PDF text extraction was added in M6R.

---

### 6.7 â€” Modern Flashcard UI Redesign

- [x] Redesign `FlashcardViewer.tsx` with:
  - Smooth 3D CSS flip animation (front = Question, back = Answer)
  - Progress indicator: `Card 3 of 12`
  - Keyboard navigation: `â†` / `â†’` to navigate, `Space` to flip
  - "Shuffle" button
  - Score tracking (mark card as "Got it" / "Review again")
- [x] Runtime verified 2026-06-03: Gemini flashcards returned populated `question`/`answer` values through `/api/ai/flashcards`.
  - Spaced repetition scheduling and review history remain future M7+ work.

---

## Milestone 7 - Quiz Engine - CORE FLOW VERIFIED

### Implementation Summary

- Added quiz persistence entities: `Quiz`, `Question`, `QuizAttempt`, and `QuizAttemptAnswer`.
- Added `AddQuizEngine` EF migration with `Quizzes`, `Questions`, `QuizAttempts`, and `QuizAttemptAnswers`.
- Added `AddQuizPolishTimer` EF migration with optional `Quizzes.TimeLimitMinutes`.
- Extended `IAiService` / `AiProviderFactory` with strict JSON quiz generation and parsing.
- Added `QuizService` for note ownership validation, quiz save/list/detail, attempt submission, scoring, and result mapping.
- Added authorized quiz endpoints for generate, list, detail, attempts submit, and attempts list.
- Added protected frontend routes `/quizzes`, `/quizzes/:id`, and `/quizzes/:id/result`.
- Added Quizzes link in protected navigation.
- Added M7.1 quiz polish: fill-in-the-blank generation/scoring, practice/exam answer visibility, optional timer, and frontend retry for incorrect answers.

### Files Changed

- `Learnify.Core/Entities/Quiz.cs`
- `Learnify.Core/Interfaces/IQuizRepository.cs`
- `Learnify.Core/Interfaces/IQuizAttemptRepository.cs`
- `Learnify.Core/Interfaces/IUnitOfWork.cs`
- `Learnify.Core/Interfaces/IAiService.cs`
- `Learnify.Core/Models/GeneratedQuizResult.cs`
- `Learnify.Application/DTOs/QuizDTOs.cs`
- `Learnify.Application/Interfaces/IQuizService.cs`
- `Learnify.Application/Services/QuizService.cs`
- `Learnify.Infrastructure/Data/ApplicationDbContext.cs`
- `Learnify.Infrastructure/AI/AiProviderFactory.cs`
- `Learnify.Infrastructure/Repositories/QuizRepository.cs`
- `Learnify.Infrastructure/Repositories/QuizAttemptRepository.cs`
- `Learnify.Infrastructure/UnitOfWork/UnitOfWork.cs`
- `Learnify.Infrastructure/Migrations/20260603175058_AddQuizEngine.cs`
- `Learnify.Infrastructure/Migrations/20260603195335_AddQuizPolishTimer.cs`
- `Learnify.Web/Controllers/QuizzesController.cs`
- `Learnify.Web/Program.cs`
- `Learnify.Client/src/services/quizService.ts`
- `Learnify.Client/src/pages/Quizzes.tsx`
- `Learnify.Client/src/pages/QuizTaking.tsx`
- `Learnify.Client/src/pages/QuizResult.tsx`
- `Learnify.Client/src/App.tsx`
- `Learnify.Client/src/components/Layout/Navbar.tsx`

### Verification Commands

- `dotnet build --no-restore` - passed with 0 errors; existing NuGet vulnerability warnings remained at this checkpoint and were resolved in M8.2.
- `dotnet test --no-restore` - passed by exit code.
- `dotnet ef migrations add AddQuizPolishTimer --project Learnify.Infrastructure --startup-project Learnify.Web` - passed.
- `dotnet ef database update --project Learnify.Infrastructure --startup-project Learnify.Web` - passed after changing optional quiz course/note foreign keys to `NoAction`.
- `dotnet run --project Learnify.Web --no-build --launch-profile http` - startup held until command timeout; API was then started from the built DLL for runtime smoke tests.
- `npm run build` in `Learnify.Client` - passed by exit code.

### Runtime Verification - 2026-06-03

- Protected `GET /api/quizzes` returned 401 without JWT.
- Registered/logged in a fresh test user and received JWT.
- Created a course and note with enough study content.
- Generated a quiz from the note using default Gemini (`gemini-3.5-flash`).
- Generated an M7.1 mixed quiz with multiple choice, true/false, fill-in-the-blank, and short answer.
- Confirmed quiz was saved and returned by `GET /api/quizzes`.
- Confirmed `GET /api/quizzes/{id}?includeAnswers=false` hides correct answers for exam mode.
- Confirmed `GET /api/quizzes/{id}?includeAnswers=true` returns correct answers and explanations for practice mode.
- Confirmed generated questions had populated question text, correct answers, explanations, and options where applicable.
- Confirmed fill-in-the-blank generation uses a blank marker (`____`) and scoring trims whitespace / ignores case.
- Confirmed the optional timer limit is persisted (`timeLimitMinutes = 1`) and frontend timer mode compiles.
- Submitted a quiz attempt and received score/percentage.
- Confirmed result details include user answers, correct answers, correctness, points, and explanations.
- Confirmed result data includes incorrect answers for the frontend Retry Incorrect Questions session.
- Confirmed `GET /api/quizzes/{id}/attempts` returned the attempt.
- Confirmed a second user cannot access the first user's quiz (`404`).
- Added `LocalOpenAI` provider path for llama.cpp/OpenAI-compatible `/v1/*` servers while keeping `Ollama` on `/api/*`.
- Confirmed Learnify's provider test reports clear protocol-specific results for `Ollama` and `LocalOpenAI`.
- Confirmed `LocalOpenAI` can be saved with `http://127.0.0.1:8080` and `Qwen3.6-35B-A3B-UD-Q4_K_M.gguf`, and `GET /api/ai/provider` reports `LocalOpenAI`.
- Final LocalOpenAI runtime generation completed successfully through Learnify: provider test passed with `compatibleApi=openai`, summarize returned content, flashcards returned 2 cards, quiz generation returned 2 questions, and submitting the generated quiz scored 2/2.

### M7 Re-Verification - 2026-06-05

- `dotnet build --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test --no-restore` - passed, 26 backend tests.
- `dotnet list package --vulnerable --include-transitive` - passed; no vulnerable packages reported for Core, Application, Infrastructure, Web, or Tests.
- `dotnet ef database update --project Learnify.Infrastructure --startup-project Learnify.Web` - passed; database was already up to date.
- `dotnet run --project Learnify.Web --no-build --launch-profile http` - attempted; migrations ran, then startup could not bind because another Learnify.Web instance was already listening on `127.0.0.1:5073`.
- `npm test -- --run` in `Learnify.Client` - passed, 7 files / 20 tests.
- `npm run build` in `Learnify.Client` - passed.
- Runtime smoke against the existing API instance confirmed unauthenticated `GET /api/quizzes` and `GET /api/ai/provider` return `401`.
- Runtime smoke registered two fresh users, created a course and note, generated a 3-question Gemini quiz from note content, retrieved it with populated question text/options/correct answers/explanations, submitted correct answers, received score `3/3` and `100%`, listed one attempt, and confirmed the second user receives `404` for the first user's quiz.
- Default provider check confirmed `Gemini` with model `gemini-3.5-flash`.
- Local LLaMA was not re-verified in this pass: `http://127.0.0.1:8080/v1/models` was not reachable.

### Completed Quiz Features

- AI-generated quizzes from note content.
- Multiple choice, true/false, short answer, and fill-in-the-blank questions.
- Difficulty selection.
- Optional timer mode.
- Practice mode with immediate local feedback and explanations.
- Exam mode with correct answers hidden until submission.
- Quiz list/detail API and UI.
- Quiz taking UI.
- Attempt scoring and percentage calculation.
- Result breakdown with correct answers and explanations.
- Retry Incorrect Questions frontend session.
- Quiz ownership enforcement.

### Remaining Quiz Gaps

- Matching questions.
- Scenario-based questions.
- Coding questions.
- AI-generated hints.
- Weakness analysis.
- Related concept questions.
- Adaptive quiz engine.
- Quiz analytics dashboard.

---

## Planned Tasks

### Milestone 8 - Testing & QA Hardening - PARTIAL PASS

Automated tests do not require a local LLM server. `LocalOpenAI` and `Ollama` protocol coverage uses mocked/fake `HttpClient` handlers; the real local provider is reserved for optional runtime smoke verification only.

#### Backend Tests Added
- [x] `AiProviderProtocolTests` verifies `LocalOpenAI` uses `/v1/chat/completions`, optional bearer auth, malformed response failures, and `Ollama` uses `/api/generate` instead of OpenAI chat endpoints.
- [x] `QuizServiceTests` verifies scoring for multiple choice, true/false, short answer, fill-in-the-blank trim/case normalization, empty/wrong answers, ownership protection, answer visibility, timer persistence, and malformed AI quiz data rejection.
- [x] Added `Learnify.Tests` to `Learnify.sln` so solution-level `dotnet test` includes the QA suite.
- [x] Fixed provider error logging in `OllamaAiProvider` and `LocalOpenAiProvider` so JSON response bodies cannot be misread as log-format placeholders.

#### Frontend Tests Added
- [x] Added Vitest, React Testing Library, Jest DOM, jsdom, and `npm test -- --run`.
- [x] `AiProviderSettings.test.tsx` verifies the `LocalOpenAI` option, provider-specific defaults, save payload, and connection-test payload.
- [x] `AiProviderBadge.test.tsx` verifies `LocalOpenAI` displays separately from `Ollama`.
- [x] `QuizResult.test.tsx` verifies score display, explanations, and Retry Incorrect Questions UI.
- [x] Fixed provider dropdown behavior so selecting `LocalOpenAI` switches to the correct local default model instead of keeping the previous provider's model.

#### Verification - 2026-06-04
- `dotnet build --no-restore` - passed with 0 errors; known NuGet warnings remained at this checkpoint and were resolved in M8.2.
- `dotnet test --no-restore` - passed, 18 backend tests.
- `npm test -- --run` in `Learnify.Client` - passed, 3 files / 6 tests.
- `npm run build` in `Learnify.Client` - passed.
- Secret scan for common API-key patterns - no committed cloud AI keys found.
- `.gitignore` hardened for `node_modules`, `dist`, `.env*`, secrets, coverage, and generic logs.
- Optional real `LocalOpenAI` runtime smoke passed after the local server became reachable at `http://127.0.0.1:8080`: provider test reported `compatibleApi=openai`, summary returned content, flashcards returned 2 cards, quiz generation returned 2 questions, and submitting the generated quiz scored 2/2. Automated tests still use mocks/fakes and do not require the local LLM.

#### Vulnerability Audit
- `dotnet list package --vulnerable --include-transitive` at solution level failed due a NuGet cache/version parsing issue (`'' is not a valid version string`).
- Per-project scans succeeded for `Learnify.Application` and `Learnify.Infrastructure` and confirmed existing transitive vulnerabilities: AutoMapper, Azure.Identity, Microsoft.Data.SqlClient, Microsoft.Extensions.Caching.Memory, System.Formats.Asn1, and System.Text.Json.
- Per-project scans for `Learnify.Web` and `Learnify.Tests` were blocked by the same NuGet cache/version parsing issue after an attempted `dotnet nuget locals http-cache --clear` could not fully clear locked cache entries.

#### Remaining M8 Work
- [ ] Integration tests for `AiController` endpoints.
- [ ] Integration tests for `UserAiSettingsController`.
- [ ] Integration tests for `QuizzesController`.
- [ ] Frontend tests for Courses/Settings/quiz-taking timer flows.
- [ ] Playwright E2E: register -> create course -> add note -> generate quiz -> submit quiz.
- [ ] Remediate or suppress documented NuGet vulnerabilities.

### Milestone 8.1 - Integration Tests + Vulnerability Cleanup - PARTIAL PASS

#### Tests Added
- [x] Added `Learnify.Tests/Integration/LearnifyWebApplicationFactory.cs` with `WebApplicationFactory`, an isolated EF Core in-memory database, fake `IAiService`, and fake local-protocol `IHttpClientFactory`.
- [x] Added `Learnify.Tests/Integration/ControllerIntegrationTests.cs` covering protected 401s, authenticated JWT flows, `AiController`, `UserAiSettingsController`, `QuizzesController`, answer hiding, scoring, attempts, and cross-user quiz isolation.
- [x] Added `Learnify.Client/src/pages/Courses.test.tsx` for empty state, create modal, edit, and delete UI basics.
- [x] Added `Learnify.Client/src/pages/Settings.test.tsx` for loading settings, saving Gemini, and saving `LocalOpenAI` without confusing it with `Ollama`.
- [x] Added `Learnify.Client/src/pages/QuizTaking.test.tsx` for question rendering, practice feedback, exam answer hiding, timer display, and submit service calls.

#### Vulnerability / Warning Cleanup
- [x] Updated safe .NET 8 patch packages: EF Core packages to `8.0.27`, `Microsoft.AspNetCore.Authentication.JwtBearer` to `8.0.27`, `Microsoft.Extensions.Http` to `8.0.1`, `Microsoft.Extensions.DependencyInjection` to `8.0.1`, `Microsoft.Extensions.Logging.Abstractions` to `8.0.3`, and `System.IdentityModel.Tokens.Jwt` to `7.5.2`.
- [x] Pinned test-only legacy transitive packages `System.Net.Http` to `4.3.4` and `System.Text.RegularExpressions` to `4.3.1`, removing the test-project warnings introduced by `Microsoft.AspNetCore.Mvc.Testing`.
- [x] Removed small compiler warnings in `AiProcessingWorker` and `LessonsController`.
- [x] M8.2 follow-up removed the vulnerable `AutoMapper.Extensions.Microsoft.DependencyInjection` package, upgraded to `AutoMapper 16.1.1`, and updated AutoMapper registration for the new API.
- [x] M8.2 follow-up removed scan-blocking `FluentValidation.AspNetCore` and `Moq`/`Castle.Core` dependencies, replacing them with core FluentValidation registration plus a small MVC action filter and manual test fakes.

#### Verification - 2026-06-04
- `dotnet restore` - passed; remaining M8.1 warning was AutoMapper only, resolved in M8.2.
- `dotnet build --no-restore` - passed; 0 errors, AutoMapper vulnerability warnings remained in M8.1 and were resolved in M8.2.
- `dotnet test --no-restore` - passed, 25 backend tests in M8.1; M8.2 passes 26 backend tests.
- `npm test -- --run` in `Learnify.Client` - passed, 6 files / 16 tests.
- `npm run build` in `Learnify.Client` - passed.
- `dotnet list package --vulnerable --include-transitive` - failed at solution level in M8.1 with the NuGet empty-version parsing issue; M8.2 resolves this and the solution-level scan passes.
- Per-project vulnerability scans:
  - `Learnify.Core` - no vulnerable packages.
  - `Learnify.Application` - transitive `AutoMapper 12.0.1`, High, `GHSA-rvv3-g6hj-g44x`.
  - `Learnify.Infrastructure` - transitive `AutoMapper 12.0.1`, High, `GHSA-rvv3-g6hj-g44x`.
  - `Learnify.Web` - scan blocked by `FluentValidation.AspNetCore` NuGet registration/cache empty-version issue.
  - `Learnify.Tests` - scan blocked by `Castle.Core` and `FluentValidation.AspNetCore` NuGet registration/cache empty-version issue.

#### Runtime Smoke - 2026-06-04
- Protected endpoint returned 401 without JWT.
- Register and login passed.
- Course create, edit, detail, and delete passed on a delete-only course.
- `.txt` and `.md` note uploads passed.
- Default Gemini/mock-equivalent summarize returned content.
- Real `LocalOpenAI` server was reachable at `http://127.0.0.1:8080`.
- `LocalOpenAI` provider test passed with `compatibleApi=openai`.
- `LocalOpenAI` flashcards returned 2 cards, quiz generation returned 2 questions, and quiz submission scored 2/2.
- Caveat resolved in M8.2: `LocalOpenAI` summarize returned an empty string on two runtime attempts in this M8.1 smoke because the reasoning model consumed the small completion budget before final content. Automated tests remain mocked/faked and do not require the local LLM.

#### Remaining M8.1 Work
- [ ] Playwright E2E smoke is not implemented; Playwright is not installed/configured.
- [x] Resolved in M8.2: AutoMapper vulnerability remediation.
- [x] Resolved in M8.2: NuGet vulnerability scanner empty-version issue for `FluentValidation.AspNetCore` / `Castle.Core` registration metadata.
- [x] Resolved in M8.2: `LocalOpenAI` summarize returning empty content at runtime while flashcards and quiz generation work.

### Milestone 8.2 - Caveat Cleanup + Real Provider Runtime Verification - PASS_WITH_CAVEAT

#### LocalOpenAI Summary Caveat Cleanup
- [x] Diagnosed the empty `LocalOpenAI` summary response against `http://127.0.0.1:8080/v1/chat/completions`: the Qwen reasoning model returned empty `message.content` with `finish_reason=length` when the summary completion budget was too small.
- [x] Increased local summary `MaxTokens` to `3000`, kept temperature low, and tightened the summary prompt to request final summary text only.
- [x] Updated `LocalOpenAiProvider` to reject empty `choices[0].message.content` with a clear exception and safe response-shape logging instead of silently returning an empty string.
- [x] Added a mocked provider protocol test for empty-content failure handling; automated tests still do not require a real local LLM.

#### Vulnerability / Scan Cleanup
- [x] Replaced vulnerable transitive `AutoMapper 12.0.1` by removing `AutoMapper.Extensions.Microsoft.DependencyInjection` and referencing `AutoMapper 16.1.1` directly.
- [x] Replaced `FluentValidation.AspNetCore` with `FluentValidation.DependencyInjectionExtensions` and a small `FluentValidationActionFilter`, avoiding the NuGet registration metadata issue while preserving controller validation.
- [x] Removed `Moq` from the test project and replaced the quiz-service mocks with manual fakes, eliminating the `Castle.Core` scan blocker.
- [x] Solution-level `dotnet list package --vulnerable --include-transitive` now completes successfully and reports no vulnerable packages.
- [x] Removed tracked `.env` file from the Git index; local env files remain ignored and the local copy was left in place.

#### Verification - 2026-06-04
- `dotnet restore` - passed.
- `dotnet build --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test --no-restore` - passed, 26 backend tests.
- `dotnet list package --vulnerable --include-transitive` - passed at solution level with no vulnerable packages reported.
- `npm test -- --run` in `Learnify.Client` - passed, 6 files / 16 tests.
- `npm run build` in `Learnify.Client` - passed.

#### Real Provider Runtime Smoke - 2026-06-04
- Direct `LocalOpenAI` `/v1/models` and `/v1/chat/completions` checks passed with `Qwen3.6-35B-A3B-UD-Q4_K_M.gguf`.
- Learnify `LocalOpenAI` provider smoke passed: provider test reported `compatibleApi=openai`, summarize returned non-empty content, flashcards returned 2 cards, quiz generation returned 2 questions, and quiz submission scored 2/2.
- Learnify Gemini smoke passed with default `gemini-3.5-flash`: provider settings, summarize, flashcards, quiz generation, and quiz submission all passed.

#### Remaining M8 Work
- [ ] Playwright E2E smoke is not implemented; Playwright is not installed/configured.

### Milestone 8.3 - Modern UI/UX Polish Using Stitch Design Reference - COMPLETE

#### UI System / App Shell
- [x] Added a lightweight reusable frontend UI layer in `Learnify.Client/src/components/UI/Primitives.tsx` for page headers, cards, section panels, buttons, badges, loading, error, empty, and stat states.
- [x] Replaced the old protected top nav with a shared responsive app shell: left sidebar, sticky topbar, active route highlighting, disabled future items, user/logout area, and AI provider badge.
- [x] Reworked `Learnify.Client/src/index.css` into LearnifyAI design tokens and reusable classes inspired by the Stitch references: light lavender/off-white workspace, white cards, indigo primary, subtle borders/shadows, responsive grids, right-side AI panels, form controls, and accessible focus states.

#### Pages Polished
- [x] Dashboard: greeting header, stat cards, quick actions, learning heatmap placeholder, AI recommendation, and recent notes caveat.
- [x] Courses and Course Detail: modern cards, frontend-only search/filter chips, preserved create/edit/delete/detail flows, and preserved exact empty-state text: `You have no courses yet. Click 'New Course' to get started.`
- [x] Notes List: folders/tags side panel, text/Markdown upload dropzone styling, note cards with AI-ready chips, and existing create/upload flows preserved.
- [x] Note Detail: Stitch-style note reader with sticky AI Analysis panel, disabled Ask AI Tutor coming-soon action, summary/flashcard/quiz actions, key-concept placeholder caveat, attachments, file upload, and existing AI helpers preserved.
- [x] Flashcards: modern AI tools page with tabs, centered study session, flip surface, progress, shuffle, and confidence controls; embedded `FlashcardViewer` keyboard navigation remains intact.
- [x] Quizzes, QuizTaking, and QuizResult: modern generation panel, quiz cards, question/difficulty chips, practice/exam mode styling, timer polish, score cards, answer breakdown, explanations, and retry incorrect flow preserved.
- [x] Settings and AI Provider Settings: provider cards plus dropdown, safe API-key placeholder behavior, separate Ollama `/api/*` and LocalOpenAI `/v1/*` protocol explanations, and LocalOpenAI defaults preserved.

#### Verification - 2026-06-04
- `dotnet build --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test --no-restore` - passed, 26 backend tests.
- `npm test -- --run` in `Learnify.Client` - passed, 6 files / 16 tests.
- `npm run build` in `Learnify.Client` - passed.
- `git diff --check` - passed; only line-ending conversion warnings were printed.
- No backend product features, AI Tutor backend, Study Planner backend, Analytics/Achievements backend, Docker, CI/CD, or deployment work was added in M8.3.

#### Remaining M8.3 / UI Gaps
- [ ] Playwright E2E smoke remains pending; Playwright is not installed/configured.
- [ ] Learning heatmap, AI recommendation, key concepts, analytics, study planner, achievements, and AI Tutor remain UI placeholders or disabled where no backend exists.
- [ ] Real markdown rendering and real PDF text extraction remain future work.

### Milestone 9 - Deployment & DevOps
- [ ] Dockerfile for `Learnify.Web`
- [ ] Dockerfile for `Learnify.Client`
- [ ] `docker-compose.yml` (backend + frontend + SQL Server)
- [ ] GitHub Actions CI/CD pipeline
- [ ] Azure App Service + Static Web Apps deployment
- [ ] Health check endpoint (`/health`)

### Milestone 10 - Documentation & Launch
- [ ] Swagger/OpenAPI via Swashbuckle
- [ ] Getting started guide
- [ ] API reference
- [ ] Architecture Decision Records (ADRs)
- [ ] Video demo

---

## Completed Milestones

| Milestone | Completion Date |
|-----------|----------------|
| Milestone 1 â€” Backend Foundation | 5/22/2026 |
| Milestone 2 â€” Authentication & Authorization | 5/22/2026 |
| Milestone 3 â€” Full-Stack Integration & UI | 5/23/2026 |
| Milestone 4 â€” Background Services | 5/23/2026 |
| Milestone 5 â€” AI Integration (Multi-Provider) | 5/23/2026 |
| Milestone 6 - Smart Learning Core (M6R) | 2026-06-03 |
| Milestone 7 - Quiz Engine core flow | 2026-06-03 |

---

## AI Provider Reference

| Provider | Type | Cost | Default | Model ID | Notes |
|----------|------|------|---------|----------|-------|
| Gemini | Cloud | Free tier | âœ… Yes | `gemini-3.5-flash` *(upgraded M6.3)* | Recommended starting point |
| OpenAI | Cloud | Paid | No | `gpt-4o-mini` | Requires paid API key |
| Claude | Cloud | Paid | No | `claude-sonnet-4-20250514` | Anthropic API key required |
| Ollama | Local | Free | No | configurable (e.g. `llama3`) | Requires local Ollama install |

### Switching Provider (Server-Wide)
```json
// Learnify.Web/appsettings.json
"AiSettings": {
  "ActiveProvider": "Gemini"  // â†’ "OpenAI" | "Claude" | "Ollama"
}
```

### Per-User Provider (After M6.4)
Each user configures their own provider via the Settings page. Server-wide config acts as fallback only.

---

## Security Notes

> âš ï¸ **Never commit API keys to source control.**

```bash
# Local development â€” use user-secrets
dotnet user-secrets set "AiSettings:Gemini:ApiKey" "your-key" --project Learnify.Web
dotnet user-secrets set "AiSettings:OpenAI:ApiKey" "your-key" --project Learnify.Web
dotnet user-secrets set "AiSettings:Claude:ApiKey" "your-key" --project Learnify.Web
```

For production: Azure Key Vault or environment variables injected at deployment time.

---

## Known Issues / Watch Items

| Issue | Status | Notes |
|-------|--------|-------|
| Local LLaMA API compatibility | COMPLETE | Learnify now has separate `Ollama` (`/api/*`) and `LocalOpenAI` (`/v1/*`) providers; `LocalOpenAI` runtime generation passed through `/v1/chat/completions`. |
| Local LLaMA quiz generation | COMPLETE | App-level `LocalOpenAI` quiz generation and generated quiz submission passed with `Qwen3.6-35B-A3B-UD-Q4_K_M.gguf`. |
| PDF extraction partial | PASS_WITH_CAVEAT | `.txt` and `.md` upload persist `Note.Content`; real PDF text extraction is still future work. |
| Advanced quiz features | Future work | Matching, scenario/coding questions, AI hints, weakness analysis, related concepts, adaptive engine, and analytics dashboard are not implemented. |
| NuGet vulnerability warnings | COMPLETE | M8.2 solution-level `dotnet list package --vulnerable --include-transitive` completed successfully with no vulnerable packages reported. |

## M8.4 - UI Runtime Bugfix + AI Action Wiring + New User Data Integrity

- Removed active demo user model seeding from `ApplicationDbContext` and the EF model snapshot.
- Added a cleanup migration for previously seeded learning data so fully migrated databases do not retain starter courses/notes/quizzes for demo seed identities.
- Replaced Dashboard client placeholders with real per-user counts and empty/coming-soon learning metrics for unavailable streak/progress/heatmap data.
- Rewired Note Detail AI actions so Generate Summary, Generate Flashcards, Generate Study Tips, and Generate Quiz call their services directly from the sidebar action buttons.
- Hardened LocalOpenAI response parsing to fail clearly on empty `choices[0].message.content`.
- Updated study tips prompting to use note content, cap prompt size, increase token budget, and reject blank provider responses.
- Made PDF upload/analyze status honest: PDF extraction is not implemented yet, and Smart Upload now accepts text/Markdown for AI analysis instead of sending base64 PDF bytes to the AI provider.
- Added frontend regression coverage for the Note Detail AI action buttons.

## M8.4.1 - Restore Text-Based PDF Extraction

- Restored Smart Upload support for `.pdf` files without reverting the M8.4 safety fix: AI receives extracted text only, never raw PDF base64.
- Added `IPdfTextExtractor` and `PdfTextExtractor` using `PdfPig 0.1.14` for selectable-text PDF extraction.
- Updated `NotesController.AnalyzeAndSave` to accept `.txt`, `.md`, and `.pdf`, extract PDF text server-side, normalize text, and return a clean 400-level error for invalid/scanned/image-only PDFs.
- Saved analyzed PDF notes include an `Extracted PDF Text` section so downstream summary, flashcard, study-tip, and quiz actions can use readable content.
- Updated Smart Upload UI copy and validation to support `.txt`, `.md`, and text-based `.pdf` files while clearly excluding scanned/image-only PDFs.
- Added backend integration tests for `.txt`, `.md`, valid text-based PDF extraction, invalid PDF 400 handling, and ensuring PDF base64 is not saved as `Note.Content`.
- Added frontend SmartUpload tests for PDF selection, supported-file copy, backend extraction error display, and `.md` analyze behavior.

### Verification - 2026-06-05

- `dotnet restore` - passed.
- `dotnet build --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test --no-restore` - passed, 30 backend tests.
- `dotnet list package --vulnerable --include-transitive` - passed; no vulnerable packages reported.
- `npm test -- --run` in `Learnify.Client` - passed, 8 files / 23 tests.
- `npm run build` in `Learnify.Client` - passed.
- Runtime smoke: Learnify.Web started on `http://localhost:5073`, Learnify.Client served `200` from `http://127.0.0.1:5173`, direct `.txt` and `.md` uploads saved notes, generated text-based PDF analysis saved a note containing extracted text and not PDF base64, Gemini summary/flashcards/study tips/quiz succeeded from the saved PDF note, and invalid PDF upload returned `400` with the readable-text unsupported message.
- LocalOpenAI smoke was not run for M8.4.1 because `http://127.0.0.1:8080/v1/models` was previously unreachable in this session.

## M8.5 - Runtime UX + AI Reliability Fixes

- Added `POST /api/notes/upload-file` for simple PDF uploads without AI analysis. The endpoint requires JWT auth, validates course ownership, accepts only `.pdf` files up to 5MB, stores the PDF as a note attachment, and stores the required non-AI placeholder in `Note.Content`.
- Updated Notes UI with a separate "Simple PDF Upload" card and kept Direct Text Upload and AI Upload/Analysis as separate paths.
- File-only PDF notes now show their attachment on Note Detail and disable summary, flashcard, study-tip, and quiz buttons with clear readable-text guidance.
- Improved LocalOpenAI-facing AI reliability by returning provider failures as clear 502 responses, preserving `/v1/chat/completions` parsing, increasing analysis/study-tip output budgets, and making empty/invalid AI outputs fail clearly.
- Study tips now request structured markdown sections: active recall, key concepts, common confusions, memory hooks, mini plan, and self-test questions.
- Auth state now hydrates from localStorage on reload, logout clears auth state, and in-flight refresh responses cannot restore a session after logout.
- Dedicated Flashcards page now surfaces backend errors and keeps generated cards in the Study Session flow.
- Added shared overflow wrapping/scrolling for large textareas and AI output panels.

### Verification - 2026-06-05

- `dotnet build --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test --no-restore` - passed, 34 backend tests.
- `dotnet list package --vulnerable --include-transitive` - passed; no vulnerable packages reported.
- `npm test -- --run` in `Learnify.Client` - passed, 9 files / 25 tests.
- `npm run build` in `Learnify.Client` - passed.

## LocalOpenAI / Qwen Quiz JSON Reliability Follow-up

- Added robust quiz response extraction and schema normalization for local model outputs:
  - raw JSON object or array
  - fenced JSON
  - leading/trailing text
  - `<think>...</think>` reasoning blocks
  - common aliases such as `question`, `choices`, `answer`, and `correct_answer`
- Added strict quiz schema validation so malformed questions fail clearly instead of silently creating bad quizzes.
- Improved LocalOpenAI quiz prompts with JSON-only and `/no_think` instructions for local/Qwen requests.
- Added LocalOpenAI-only adaptive batching for larger quiz requests:
  - requests over 5 questions are generated in smaller batches
  - default local batch size is 4 questions
  - retry once with a smaller 2-question batch after empty content, length-style failures, invalid JSON, or invalid schema
  - merged result must satisfy the requested question count or return a clear “try fewer questions or switch provider” error
- Gemini, Mock, Ollama, quiz scoring, and M9 analytics behavior were not changed.

### Verification

- `dotnet build --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test --no-restore` - passed, 45 backend tests.
- `dotnet list package --vulnerable --include-transitive` - passed; no vulnerable packages reported.
- `npm test -- --run` in `Learnify.Client` - passed, 9 files / 25 tests.
- `npm run build` in `Learnify.Client` - passed.
- LocalOpenAI 3/5/8-question live runtime smoke after batching was not completed in this pass because the command approval environment hit its usage limit before the model probe/API smoke could run.

## LocalOpenAI / Qwen 7-8 Question Quiz Reliability Follow-up

- Added a stronger LocalOpenAI-only fallback for larger quizzes when 4-question batches and 2-question retry batches cannot fill the requested quiz size.
- The fallback generates exactly one question per sequential LocalOpenAI request, parses each response with `QuizResponseParser`, skips duplicate question text, rotates compact note excerpts across attempts, and continues until the requested count is reached or the attempt limit is exhausted.
- The fallback keeps `/no_think`, compact JSON-only prompting, low temperature, small output budgets, and the existing strict validation rules.
- The existing clear failure remains in place if local generation cannot produce enough valid unique questions: `The local model could not generate the requested quiz size. Try fewer questions or switch provider.`
- Gemini, Ollama, Mock, quiz scoring, and M9 analytics behavior were not changed.

### Verification

- `dotnet build --no-restore` - passed with 0 warnings and 0 errors.
- `dotnet test --no-restore` - passed, 48 backend tests.
- `dotnet list package --vulnerable --include-transitive` - passed; no vulnerable packages reported.
- `npm test -- --run` in `Learnify.Client` - passed, 9 files / 25 tests.
- `npm run build` in `Learnify.Client` - passed.
- `git diff --check` - passed; only CRLF conversion warnings were printed.
- Conflict marker scan - no markers found.
- LocalOpenAI live runtime smoke was blocked because `http://127.0.0.1:8080/v1/models` was unreachable in this pass.
