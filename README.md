# LearnifyAI

> An AI-powered personal learning platform — upload your study materials and let AI generate summaries, flashcards, quizzes, and personalized study plans.

[![Backend Build](https://img.shields.io/badge/backend-passing-brightgreen)]()
[![Frontend Build](https://img.shields.io/badge/frontend-passing-brightgreen)]()
[![Milestone](https://img.shields.io/badge/milestone-6R%20verified-brightgreen)]()

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Feature Status](#feature-status)
- [Getting Started](#getting-started)
- [AI Provider Configuration](#ai-provider-configuration)
- [Project Structure](#project-structure)
- [Roadmap](#roadmap)

---

## Overview

LearnifyAI is a full-stack AI-powered learning platform targeting individual learners, university students, professionals, and exam preparation students. Users upload study materials (notes, markdown, PDFs) and AI analyzes the content to generate summaries, flashcards, quizzes, study plans, and personalized learning insights.

---

## Tech Stack

### Frontend (`Learnify.Client`)

| Technology | Purpose |
|---|---|
| React 18 + TypeScript | UI framework |
| Vite | Build tool & dev server |
| Zustand | Global state management |
| Axios | HTTP client with JWT interceptors |
| React Router v7 | Client-side routing |

### Backend (`Learnify.*`)

| Technology | Purpose |
|---|---|
| ASP.NET Core (.NET 8) | Web API framework |
| Entity Framework Core | ORM + migrations |
| SQL Server | Primary database |
| JWT Bearer Auth | Authentication & authorization |
| FluentValidation | Request validation |
| Background Services (Channel\<T\>) | Async notification queue |

### AI Providers

| Provider | Model | Type | Default |
|---|---|---|---|
| Gemini | `gemini-3.5-flash` | Cloud — Free tier | ✅ Yes |
| OpenAI | `gpt-4o-mini` | Cloud — Paid | No |
| Claude | `claude-sonnet-4-20250514` | Cloud — Paid | No |
| Ollama | Configurable (e.g. `llama3`) | Local — Free | No |

---

## Architecture

```
Learnify.Core            → Domain entities, interfaces, models (zero external deps)
Learnify.Application     → DTOs, service interfaces, business logic
Learnify.Infrastructure  → EF Core, repositories, AI providers, email
Learnify.Web             → ASP.NET Core API, controllers, middleware, workers
Learnify.Tests           → xUnit + Moq test suite
Learnify.Client          → React + TypeScript frontend (Vite)
```

---

## Feature Status

> **Legend:** ✅ Complete (FE + BE both done) · 🔄 Partial (one side missing or incomplete) · ❌ Not Started · `N/A` Not Applicable

---

### 🔐 Authentication & User Management

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| User Registration | ✅ | ✅ | ✅ Complete |
| User Login (JWT) | ✅ | ✅ | ✅ Complete |
| JWT Token Refresh | ✅ | ✅ | ✅ Complete |
| Protected Route Guard | N/A | ✅ | ✅ Complete |
| Global Exception Middleware | ✅ | N/A | ✅ Complete |
| Forgot Password | ❌ | ❌ | ❌ Not Started |
| Social Login (OAuth) | ❌ | ❌ | ❌ Not Started |
| Onboarding Flow (5-step wizard) | ❌ | ❌ | ❌ Not Started |

---

### 📚 Course Management

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| List Courses (authenticated user) | ✅ | ✅ | ✅ Complete |
| Create New Course | ✅ | ✅ | ✅ Complete |
| Delete Course (owner-only) | ✅ | ✅ | ✅ Complete |
| Edit / Update Course | ✅ | ✅ | ✅ Complete |
| Course Detail Page | ✅ | ✅ | ✅ Complete |
| Course Search & Filter | ❌ | ❌ | ❌ Not Started |
| Course Sorting | ❌ | ❌ | ❌ Not Started |
| Course Cover Image | ❌ | ❌ | ❌ Not Started |
| Course Progress Indicator | ❌ | ❌ | ❌ Not Started |

---

### 📝 Notes & Content Management

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| Note Storage (basic) | ✅ | ✅ | ✅ Complete |
| Note Detail View | ✅ | ✅ | ✅ Complete |
| Create Note (manual text) | ✅ | ❌ | 🔄 Partial |
| File Upload (.txt, .md) | ✅ | ✅ | ✅ Complete |
| PDF File Upload | 🔄 | ✅ | 🔄 Partial - AI analyze only, no real PDF text extraction |
| Drag-and-Drop Upload UI | 🔄 | 🔄 | 🔄 Partial |
| Markdown Rendering | ❌ | ❌ | ❌ Not Started |
| Note Search | ❌ | ❌ | ❌ Not Started |
| Folder Organization | ❌ | ❌ | ❌ Not Started |
| Note Tags | ❌ | ❌ | ❌ Not Started |

---

### 🤖 AI Features

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| AI Summary Generation | ✅ | ✅ | ✅ Complete |
| AI Flashcard Generation | ✅ | ✅ | ✅ Complete |
| AI Study Tips | ✅ | ✅ | ✅ Complete |
| AI Provider Badge | ✅ | ✅ | ✅ Complete |
| Multi-Provider Support (4 providers) | ✅ | 🔄 | 🔄 Partial |
| Per-User AI Provider Settings | ✅ | ✅ | ✅ Complete |
| AI Quiz Generation | ❌ | ❌ | ❌ Not Started |
| AI Study Plan Generation | ❌ | ❌ | ❌ Not Started |
| AI Important Concepts | ❌ | ❌ | ❌ Not Started |
| AI Exam Questions | ❌ | ❌ | ❌ Not Started |
| Ask AI About This Note | ❌ | ❌ | ❌ Not Started |
| AI Tutor (Chat Interface) | ❌ | ❌ | ❌ Not Started |

---

### 🃏 Flashcard System

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| Basic Flashcard Viewer | ✅ | ✅ | ✅ Complete |
| 3D CSS Flip Animation | N/A | ✅ | ✅ Complete |
| Keyboard Navigation (← / → / Space) | N/A | ✅ | ✅ Complete |
| Progress Indicator (Card N of M) | N/A | ✅ | ✅ Complete |
| Shuffle Mode | N/A | ✅ | ✅ Complete |
| Confidence Rating ("Got it" / "Review") | N/A | ✅ | ✅ Complete |
| Score Tracking per Session | N/A | ✅ | ✅ Complete |
| Spaced Repetition Algorithm | ❌ | ❌ | ❌ Not Started |
| Review History | ❌ | ❌ | ❌ Not Started |
| Difficulty Levels | ❌ | ❌ | ❌ Not Started |

---

### 🧠 Quiz System

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| Multiple Choice Questions | ❌ | ❌ | ❌ Not Started |
| True / False Questions | ❌ | ❌ | ❌ Not Started |
| Fill-in-the-Blank | ❌ | ❌ | ❌ Not Started |
| Short Answer | ❌ | ❌ | ❌ Not Started |
| Matching Questions | ❌ | ❌ | ❌ Not Started |
| Scenario-Based Questions | ❌ | ❌ | ❌ Not Started |
| Coding Questions | ❌ | ❌ | ❌ Not Started |
| Timer Mode | ❌ | ❌ | ❌ Not Started |
| Practice Mode | ❌ | ❌ | ❌ Not Started |
| Exam Mode | ❌ | ❌ | ❌ Not Started |
| Difficulty Selection | ❌ | ❌ | ❌ Not Started |
| Instant Feedback + Explanations | ❌ | ❌ | ❌ Not Started |
| AI-Generated Hints | ❌ | ❌ | ❌ Not Started |
| Score Breakdown | ❌ | ❌ | ❌ Not Started |
| Weakness Analysis | ❌ | ❌ | ❌ Not Started |
| Retry Incorrect Questions | ❌ | ❌ | ❌ Not Started |
| Related Concept Questions | ❌ | ❌ | ❌ Not Started |

---

### 🗓️ Study Planner

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| Calendar View | ❌ | ❌ | ❌ Not Started |
| Daily Task List | ❌ | ❌ | ❌ Not Started |
| Weekly Goals | ❌ | ❌ | ❌ Not Started |
| AI-Generated Study Schedule | ❌ | ❌ | ❌ Not Started |
| Reminders / Notifications | ❌ | ❌ | ❌ Not Started |

---

### 📊 Progress & Analytics

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| Learning Streak Tracking | ❌ | ❌ | ❌ Not Started |
| Quiz Performance Charts | ❌ | ❌ | ❌ Not Started |
| Topic Mastery Visualization | ❌ | ❌ | ❌ Not Started |
| Time Spent Studying | ❌ | ❌ | ❌ Not Started |
| Knowledge Growth Trend | ❌ | ❌ | ❌ Not Started |
| Learning Heatmap | ❌ | ❌ | ❌ Not Started |
| Weekly / Monthly Reports | ❌ | ❌ | ❌ Not Started |
| Course Completion Stats | ❌ | ❌ | ❌ Not Started |

---

### 🏆 Achievement System

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| XP Points System | ❌ | ❌ | ❌ Not Started |
| User Levels | ❌ | ❌ | ❌ Not Started |
| Badges | ❌ | ❌ | ❌ Not Started |
| Milestones | ❌ | ❌ | ❌ Not Started |
| Streak Rewards | ❌ | ❌ | ❌ Not Started |
| Learning Challenges | ❌ | ❌ | ❌ Not Started |

---

### 🏠 Dashboard

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| Dashboard Page (`/dashboard`) | N/A | ✅ | ✅ Complete |
| Welcome Message + User Name | N/A | ✅ | ✅ Complete |
| Daily Learning Streak Widget | ❌ | ❌ | ❌ Not Started |
| Learning Statistics Cards | ❌ | ❌ | ❌ Not Started |
| Study Progress Bars | ❌ | ❌ | ❌ Not Started |
| Recent Notes Widget | ❌ | ❌ | ❌ Not Started |
| Upcoming Study Sessions Widget | ❌ | ❌ | ❌ Not Started |
| AI Recommendations Widget | ❌ | ❌ | ❌ Not Started |
| Quick Actions Panel | N/A | ✅ | ✅ Complete |
| Recently Generated Quizzes | ❌ | ❌ | ❌ Not Started |
| Flashcard Performance Widget | ❌ | ❌ | ❌ Not Started |
| Weekly Learning Graph | ❌ | ❌ | ❌ Not Started |
| Achievement Cards | ❌ | ❌ | ❌ Not Started |

---

### ⚙️ Settings Page

| Feature | Backend | Frontend | Overall |
|---|---|---|---|
| Profile Settings (name, email) | ❌ | ❌ | ❌ Not Started |
| Avatar / Profile Picture | ❌ | ❌ | ❌ Not Started |
| AI Provider Selection | ✅ | ✅ | ✅ Complete |
| API Key Management (masked) | ✅ | ✅ | ✅ Complete |
| Ollama Base URL Field | ✅ | ✅ | ✅ Complete |
| Theme Toggle (Light / Dark) | ❌ | ❌ | ❌ Not Started |
| Notification Settings | ❌ | ❌ | ❌ Not Started |
| Security Settings (change password) | ❌ | ❌ | ❌ Not Started |

---

### 🏗️ Infrastructure & DevOps

| Feature | Status |
|---|---|
| Email Notification Queue (Channel\<T\>) | ✅ Complete |
| Background Worker (HostedService) | ✅ Complete |
| Global Exception Middleware | ✅ Complete |
| CORS Policy | ✅ Complete |
| Standardized API Response Wrapper | ✅ Complete |
| EF Core Migrations | ✅ Complete |
| Unit Tests (NotificationWorker — 7 passing) | ✅ Complete |
| Swagger / OpenAPI docs | ❌ Not Started |
| Dark Mode / Light Mode | ❌ Not Started |
| Responsive Layout (Mobile + Tablet) | ❌ Not Started |
| Loading Skeleton States | ❌ Not Started |
| Toast Notification System | ❌ Not Started |
| Global Sidebar Navigation | ❌ Not Started |
| Docker / docker-compose | ❌ Not Started |
| CI/CD Pipeline | ❌ Not Started |

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- SQL Server (local or Docker)

### Backend Setup

```bash
cd Learnify.Web
dotnet user-secrets set "AiSettings:Gemini:ApiKey" "your-key"
dotnet ef database update
dotnet run
```

### Frontend Setup

```bash
cd Learnify.Client
npm install
cp .env.example .env   # Set VITE_API_BASE_URL=http://localhost:5xxx
npm run dev
```

---

## AI Provider Configuration

Configured in `Learnify.Web/appsettings.json`:

```json
"AiSettings": {
  "ActiveProvider": "Gemini",
  "Gemini":  { "ApiKey": "", "Model": "gemini-3.5-flash" },
  "OpenAI":  { "ApiKey": "", "Model": "gpt-4o-mini" },
  "Claude":  { "ApiKey": "", "Model": "claude-sonnet-4-20250514" },
  "Ollama":  { "BaseUrl": "http://127.0.0.1:8080", "Model": "llama3" }
}
```

> ⚠️ **Never commit API keys to source control.** Use `dotnet user-secrets` locally and Azure Key Vault / environment variables in production.

---

## Project Structure

```
LearnifyAI/
├── Learnify.Core/              # Domain entities, interfaces, models
├── Learnify.Application/       # DTOs, service interfaces, business logic
├── Learnify.Infrastructure/    # EF Core, repositories, AI providers
├── Learnify.Web/               # ASP.NET Core API, controllers, middleware
├── Learnify.Tests/             # xUnit test suite
└── Learnify.Client/            # React + TypeScript frontend
    ├── src/
    │   ├── components/         # Reusable UI components
    │   │   └── AI/             # AI-specific components
    │   ├── pages/              # Page-level route components
    │   ├── services/           # Axios API service layer
    │   ├── store/              # Zustand state stores
    │   └── App.tsx             # Router + route definitions
    └── vite.config.ts
```

---

## Roadmap

| Milestone | Focus | Status |
|---|---|---|
| M1 — Backend Foundation | Entities, repositories, Unit of Work | ✅ Done |
| M2 — Auth & Security | JWT, middleware, FluentValidation | ✅ Done |
| M3 — Full-Stack Integration | Frontend scaffolding, Axios, routing | ✅ Done |
| M4 — Background Services | Email queue, HostedService | ✅ Done |
| M5 — AI Integration | Multi-provider AI, summary, flashcards | ✅ Done |
| M6 — Smart Learning Core | Course CRUD, `.txt/.md` upload, per-user settings, flashcard polish | ✅ M6R verified; PDF extraction remains future work |
| M7 — Quiz & Flashcard System | Full quiz engine, advanced flashcards | ⏳ Planned |
| M8 — AI Tutor & Study Planner | Chat interface, calendar scheduler | ⏳ Planned |
| M9 — Analytics & Achievements | Progress tracking, gamification | ⏳ Planned |
| M10 — Dashboard & UI Polish | Full dashboard, dark mode, animations | ⏳ Planned |
| M11 — Testing & QA | Unit, integration, E2E (Playwright) | ⏳ Planned |
| M12 — Deployment & DevOps | Docker, CI/CD, Azure App Service | ⏳ Planned |
| M13 — Docs & Launch | Swagger, ADRs, getting started guide | ⏳ Planned |
