namespace CourseRegistrationEngine.Domain.Entities;

public class CourseOffering
{
    public string CourseId { get; }
    public string Title { get; }
    public int Capacity { get; }
    public string PrerequisiteCourseId { get; }

    private readonly HashSet<string> _enrolledLearners = new();
    private readonly List<string> _waitlist = new();

    public CourseOffering(string courseId, string title, int capacity, string prerequisiteCourseId = null)
    {
        CourseId = courseId;
        Title = title;
        Capacity = capacity;
        PrerequisiteCourseId = prerequisiteCourseId;
    }

    public IReadOnlyCollection<string> EnrolledLearners => _enrolledLearners;
    public IReadOnlyList<string> Waitlist => _waitlist;

    public bool IsEnrolled(string learnerId) => _enrolledLearners.Contains(learnerId);
    public bool IsOnWaitlist(string learnerId) => _waitlist.Contains(learnerId);

    internal void Enroll(string learnerId) => _enrolledLearners.Add(learnerId);
    internal void RemoveEnrollment(string learnerId) => _enrolledLearners.Remove(learnerId);
    internal void AddToWaitlist(string learnerId) => _waitlist.Add(learnerId);
    internal void RemoveFromWaitlist(string learnerId) => _waitlist.Remove(learnerId);

    internal string PopNextEligibleFromWaitlist(Func<string, bool> isEligibleFunc)
    {
        for (int i = 0; i < _waitlist.Count; i++)
        {
            var learnerId = _waitlist[i];
            if (isEligibleFunc(learnerId))
            {
                _waitlist.RemoveAt(i);
                return learnerId;
            }
        }
        return null;
    }
}