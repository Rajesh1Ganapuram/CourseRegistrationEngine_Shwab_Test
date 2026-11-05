using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using CourseRegistrationEngine.Domain.Entities;
using CourseRegistrationEngine.Domain.Enums;
using CourseRegistrationEngine.Domain.Models;
using CourseRegistrationEngine.Services;

namespace CourseRegistrationEngine.Tests;

public class CourseRegistrationServiceTests
{
    [Fact]
    public async Task Enroll_WhenCapacityAvailable_Succeeds()
    {
        // Arrange
        var service = new CourseRegistrationService();
        service.Courses.TryAdd("CS101", new CourseOffering("CS101", "Introduction to Computer Science", capacity: 2));
        service.Learners.TryAdd("L1", new Learner("L1", "Alice", new HashSet<string>()));

        // Act
        var result = await service.EnrollAsync("CS101", "L1");

        // Assert
        Assert.Equal(EnrollmentStatus.Enrolled, result.Status);
        var course = service.Courses["CS101"];
        Assert.Contains("L1", course.EnrolledLearners);
    }

    [Fact]
    public async Task Enroll_WhenFull_AddsToWaitlistOnce()
    {
        // Arrange
        var service = new CourseRegistrationService();
        service.Courses.TryAdd("CS101", new CourseOffering("CS101", "Intro to CS", capacity: 1));
        service.Learners.TryAdd("L1", new Learner("L1", "Alice", new HashSet<string>()));
        service.Learners.TryAdd("L2", new Learner("L2", "Bob", new HashSet<string>()));

        // Act
        await service.EnrollAsync("CS101", "L1"); // Fills the single seat
        var result1 = await service.EnrollAsync("CS101", "L2"); // Goes to waitlist
        var result2 = await service.EnrollAsync("CS101", "L2"); // Duplicate registration attempt

        // Assert
        Assert.Equal(EnrollmentStatus.Waitlisted, result1.Status);
        Assert.Equal(EnrollmentStatus.AlreadyEnrolled, result2.Status);

        var course = service.Courses["CS101"];
        Assert.Single(course.Waitlist);
        Assert.Equal("L2", course.Waitlist.First());
    }

    [Fact]
    public async Task Drop_EnrolledLearner_PromotesEarliestEligibleWaitlistedLearner()
    {
        // Arrange
        var service = new CourseRegistrationService();
        service.Courses.TryAdd("CS101", new CourseOffering("CS101", "Intro to CS", capacity: 1));

        service.Learners.TryAdd("L1", new Learner("L1", "Alice", new HashSet<string>()));
        service.Learners.TryAdd("L2", new Learner("L2", "Bob", new HashSet<string>())); // Eligible (no prereq)

        await service.EnrollAsync("CS101", "L1");
        await service.EnrollAsync("CS101", "L2"); // Placed on waitlist

        // Act - L1 drops, opening a seat for L2 via FIFO promotion
        var dropResult = await service.DropAsync("CS101", "L1");

        // Assert
        Assert.Equal(EnrollmentStatus.Enrolled, dropResult.Status);
        var course = service.Courses["CS101"];
        Assert.DoesNotContain("L1", course.EnrolledLearners);
        Assert.Contains("L2", course.EnrolledLearners);
        Assert.Empty(course.Waitlist);
    }

    [Fact]
    public async Task WaitlistPromotion_SkipsIneligibleLearnerAndPromotesNextEligible()
    {
        // Arrange - Course requires "MATH101" prerequisite
        var service = new CourseRegistrationService();
        service.Courses.TryAdd("CS201", new CourseOffering("CS201", "Advanced CS", capacity: 1, prerequisiteCourseId: "MATH101"));

        service.Learners.TryAdd("L1", new Learner("L1", "Alice", new HashSet<string> { "MATH101" })); // Enrolled
        service.Learners.TryAdd("L2", new Learner("L2", "Bob", new HashSet<string>()));   // Ineligible (missing MATH101)
        service.Learners.TryAdd("L3", new Learner("L3", "Charlie", new HashSet<string> { "MATH101" })); // Eligible

        await service.EnrollAsync("CS201", "L1"); // Capacity 1 full
        await service.EnrollAsync("CS201", "L2"); // Waitlist pos 1 (Ineligible)
        await service.EnrollAsync("CS201", "L3"); // Waitlist pos 2 (Eligible)

        // Act - L1 drops their seat
        await service.DropAsync("CS201", "L1");

        // Assert - L2 was skipped due to prerequisite failure, L3 was promoted instead
        var course = service.Courses["CS201"];
        Assert.Contains("L3", course.EnrolledLearners);
        Assert.DoesNotContain("L2", course.EnrolledLearners);
        Assert.Contains("L2", course.Waitlist); // L2 remains in waitlist queue according to policy
    }

    [Fact]
    public async Task Drop_InvalidOrNotEnrolledLearner_ReturnsClearResultWithoutCorruption()
    {
        // Arrange
        var service = new CourseRegistrationService();
        service.Courses.TryAdd("CS101", new CourseOffering("CS101", "Intro to CS", capacity: 2));
        service.Learners.TryAdd("L1", new Learner("L1", "Alice", new HashSet<string>()));

        // Act - Attempting to drop a learner who is neither enrolled nor waitlisted
        var result = await service.DropAsync("CS101", "L1");

        // Assert
        Assert.Equal(EnrollmentStatus.LearnerNotFound, result.Status);
        Assert.Empty(service.Courses["CS101"].EnrolledLearners);
    }

    [Fact]
    public async Task ConcurrentEnrollments_PreservesCapacityLimits()
    {
        // Arrange
        var service = new CourseRegistrationService();
        int capacity = 5;
        service.Courses.TryAdd("CS101", new CourseOffering("CS101", "Concurrent CS", capacity: capacity));

        // Create 30 concurrent learners
        var tasks = new List<Task<EnrollmentResult>>();
        for (int i = 1; i <= 30; i++)
        {
            string id = $"L{i}";
            service.Learners.TryAdd(id, new Learner(id, $"Student {i}", new HashSet<string>()));
            tasks.Add(service.EnrollAsync("CS101", id));
        }

        // Act - Fire 30 simultaneous enrollment requests
        await Task.WhenAll(tasks);

        // Assert - Exactly 5 enrolled due to capacity lock, remaining 25 safely waitlisted
        var course = service.Courses["CS101"];
        Assert.Equal(capacity, course.EnrolledLearners.Count);
        Assert.Equal(25, course.Waitlist.Count);
    }
}