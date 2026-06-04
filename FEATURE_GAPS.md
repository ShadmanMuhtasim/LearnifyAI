# Feature Gaps

Last updated: 2026-06-04

## M6R Smart Learning Core Status

| Area | Status | Notes |
|------|--------|-------|
| New user empty state | Complete | Runtime audit confirmed a new user receives 0 courses and the frontend empty-state text is correct. |
| Course management | Complete | Runtime audit confirmed create, edit, detail, and delete flows through the protected API. |
| Per-user AI settings | Complete | Runtime audit confirmed persistent GET/PUT, safe key responses, Gemini restore, and Ollama URL `http://127.0.0.1:8080`. |
| AI provider badge/settings UI | Complete | Static inspection confirms protected Settings route, save UI, local connection test, and global protected nav badge. |
| Text note upload | Complete | Runtime audit confirmed `.txt` and `.md` uploads persist `Note.Content`. |
| Flashcard viewer polish | Complete | Static inspection confirms flip, previous/next, keyboard navigation, shuffle, confidence buttons, and session score tracking. |
| PDF text extraction | Partial | Upload analysis can store PDF/base64 context, but real PDF text extraction is not implemented. |
| Local LLaMA generation | Complete | Learnify supports separate `Ollama` (`/api/*`) and `LocalOpenAI` (`/v1/*`) providers. Runtime verification passed through `LocalOpenAI` for provider test, summarize, flashcards, quiz generation, and quiz submission. |

## M7 Quiz Engine Status

| Area | Status | Notes |
|------|--------|-------|
| Quiz domain and persistence | Complete | Runtime audit applied `AddQuizEngine` migration and created `Quizzes`, `Questions`, `QuizAttempts`, and `QuizAttemptAnswers`. |
| AI quiz generation | Complete | Runtime audit generated a Gemini quiz from a saved note with populated question text, correct answers, options where applicable, and explanations. |
| Supported question types | Complete | Multiple choice, true/false, short answer, and fill-in-the-blank are implemented and runtime verified with Gemini. |
| Quiz list/detail API | Complete | Runtime audit confirmed saved quiz listing and protected quiz detail retrieval. |
| Quiz attempts and scoring | Complete | Runtime audit submitted an attempt and confirmed score, percentage, answer correctness, correct answers, and explanations. |
| Practice/exam modes | Complete | Exam mode loads quizzes without answers; practice mode loads answers/explanations for immediate local feedback. |
| Timer mode | Complete | Optional `timeLimitMinutes` is persisted and the quiz-taking UI shows a disabled-by-default countdown that auto-submits when feasible. |
| Retry incorrect questions | Complete | Result UI supports a frontend retry session for incorrect answers. |
| Quiz frontend flow | Complete | `/quizzes`, `/quizzes/:id`, and `/quizzes/:id/result` compile and are protected routes with generation, taking, modes, timer, retry, and result UI. |
| Cross-user quiz access protection | Complete | Runtime audit confirmed another user receives 404 for another user's quiz. |
| Local LLaMA quiz generation | Complete | Runtime verification generated a quiz through `LocalOpenAI` with `Qwen3.6-35B-A3B-UD-Q4_K_M.gguf` and submitted the generated quiz attempt successfully. |

## M8 Testing & QA Status

| Area | Status | Notes |
|------|--------|-------|
| Backend automated tests | Complete for M8.2 | Added provider protocol tests, quiz service tests, and controller integration tests. `dotnet test --no-restore` passes 26 tests. |
| Local provider test strategy | Complete | Automated `LocalOpenAI` and `Ollama` tests use mocks/fakes and do not require `http://127.0.0.1:8080` to be running. Real local LLM checks are optional runtime smoke only. |
| Frontend automated tests | Complete for M8.2 | Added Vitest/React Testing Library coverage for LocalOpenAI settings, provider badge, Courses, Settings, QuizTaking, and QuizResult. `npm test -- --run` passes 6 files / 16 tests. |
| Build verification | Complete | `dotnet build --no-restore`, `dotnet test --no-restore`, `npm test -- --run`, and `npm run build` passed on 2026-06-04. |
| Secret hygiene | Complete | Common API-key pattern scan found no committed cloud AI keys; `.gitignore` now excludes local env files, secrets, logs, coverage, dist, and node_modules. |
| Dependency vulnerability audit | Complete for M8.2 | Removed vulnerable AutoMapper extension and scan-blocking FluentValidation.AspNetCore/Moq transitive dependencies. Solution-level `dotnet list package --vulnerable --include-transitive` passes with no vulnerable packages reported. |
| UI/UX polish | Complete for M8.3 | Added shared app shell, reusable UI primitives, responsive design tokens, polished Dashboard/Courses/Notes/Flashcards/Quizzes/Settings pages, and preserved existing frontend tests. No backend product features were added. |
| E2E coverage | Not Started | Playwright flow for register -> create course -> add note -> generate quiz -> submit quiz remains future work. |

## M8.1 Integration Tests & Vulnerability Cleanup

| Area | Status | Notes |
|------|--------|-------|
| Backend integration tests | Complete for M8.2 | Added WebApplicationFactory tests with in-memory EF and fake AI service for Auth, AiController, UserAiSettingsController, and QuizzesController. `dotnet test --no-restore` passes 26 backend tests. |
| Frontend page tests | Complete for M8.1 | Added practical page tests for Courses, Settings, and QuizTaking. Existing QuizResult tests still pass. `npm test -- --run` passes 6 files / 16 tests. |
| Local provider test strategy | Complete | Automated tests still use mocks/fakes and do not require Gemini, OpenAI, Claude, Ollama, or LocalOpenAI to be reachable. |
| Vulnerability cleanup | Complete for M8.2 | Safe patch updates plus the M8.2 AutoMapper major migration removed the known vulnerable package warnings. |
| Vulnerability scan tooling | Complete for M8.2 | Removed the scan-blocking `FluentValidation.AspNetCore` and `Castle.Core` transitive dependencies; solution-level vulnerability scan now completes successfully. |
| Runtime smoke | Complete for M8.2 | Auth, course CRUD, `.txt/.md` upload, Gemini summarize/flashcards/quiz, LocalOpenAI provider test, LocalOpenAI summarize/flashcards/quiz generation, and quiz submit passed. |
| Playwright E2E | Not Started | Playwright is not installed/configured; remains future M8 work. |

## M8.3 Modern UI/UX Polish

| Area | Status | Notes |
|------|--------|-------|
| Shared authenticated layout | Complete | Responsive sidebar/topbar shell, active route highlighting, AI provider badge, user/logout area, disabled future navigation items. |
| Reusable UI system | Complete | Added lightweight primitives and global tokens/classes for cards, buttons, badges, forms, stat cards, empty/loading/error states, and responsive grids. |
| Dashboard polish | Complete with caveats | Uses real course count plus quick actions and clearly labeled placeholder heatmap/recommendation widgets. No backend analytics were added. |
| Courses polish | Complete | Modern course cards, frontend-only search/filter chips, create/edit/delete/detail flows preserved, exact empty-state message preserved. |
| Notes polish | Complete with caveats | Notes list and detail now use folders/tags UI, upload dropzone styling, note cards, right-side AI panel, and placeholder concept caveats. PDF extraction remains partial. |
| Flashcards polish | Complete | Standalone AI tools page and existing embedded viewer remain functional with flip/progress/shuffle/confidence controls and keyboard navigation. |
| Quizzes polish | Complete | Generation, taking, timer/mode UI, results, explanations, and retry incorrect flow modernized without scoring changes. |
| Settings polish | Complete | Provider cards/dropdown, safe API-key behavior, separate Ollama `/api/*` and LocalOpenAI `/v1/*` protocol copy, LocalOpenAI defaults preserved. |
| Verification | Complete for frontend | `npm test -- --run` and `npm run build` passed on 2026-06-04. Backend verification remains unchanged from M8.2 unless rerun. |

## Remaining Quiz Gaps

- Matching questions
- Scenario-based questions
- Coding questions
- AI-generated hints
- Weakness analysis
- Related concept questions
- Adaptive quiz engine
- Quiz analytics dashboard

## Still Future Work

- Study planner
- Analytics and achievements
- Backend analytics for dashboard widgets
- AI Tutor backend
- Playwright E2E smoke
- DevOps, Docker, and CI/CD
