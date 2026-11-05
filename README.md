# Course Registration & Waitlist Engine

A robust, thread-safe .NET 8 Minimal API service slice implementing course enrollment, dropping, prerequisite verification, and FIFO waitlist promotion. Developed as part of an AI-assisted engineering assessment.

## Project Architecture & Structure

```
CourseRegistrationEngine_Shwab_Test/
├── Src/
│   ├── CourseRegistrationEngine.Domain/      # Domain entities, records, and enums
│   ├── CourseRegistrationEngine.Services/    # Core business logic & per-course concurrency locks
│   └── CourseRegistrationEngine.Api/         # ASP.NET Core Minimal API endpoints & data initializer
├── Tests/
│   └── CourseRegistrationEngine.Tests/       # xUnit test suite (including high-concurrency stress test)
├── Design-Spec.md                            # Architecture, domain model, invariants, and trade-offs
├── AI-Prompt-Record.md                       # Log of AI prompts, review decisions, and verification evidence
└── README.md                                 # Setup, prerequisites, and execution instructions
```

## Key Features & Invariants
- **Capacity Enforcement:** Ensures active enrollments never exceed the course capacity limit.
- **Prerequisite Validation:** Verifies that learners meet required prerequisite course completions before initial enrollment and upon waitlist promotion.
- **FIFO Waitlist Promotion & Skipping Policy:** When a seat opens, the engine scans the waitlist in First-In, First-Out order and promotes the first eligible learner. Ineligible learners are skipped for promotion but retained in the queue.
- **Thread-Safe Concurrency:** Utilizes a per-course `SemaphoreSlim` (`AsyncLock` pattern) inside a `ConcurrentDictionary` to guarantee atomic capacity checks and prevent race conditions under heavy parallel load without global locking bottlenecks.

## Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later installed on your machine.

## Getting Started & Execution Instructions

### 1. Clone the Repository
```bash
git clone https://github.com/Rajesh1Ganapuram/CourseRegistrationEngine_Shwab_Test.git
cd CourseRegistrationEngine_Shwab_Test
```

### 2. Run the Automated Tests
To run the full unit and concurrency test suite, execute:
```bash
dotnet test
```

### 3. Run the Minimal API
To start the local web server and test endpoints via Swagger or cURL:
```bash
dotnet run --project Src/CourseRegistrationEngine.Api/CourseRegistrationEngine.Api.csproj
```
* Once running, navigate to `https://localhost:5001/swagger` (or the HTTP equivalent output in your terminal) to explore and test the endpoints (`POST /api/courses/{courseId}/enroll`, `POST /api/courses/{courseId}/drop`, and `GET /api/courses/{courseId}`).