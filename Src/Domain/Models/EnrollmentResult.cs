using CourseRegistrationEngine.Domain.Enums;

namespace CourseRegistrationEngine.Domain.Models;

public record EnrollmentResult(EnrollmentStatus Status, string Message);