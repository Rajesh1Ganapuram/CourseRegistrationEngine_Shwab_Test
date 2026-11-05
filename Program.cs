using CourseRegistrationEngine.Api;
using CourseRegistrationEngine.Domain.Enums;
using CourseRegistrationEngine.Domain.Models;
using CourseRegistrationEngine.Services;
using CourseRegistrationEngine.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register service as singleton to maintain in-memory state across HTTP requests
builder.Services.AddSingleton<ICourseRegistrationService, CourseRegistrationService>();

// Add OpenAPI/Swagger support for easy local testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Seed sample data using the extracted initializer
DataInitializer.SeedInitialData(app.Services);

// --- API Endpoints ---

var coursesGroup = app.MapGroup("/api/courses").WithTags("Course Registration");

// 1. Enroll Learner Endpoint
coursesGroup.MapPost("/{courseId}/enroll", async (string courseId, EnrollmentRequest request, ICourseRegistrationService service) =>
{
    var result = await service.EnrollAsync(courseId, request.LearnerId);
    return result.Status switch
    {
        EnrollmentStatus.Enrolled => Results.Ok(result),
        EnrollmentStatus.Waitlisted => Results.Accepted(null, result),
        EnrollmentStatus.AlreadyEnrolled => Results.Conflict(result),
        EnrollmentStatus.PrerequisiteNotMet => Results.UnprocessableEntity(result),
        EnrollmentStatus.CourseNotFound or EnrollmentStatus.LearnerNotFound => Results.NotFound(result),
        _ => Results.BadRequest(result)
    };
})
.WithName("EnrollLearner")
.WithSummary("Enroll a learner in a course or add them to the waitlist if full.");

// 2. Drop Learner Endpoint
coursesGroup.MapPost("/{courseId}/drop", async (string courseId, EnrollmentRequest request, ICourseRegistrationService service) =>
{
    var result = await service.DropAsync(courseId, request.LearnerId);
    return result.Status switch
    {
        EnrollmentStatus.Enrolled => Results.Ok(result),
        EnrollmentStatus.CourseNotFound or EnrollmentStatus.LearnerNotFound => Results.NotFound(result),
        _ => Results.BadRequest(result)
    };
})
.WithName("DropLearner")
.WithSummary("Drop an enrolled or waitlisted learner and trigger FIFO waitlist promotion.");

// 3. Get Course State Endpoint (Helper for verification)
coursesGroup.MapGet("/{courseId}", (string courseId, ICourseRegistrationService service) =>
{
    if (service is CourseRegistrationService concreteService && concreteService.Courses.TryGetValue(courseId, out var course))
    {
        return Results.Ok(new
        {
            course.CourseId,
            course.Title,
            course.Capacity,
            course.PrerequisiteCourseId,
            Enrolled = course.EnrolledLearners,
            Waitlist = course.Waitlist
        });
    }
    return Results.NotFound(new { Message = "Course not found." });
})
.WithName("GetCourseState")
.WithSummary("Inspect current enrollment and waitlist state for a course.");

app.Run();

// --- Supporting Request DTO ---

record EnrollmentRequest(string LearnerId);