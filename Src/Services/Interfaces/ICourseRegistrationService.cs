using System.Threading.Tasks;
using CourseRegistrationEngine.Domain.Models;

namespace CourseRegistrationEngine.Services.Interfaces;

public interface ICourseRegistrationService
{
    Task<EnrollmentResult> EnrollAsync(string courseId, string learnerId);
    Task<EnrollmentResult> DropAsync(string courseId, string learnerId);
}