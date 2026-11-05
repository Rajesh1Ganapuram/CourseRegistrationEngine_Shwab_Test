# Design Specification: Course Registration & Waitlist Engine

## 1. Service Boundary & Scope
This service implements a bounded course-registration slice for a **single course offering**, handling three core workflows:
1. **Enrollment:** Direct registration into available seats or overflow into a FIFO waitlist.
2. **Dropping:** Removal of an enrolled learner, which automatically triggers seat reassignment.
3. **Waitlist Promotion:** Deterministic, FIFO-based promotion of eligible learners from the waitlist into newly opened seats.

**Out of Scope:** Multi-term course scheduling, full course catalogs, user authentication/identity management, billing/payments, frontend interfaces, and permanent database persistence (thread-safe in-memory collections and ASP.NET Core Minimal APIs are used for the prototype).

---

## 2. Data & Domain Model
- **`Learner`**: Represents a student profile containing:
  - `Id` (string): Unique identifier.
  - `Name` (string): Learner's full name.
  - `CompletedCourses` (`HashSet<string>`): Collection of course IDs successfully completed (used for prerequisite validation).
- **`CourseOffering`**: Represents a specific course instance containing:
  - `CourseId` & `Title` (string): Identifiers.
  - `Capacity` (int): Maximum concurrent enrolled seats.
  - `PrerequisiteCourseId` (string, optional): Required completed course ID.
  - `EnrolledLearners` (`HashSet<string>`): Active enrolled student IDs.
  - `Waitlist` (`List<string>`): Ordered queue maintaining FIFO waitlist placement.
- **`EnrollmentResult`**: Encapsulates operation status (`Enrolled`, `Waitlisted`, `AlreadyEnrolled`, `PrerequisiteNotMet`, `CourseNotFound`, `LearnerNotFound`) and a descriptive message.

---

## 3. Core Invariants & Business Rules
1. **Capacity Invariant:** The number of active `EnrolledLearners` shall never exceed `Capacity`.
2. **Uniqueness:** A learner cannot be duplicated across active enrollment or waitlist collections for the same course.
3. **Prerequisite Enforcement:** A learner must satisfy the prerequisite rule prior to initial enrollment and upon waitlist promotion.
4. **FIFO Waitlist Order:** Waitlist placement and promotion adhere strictly to First-In, First-Out temporal order.

---

## 4. Waitlist Promotion & Skipping Policy
- **Policy Definition:** When an enrolled learner drops a course, the service scans the waitlist starting from the front (**FIFO order**) to find the first eligible learner.
- **Ineligible Handling:** If the learner at the front of the queue fails the prerequisite check, they are **skipped** for promotion, but **retained in the waitlist queue** until they fulfill the prerequisite or voluntarily drop. This prevents queue stagnation while maintaining fair ordering.

---

## 5. Concurrency & Thread Safety Strategy
- **Per-Course Locking (`AsyncLock` Pattern):** To prevent race conditions during high-volume parallel registration requests without creating a global application bottleneck, the service utilizes a per-course `SemaphoreSlim` (initialized with a count of 1) managed inside a `ConcurrentDictionary<string, SemaphoreSlim>`.
- **Atomic Execution:** All capacity checks, status validations, and collection modifications for a given course occur inside a `try/finally` block governed by its specific course semaphore.

---

## 6. Assumptions & Trade-offs
- **In-Memory Store:** `ConcurrentDictionary` and thread-safe collections provide high throughput and deterministic unit-test behavior without requiring external database setup overhead.
- **Explicit Prerequisite Requirement:** Prerequisite dependencies are modeled as simple course ID string matching against a learner's historical completion record, avoiding complex rule engine overhead.