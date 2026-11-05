# AI Prompt and Use Record

This document records the prompts issued to AI engineering assistants, the nature of the responses, review decisions (accepted, edited, or rejected), and verification outcomes.

---

## Activity 1: Domain Model and Thread-Safe Service Architecture

### Prompt Used
> "As a senior .Net Developer, Help me design the C# domain model, service architecture, and concurrency strategy for the course registration and waitlist feature. Ensure thread safety under parallel requests and handle FIFO waitlist promotion with prerequisite checks."

### AI Response Summary
The AI generated a domain model using immutable records and domain entities, a service class utilizing `ConcurrentDictionary`, and an async per-course lock pattern (`SemaphoreSlim`) to prevent race conditions during concurrent seat allocation.

### Review & Decision
- **Status:** **Accepted with Edits**
- **Edits Made:** Refined the waitlist promotion logic (`PopNextEligibleFromWaitlist`) to explicitly handle the **skipping policy**—ensuring that ineligible learners are skipped during promotion sweeps while remaining in the waitlist queue rather than being prematurely evicted.
- **Informed Files:** `src/CourseRegistrationEngine.Domain/`, `src/CourseRegistrationEngine.Services/`

---

## Activity 2: Minimal API and Endpoints Integration

### Prompt Used
> "Transition the console application architecture into an ASP.NET Core Minimal API with endpoints for enrolling, dropping, and inspecting course state, complete with a clean data initializer."

### AI Response Summary
The AI generated a structured Minimal API `Program.cs` file with dependency injection, routing groups, OpenAPI/Swagger support, and a decoupled `DataInitializer` class.

### Review & Decision
- **Status:** **Accepted**
- **Informed Files:** `src/CourseRegistrationEngine.Api/Program.cs`, `src/CourseRegistrationEngine.Api/DataInitializer.cs`

---

## Activity 3: Comprehensive Unit & Concurrency Test Suite

### Prompt Used
> "As a senior .Net Developer, Generate the complete xUnit test suite for the namespaced course registration and waitlist service. Include tests for available capacity, full capacity, duplicate requests, FIFO promotion, prerequisite failure, invalid drops, and a high-concurrency stress test using Task.WhenAll."

### AI Response Summary
The AI generated a full xUnit test class covering 6 distinct scenarios, including a stress test firing 30 concurrent enrollment requests against a course with a capacity of 5.

### Review & Decision
- **Status:** **Accepted**
- **Verification Evidence:** Executed `dotnet test` locally in the terminal. All tests compiled successfully and passed on the first run, verifying that capacity limits (exactly 5 enrolled, 25 waitlisted) and FIFO promotion rules hold under parallel execution.
- **Informed Files:** `tests/CourseRegistrationEngine.Tests/CourseRegistrationServiceTests.cs`