# Feature Gaps

Last updated: 2026-06-03

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
| Local LLaMA generation | Caveat | Ollama/local LLaMA remains configured at `http://127.0.0.1:8080`; audit found the local server offline. |

## Still Future Work

- Quiz system
- Study planner
- Analytics and achievements
- Full dashboard expansion
- DevOps, Docker, and CI/CD
