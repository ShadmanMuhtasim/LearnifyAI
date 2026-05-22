# LearnifyAI

An AI-powered learning platform for course management and intelligent note-taking, built with .NET 8 and Clean Architecture.

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/shadmanmuhtasim/LearnifyAI/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)

**Project Status:** 🟡 *Milestone 1 Complete — Infrastructure Layer Ready*

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Milestone Roadmap](#milestone-roadmap)
  - [Milestone 1: Foundation & Infrastructure](#milestone-1-foundation--infrastructure)
    - [1.1 Solution Scaffolding](#11-solution-scaffolding)
    - [1.2 Domain Entities](#12-domain-entities)
    - [1.3 Database Infrastructure](#13-database-infrastructure)
    - [1.4 Repositories & Unit of Work](#14-repositories--unit-of-work)
    - [1.5 API Controllers & DTOs](#15-api-controllers--dtos)
    - [1.6 Middleware & Validation](#16-middleware--validation)
    - [1.7 Seed Data & Migration](#17-seed-data--migration)
    - [1.8 Testing & Verification](#18-testing--verification)
  - [Milestone 2: Authentication & Authorization](#milestone-2-authentication--authorization)
  - [Milestone 3: Application Services](#milestone-3-application-services)
  - [Milestone 4: Background Services](#milestone-4-background-services)
  - [Milestone 5: Frontend Integration](#milestone-5-frontend-integration)
  - [Milestone 6: AI Integration](#milestone-6-ai-integration)
  - [Milestone 7: Testing & QA](#milestone-7-testing--qa)
  - [Milestone 8: Deployment & DevOps](#milestone-8-deployment--devops)
  - [Milestone 9: Documentation & Launch](#milestone-9-documentation--launch)
- [Installation & Setup](#installation--setup)
- [Project Structure](#project-structure)
- [License](#license)
- [Author](#author)

---

## Overview

LearnifyAI is a comprehensive learning management system designed to help students and teachers manage courses, take intelligent notes, and leverage AI-powered features for an enhanced learning experience. The application follows **Clean Architecture** principles to ensure separation of concerns, testability, and long-term maintainability.

### Key Features (Planned)
- Course creation and management for teachers
- Intuitive course enrollment and progress tracking for students
- AI-powered note summarization and flashcard generation
- Role-based access control (Student / Teacher)
- RESTful API with JWT authentication
- Responsive frontend with real-time notifications

---

## Architecture

LearnifyAI follows **Clean Architecture** (also known as Onion Architecture), organizing the codebase into concentric layers where each layer depends only on the layers inward toward it.

```
┌─────────────────────────────────────────────────────────┐
│                   LearnPlatform.Web                      │
│            (ASP.NET Core MVC — Presentation)             │
│   Controllers, Views, wwwroot, Models                    │
└─────────────┬───────────────────────┬───────────────────┘
              │                       │
              ▼                       ▼
┌─────────────────────────┐ ┌─────────────────────────────┐
│  LearnPlatform.Application│ │ LearnPlatform.Infrastructure│
│   (Application Layer)    │ │   (Infrastructure Layer)    │
│   Interfaces, DTOs,      │ │   EF Core, Repositories,    │
│   Service Contracts      │ │   Data Access, UnitOfWork   │
└─────────────┬────────────┘ └─────────────┬───────────────┘
              │                             │
              ▼                             │
┌───────────────────────────────────────────┐│
│            LearnPlatform.Core             ││
│            (Domain Layer)                 ││
│   Entities, Repository Interfaces,        ││
│   Domain Services                         ││
└───────────────────────────────────────────┘│
                                             │
                                     ┌───────┘
                                     │
                          ┌─────────────────┐
                          │  SQL Server     │
                          │  (Database)     │
                          └─────────────────┘
```

### Why Clean Architecture?

| Benefit | Description |
|---------|-------------|
| **Separation of Concerns** | Each layer has a single, well-defined responsibility |
| **Testability** | Core entities and interfaces can be unit-tested without infrastructure dependencies |
| **Framework Independence** | The domain logic is decoupled from ASP.NET Core and EF Core |
| **Maintainability** | Changes in one layer do not cascade to unrelated layers |
| **Scalability** | New features can be added by extending individual layers |

### Dependency Rules

```
LearnPlatform.Web  →  LearnPlatform.Application  →  LearnPlatform.Core
       ↓                                      ↗
       └────────────→ LearnPlatform.Infrastructure
```

- **Core** has zero dependencies on any other project
- **Application** depends only on **Core**
- **Infrastructure** depends on **Core** and **Application**
- **Web** depends on **Application** and **Infrastructure**

---

## Technology Stack

| Category | Technology |
|----------|-----------|
| **Runtime** | .NET 8 (C# 12) |
| **Framework** | ASP.NET Core MVC |
| **ORM** | Entity Framework Core 8 |
| **Database** | Microsoft SQL Server |
| **Authentication** | JWT Bearer Tokens (planned) |
| **Architecture Pattern** | Clean Architecture, Repository Pattern, Unit of Work |
| **API** | RESTful Web API |
| **Frontend** | Razor Views, Bootstrap (planned) |
| **AI Integration** | OpenAI API / Azure AI (planned) |

---

## Milestone Roadmap

| Milestone | Status | Description |
|-----------|--------|-------------|
| **1.1** Solution Scaffolding | ✅ Completed | Clean Architecture solution with 4 projects |
| **1.2** Domain Entities | ✅ Completed | BaseEntity, User, Course, Note models |
| **1.3** Database Infrastructure | ✅ Completed | ApplicationDbContext, EF Core configuration |
| **1.4** Repositories & Unit of Work | ✅ Completed | Generic + specialized repositories |
| **1.5** API Controllers & DTOs | 📋 Planned | CRUD endpoints for all entities |
| **1.6** Middleware & Validation | 📋 Planned | Error handling, validation filters |
| **1.7** Seed Data & Migration | 📋 Planned | Initial data seeding, EF migrations |
| **1.8** Testing & Verification | 📋 Planned | Unit & integration tests |
| **2** Authentication & Authorization | 📋 Planned | JWT, role-based access |
| **3** Application Services | 📋 Planned | Business logic layer |
| **4** Background Services | 📋 Planned | Notifications, AI processing |
| **5** Frontend Integration | 📋 Planned | UI/UX implementation |
| **6** AI Integration | 📋 Planned | Summarization, flashcards |
| **7** Testing & QA | 📋 Planned | E2E, load testing |
| **8** Deployment & DevOps | 📋 Planned | CI/CD, containerization |
| **9** Documentation & Launch | 📋 Planned | API docs, user guides |

---

### Milestone 1: Foundation & Infrastructure

#### 1.1 Solution Scaffolding
**Status:** ✅ Completed

Created the Clean Architecture solution with four distinct projects:
- `LearnPlatform.Core` — Domain layer (entities, interfaces)
- `LearnPlatform.Application` — Application layer (DTOs, service contracts)
- `LearnPlatform.Infrastructure` — Infrastructure layer (EF Core, repositories)
- `LearnPlatform.Web` — Presentation layer (MVC web application)

**Files Created:**
- `LearnPlatform.sln` — Solution file
- Individual `.csproj` files for each project with correct target framework (`net8.0`)

#### 1.2 Domain Entities
**Status:** ✅ Completed

Implemented the core domain model with proper inheritance and relationships:

| Entity | Key Properties | Relationships |
|--------|---------------|---------------|
| **BaseEntity** | `Id` (Guid), `CreatedAt` (DateTime), `UpdatedAt` (DateTime) | Base class for all entities |
| **User** | `FullName`, `Email` (unique), `PasswordHash`, `Role` (Student/Teacher) | Has many Courses |
| **Course** | `Title`, `Description`, `UserId` (FK) | Belongs to User, Has many Notes |
| **Note** | `Content`, `CourseId` (FK) | Belongs to Course |

**Files:**
- `LearnPlatform.Core/Entities/BaseEntity.cs`
- `LearnPlatform.Core/Entities/User.cs`
- `LearnPlatform.Core/Entities/Course.cs`
- `LearnPlatform.Core/Entities/Note.cs`

#### 1.3 Database Infrastructure
**Status:** ✅ Completed

Configured Entity Framework Core with SQL Server:

- `ApplicationDbContext` in `LearnPlatform.Infrastructure/Data/ApplicationDbContext.cs`
- `DbSet<User>`, `DbSet<Course>`, `DbSet<Note>` properties
- Entity configurations in `OnModelCreating` with:
  - Unique constraint on `User.Email`
  - Cascade delete on `User → Courses` and `Course → Notes`
  - Property length constraints and default values
- SQL Server connection string in `appsettings.json`
- DbContext registered in `Program.cs` with dependency injection

#### 1.4 Repositories & Unit of Work
**Status:** ✅ Completed

Implemented the Repository and Unit of Work patterns:

| Component | Description |
|-----------|-------------|
| **`IRepository<T>`** | Generic interface with `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ToListAsync` |
| **`EfRepository<T>`** | Generic EF Core implementation of `IRepository<T>` |
| **`IUserRepository`** | Extends `IRepository<User>` with `FindByEmailAsync`, `EmailExistsAsync`, `FindByRoleAsync` |
| **`ICourseRepository`** | Extends `IRepository<Course>` with `FindByUserIdAsync`, `SearchByTitleAsync`, `CountByUserIdAsync` |
| **`INoteRepository`** | Extends `IRepository<Note>` with `FindByCourseIdAsync`, `FindByUserIdAsync`, `CountByCourseIdAsync` |
| **`IUnitOfWork`** | Coordinates repositories with `SaveChangesAsync` and transaction support |
| **`UnitOfWork`** | Concrete implementation managing `ApplicationDbContext` and all repository instances |

All services registered as **scoped** in `Program.cs` for proper request lifecycle management.

#### 1.5 API Controllers & DTOs
**Status:** 📋 Planned

- Create `UserController`, `CourseController`, `NoteController` with full CRUD endpoints
- Define DTOs for request/response mapping
- Implement proper HTTP status codes (200, 201, 400, 404, 500)

#### 1.6 Middleware & Validation
**Status:** 📋 Planned

- Global exception handling middleware
- Request validation filters
- Custom error response format

#### 1.7 Seed Data & Migration
**Status:** 📋 Planned

- Initial database seed data (sample users, courses, notes)
- EF Core migration scripts
- Seed data generation via `DbInitializer` middleware

#### 1.8 Testing & Verification
**Status:** 📋 Planned

- Unit tests for repository operations
- Integration tests for API endpoints
- Database context tests

---

### Milestone 2: Authentication & Authorization
**Status:** 📋 Planned

- JWT token generation and validation
- Password hashing with BCrypt
- Role-based authorization (Student / Teacher)
- Login, Register, and Refresh Token endpoints

---

### Milestone 3: Application Services
**Status:** 📋 Planned

- Service layer interfaces in `LearnPlatform.Application`
- Business logic for course enrollment, note management
- AutoMapper for DTO mapping

---

### Milestone 4: Background Services
**Status:** 📋 Planned

- `BackgroundService` for periodic notifications
- AI processing queue for note summarization
- Email notification service

---

### Milestone 5: Frontend Integration
**Status:** 📋 Planned

- Razor Pages / MVC views for course and note management
- Bootstrap 5 styling
- AJAX-powered API calls
- Responsive design

---

### Milestone 6: AI Integration
**Status:** 📋 Planned

- OpenAI API integration for note summarization
- AI-powered flashcard generation
- Course content recommendations

---

### Milestone 7: Testing & QA
**Status:** 📋 Planned

- End-to-end testing with Playwright
- Load testing with k6
- Code coverage reporting

---

### Milestone 8: Deployment & DevOps
**Status:** 📋 Planned

- Docker containerization
- CI/CD pipeline (GitHub Actions / Azure DevOps)
- Azure App Service deployment
- Database backup strategy

---

### Milestone 9: Documentation & Launch
**Status:** 📋 Planned

- Swagger/OpenAPI documentation
- User documentation
- API reference guide
- Deployment checklist

---

## Installation & Setup

### Prerequisites

Before running this project, ensure you have the following installed:

| Requirement | Minimum Version |
|-------------|----------------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0.x |
| [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) | 2019 or later / Express |
| [Entity Framework Core Tools](https://learn.microsoft.com/ef/core/cli/dotnet/) | 8.0.x |
| [Visual Studio](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) | Latest |

### How to Run

```bash
# 1. Clone the repository
git clone https://github.com/shadmanmuhtasim/LearnifyAI.git
cd LearnifyAI

# 2. Restore NuGet packages
dotnet restore

# 3. Update connection string in LearnPlatform.Web/appsettings.json
#    Set your SQL Server connection string:
#    "DefaultConnection": "Server=...;Database=LearnifyAI;Trusted_Connection=True;TrustServerCertificate=True;"

# 4. Apply database migrations
dotnet ef database update --project LearnPlatform.Infrastructure --startup-project LearnPlatform.Web

# 5. Run the application
dotnet run --project LearnPlatform.Web

# 6. Open in browser
#    https://localhost:5001
```

### Building from Source

```bash
# Build the entire solution
dotnet build LearnPlatform.sln

# Build without restoring (fast rebuild)
dotnet build --no-restore

# Publish for production
dotnet publish LearnPlatform.Web -c Release -o ./publish
```

---

## Project Structure

```
LearnifyAI/
├── LearnPlatform.sln                          ← Solution file
├── README.md                                  ← This file
├── PROJECT.md                                 ← Detailed architecture doc
│
├── LearnPlatform.Core/                        ← Domain Layer
│   ├── Entities/
│   │   ├── BaseEntity.cs                      ← Base entity (Id, CreatedAt, UpdatedAt)
│   │   ├── User.cs                            ← User entity (Student/Teacher)
│   │   ├── Course.cs                          ← Course entity
│   │   └── Note.cs                            ← Note entity
│   └── Interfaces/
│       ├── IRepository.cs                     ← Generic repository interface
│       ├── IUserRepository.cs                 ← User-specific repository interface
│       ├── ICourseRepository.cs               ← Course-specific repository interface
│       ├── INoteRepository.cs                 ← Note-specific repository interface
│       └── IUnitOfWork.cs                     ← Unit of Work interface
│
├── LearnPlatform.Application/                 ← Application Layer
│   └── (DTOs, Service Interfaces — to be added)
│
├── LearnPlatform.Infrastructure/              ← Infrastructure Layer
│   ├── Data/
│   │   └── ApplicationDbContext.cs            ← EF Core DbContext
│   ├── Repositories/
│   │   ├── EfRepository.cs                    ← Generic EF repository
│   │   ├── UserRepository.cs                  ← User repository
│   │   ├── CourseRepository.cs                ← Course repository
│   │   └── NoteRepository.cs                  ← Note repository
│   └── UnitOfWork/
│       └── UnitOfWork.cs                      ← Unit of Work implementation
│
└── LearnPlatform.Web/                         ← Presentation Layer
    ├── Controllers/                           ← API Controllers (to be added)
    ├── Views/                                 ← Razor Views
    ├── Models/                                ← View Models
    ├── Program.cs                             ← App startup & DI
    ├── appsettings.json                       ← Configuration
    └── wwwroot/                               ← Static files
```

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2026 Shadman Muhtasim

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## Author

| Detail | Information |
|--------|-------------|
| **Name** | Shadman Muhtasim |
| **Project** | LearnifyAI — AI-Powered Learning Platform |
| **Tech Stack** | .NET 8, EF Core 8, SQL Server, Clean Architecture |
| **Current Status** | Milestone 1 Complete — Infrastructure Layer Ready |

---

> **Note:** This project is actively under development. Milestones 1.5 through 1.8 are in progress, followed by Milestones 2 through 9. Check the [Milestone Roadmap](#milestone-roadmap) section for the latest status.