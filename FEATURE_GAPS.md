# Feature Gaps

Last updated: 2026-06-05

## M6R Smart Learning Core Status

| Area | Status | Notes |
|------|--------|-------|
| New user empty state | Complete | Runtime audit confirmed a new user receives 0 courses and the frontend empty-state text is correct. |
| Course management | Complete | Runtime audit confirmed create, edit, detail, and delete flows through the protected API. |
| Per-user AI settings | Complete | Runtime audit confirmed persistent GET/PUT, safe key responses, Gemini restore, and Ollama URL `http://127.0.0.1:8080`. |
| AI provider badge/settings UI | Complete | Static inspection confirms protected Settings route, save UI, local connection test, and global protected nav badge. |
| Text note upload | Complete | Runtime audit confirmed `.txt` and `.md` uploads persist `Note.Content`. |
| Flashcard viewer polish | Complete | Static inspection confirms flip, previous/next, keyboard navigation, shuffle, confidence buttons, and session score tracking. |
| PDF text extraction | Partial | Text-based PDF extraction is supported through server-side PdfPig extraction. Scanned/image-only PDFs and OCR are not supported yet. |
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

Latest M7 re-verification on 2026-06-05 passed with Gemini default `gemini-3.5-flash`: generated quiz, saved quiz retrieval, attempt scoring, attempt listing, and cross-user 404 were confirmed through the running API. Local LLaMA was not re-verified because `http://127.0.0.1:8080/v1/models` was unreachable.

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

## M9 Analytics & Achievements Status

| Area | Status | Notes |
|------|--------|-------|
| Learning activity tracking | Complete | Tracks real authenticated user activity for course creation, note/PDF upload, AI study actions, quiz generation, and quiz attempts. |
| Dashboard analytics API | Complete | `/api/analytics/dashboard` returns real per-user totals, XP, streaks, quiz scores, achievements, and recent activity. Fresh users receive zeros and empty activity. |
| Quiz performance API | Complete | `/api/analytics/quiz-performance` returns real attempts, recent scores, and per-quiz summaries scoped to the current user. |
| Achievements API | Complete | `/api/achievements` returns static achievement definitions with per-user lock/unlock state and progress. No fake users or demo learning data were added. |
| Analytics frontend | Complete | Dashboard now uses backend analytics; protected `/analytics` page shows stats, quiz performance, and recent activity. |
| Achievements frontend | Complete | Protected `/achievements` page shows locked/unlocked achievement cards and progress. |
| Cross-user isolation | Complete | Integration tests verify one user's activity does not appear in another user's analytics. |
| Advanced analytics | Future work | Time spent studying, topic mastery, heatmap charts, trend reports, adaptive recommendations, and levels remain future work. |

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
| Notes polish | Complete with caveats | Notes list and detail now use folders/tags UI, upload dropzone styling, note cards, right-side AI panel, and placeholder concept caveats. Text-based PDF extraction is supported; OCR remains future work. |
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
- Advanced analytics charts, topic mastery, reports, and levels
- AI Tutor backend
- Playwright E2E smoke
- DevOps, Docker, and CI/CD
## M8.4 Status Update

- New-user dashboard integrity: fixed. Dashboard totals are loaded from the authenticated user's existing endpoints, and unavailable streak/progress/heatmap data displays as empty/coming soon instead of fake progress.
- Demo seed data: fixed for the active model and cleanup migration. Existing databases should apply `20260604190000_RemoveDemoLearningSeed` to remove the prior demo learning records.
- Note Detail AI actions: fixed. Sidebar buttons now call summary, flashcard, study-tip, and quiz generation services directly.
- LocalOpenAI study tips: improved. The provider now rejects empty message content and study tips use note content with a larger token budget.
- PDF text extraction: partial but functional for text-based PDFs. Smart Upload accepts `.txt`, `.md`, and selectable-text `.pdf`; scanned/image-only PDFs return a clean unsupported-readable-text error.

## M8.4.1 Status Update

- Restored Smart Upload PDF analysis with real server-side text extraction using `PdfPig 0.1.14`.
- AI analysis receives extracted PDF text only; raw PDF bytes/base64 are not sent as AI input.
- Saved analyzed PDF notes include the extracted PDF text section so summary, flashcard, study tips, and quiz flows can use readable content.
- Scanned/image-only or invalid PDFs return a clean 400-level error. OCR remains future work.
- Playwright/browser verification: not completed in this pass because command escalation was blocked by the environment usage limit.

## M8.5 Runtime UX + AI Reliability Status Update

- Simple PDF upload without AI analysis is complete: `POST /api/notes/upload-file` stores file-only PDFs as note attachments, uses the required non-AI placeholder content, validates auth/course ownership/file type/size, and does not call AI.
- File-only PDF Note Detail UX is complete: attachments are visible and AI actions are disabled with readable-text guidance.
- LocalOpenAI reliability is improved: Learnify keeps LocalOpenAI on `/v1/chat/completions`, surfaces provider failures as clear 502 responses, rejects empty content, and uses stricter prompts/output budgets for analysis and study tips.
- Study tips output is improved: prompts request the required structured markdown sections and frontend rendering preserves line breaks.
- Auth reload/logout behavior is improved: auth store hydrates from localStorage and stale refresh responses cannot restore a logged-out session.
- Dedicated Flashcards generation is improved: backend errors are shown and generated cards remain in the Study Session flow.
- Large text overflow is improved across textareas, note content, flashcards, and AI output panels.

## LocalOpenAI / Qwen Quiz Reliability Follow-up

- Implemented adaptive LocalOpenAI quiz batching for larger quiz requests: requests above 5 questions are generated in smaller batches, parsed through the robust quiz parser, merged, and retried once with 2-question batches on local-model output failures.
- Added `/no_think` JSON-only prompting for LocalOpenAI quiz generation while leaving Gemini/Ollama/Mock behavior separate.
- Automated tests cover 8-question batch merging, empty-content retry, invalid-batch failure, parser robustness, Mock compatibility, existing quiz scoring, and M9 analytics regressions.
- Remaining caveat: the post-fix live 8-question Qwen runtime smoke still needs confirmation because the local command approval environment hit its usage limit before the runtime model/API smoke could run.
- Follow-up reliability fix adds a sequential one-question fallback for LocalOpenAI 7- and 8-question quizzes after batch generation cannot fill the requested size. Automated tests cover 7-question fallback, 8-question fallback, duplicate skipping, and max-attempt failure.
- Remaining caveat: live 7/8-question Qwen runtime smoke still needs confirmation because `http://127.0.0.1:8080/v1/models` was unreachable during the latest verification pass.
