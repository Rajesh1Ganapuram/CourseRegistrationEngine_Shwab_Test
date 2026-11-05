using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CourseRegistrationEngine.Domain.Entities;
using CourseRegistrationEngine.Domain.Enums;
using CourseRegistrationEngine.Domain.Models;
using CourseRegistrationEngine.Services.Interfaces;

namespace CourseRegistrationEngine.Services;

public class CourseRegistrationService : ICourseRegistrationService
{
    // Public dictionaries exposed for unit test setup convenience
    public ConcurrentDictionary<string, CourseOffering> Courses { get; } = new();
    public ConcurrentDictionary<string, Learner> Learners { get; } = new();

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _courseLocks = new();

    private SemaphoreSlim GetCourseLock(string courseId) =>
        _courseLocks.GetOrAdd(courseId, _ => new SemaphoreSlim(1, 1));

    public async Task<EnrollmentResult> EnrollAsync(string courseId, string learnerId)
    {
        if (!Courses.TryGetValue(courseId, out var course))
            return new EnrollmentResult(EnrollmentStatus.CourseNotFound, "Course offering does not exist.");

        if (!Learners.TryGetValue(learnerId, out var learner))
            return new EnrollmentResult(EnrollmentStatus.LearnerNotFound, "Learner profile does not exist.");

        var sem = GetCourseLock(courseId);
        await sem.WaitAsync();
        try
        {
            if (course.IsEnrolled(learnerId) || course.IsOnWaitlist(learnerId))
                return new EnrollmentResult(EnrollmentStatus.AlreadyEnrolled, "Learner is already enrolled or waitlisted.");

            if (!SatisfiesPrerequisite(learner, course))
                return new EnrollmentResult(EnrollmentStatus.PrerequisiteNotMet, "Learner does not meet the course prerequisites.");

            if (course.EnrolledLearners.Count < course.Capacity)
            {
                course.Enroll(learnerId);
                return new EnrollmentResult(EnrollmentStatus.Enrolled, "Successfully enrolled in the course.");
            }
            else
            {
                course.AddToWaitlist(learnerId);
                return new EnrollmentResult(EnrollmentStatus.Waitlisted, "Course is full. Added to the waitlist.");
            }
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<EnrollmentResult> DropAsync(string courseId, string learnerId)
    {
        if (!Courses.TryGetValue(courseId, out var course))
            return new EnrollmentResult(EnrollmentStatus.CourseNotFound, "Course offering does not exist.");

        var sem = GetCourseLock(courseId);
        await sem.WaitAsync();
        try
        {
            if (course.IsEnrolled(learnerId))
            {
                course.RemoveEnrollment(learnerId);

                var promotedLearnerId = course.PopNextEligibleFromWaitlist(id =>
                    Learners.TryGetValue(id, out var l) && SatisfiesPrerequisite(l, course));

                if (promotedLearnerId != null)
                {
                    course.Enroll(promotedLearnerId);
                }

                return new EnrollmentResult(EnrollmentStatus.Enrolled, "Successfully dropped course and processed waitlist promotion.");
            }

            if (course.IsOnWaitlist(learnerId))
            {
                course.RemoveFromWaitlist(learnerId);
                return new EnrollmentResult(EnrollmentStatus.Enrolled, "Successfully removed from waitlist.");
            }

            return new EnrollmentResult(EnrollmentStatus.LearnerNotFound, "Learner is not enrolled or waitlisted in this course.");
        }
        finally
        {
            sem.Release();
        }
    }

    private bool SatisfiesPrerequisite(Learner learner, CourseOffering course)
    {
        if (string.IsNullOrEmpty(course.PrerequisiteCourseId)) return true;
        return learner.CompletedCourses.Contains(course.PrerequisiteCourseId);
    }
}