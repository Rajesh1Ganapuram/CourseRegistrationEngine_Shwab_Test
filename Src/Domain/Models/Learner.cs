namespace CourseRegistrationEngine.Domain.Models;

public record Learner(string Id, string Name, HashSet<string> CompletedCourses);