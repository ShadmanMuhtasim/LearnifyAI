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
| Backend automated tests | Partial | Added provider protocol and quiz service coverage. Solution-level `dotnet test --no-restore` passes with 18 tests. Controller integration tests remain future work. |
| Local provider test strategy | Complete | Automated `LocalOpenAI` and `Ollama` tests use mocks/fakes and do not require `http://127.0.0.1:8080` to be running. Real local LLM checks are optional runtime smoke only. |
| Frontend automated tests | Partial | Added Vitest/React Testing Library coverage for LocalOpenAI settings, provider badge, and quiz result retry/explanations. Quiz-taking timer and broader page tests remain future work. |
| Build verification | Complete | `dotnet build --no-restore`, `dotnet test --no-restore`, `npm test -- --run`, and `npm run build` passed on 2026-06-04. |
| Secret hygiene | Complete | Common API-key pattern scan found no committed cloud AI keys; `.gitignore` now excludes local env files, secrets, logs, coverage, dist, and node_modules. |
| Dependency vulnerability audit | Partial | Existing NuGet vulnerabilities remain. Solution-level and some project-level scans are blocked by a NuGet cache/version parsing issue; Application and Infrastructure scans completed and documented affected transitive packages. |
| E2E coverage | Not Started | Playwright flow for register -> create course -> add note -> generate quiz -> submit quiz remains future work. |

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
- Full dashboard expansion
- DevOps, Docker, and CI/CD
