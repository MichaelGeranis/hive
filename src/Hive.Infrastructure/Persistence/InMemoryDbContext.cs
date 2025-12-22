using Hive.Core.Entities;
using System.Collections.Concurrent;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// In-memory database context for development/testing.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public class InMemoryDbContext
{
    public ConcurrentDictionary<Guid, DirectReport> DirectReports { get; } = new();
    public ConcurrentDictionary<Guid, PerformanceReview> PerformanceReviews { get; } = new();
    public ConcurrentDictionary<Guid, Skill> Skills { get; } = new();
    public ConcurrentDictionary<Guid, SkillAssessment> SkillAssessments { get; } = new();
    public ConcurrentDictionary<Guid, OneOnOneMeeting> OneOnOneMeetings { get; } = new();
    public ConcurrentDictionary<Guid, MeetingNote> MeetingNotes { get; } = new();

    /// <summary>
    /// Seeds the database with sample data for development.
    /// </summary>
    public void SeedData()
    {
        var sampleReports = new[]
        {
            new DirectReport("Alice", "Johnson", "alice.johnson@company.com", "Senior Software Engineer", "Engineering", new DateTime(2022, 3, 15)),
            new DirectReport("Bob", "Smith", "bob.smith@company.com", "Software Engineer", "Engineering", new DateTime(2023, 1, 10)),
            new DirectReport("Carol", "Williams", "carol.williams@company.com", "Staff Engineer", "Engineering", new DateTime(2021, 6, 1))
        };

        foreach (var report in sampleReports)
        {
            DirectReports.TryAdd(report.Id, report);
        }

        // Seed sample performance reviews
        SeedPerformanceReviews();

        // Seed skills and assessments
        SeedSkills();

        // Seed 1:1 meetings
        SeedOneOnOneMeetings();
    }

    private void SeedPerformanceReviews()
    {
        var directReportIds = DirectReports.Keys.ToList();
        if (directReportIds.Count == 0) return;

        // Create a sample completed review for first direct report
        var review1 = new PerformanceReview(directReportIds[0], "2024 Annual Review", new DateTime(2024, 12, 15));
        review1.UpdateContent(
            "Strong technical leadership and mentoring skills",
            "Could improve delegation and documentation",
            "Lead architecture redesign initiative",
            "Excellent year overall",
            PerformanceRating.ExceedsExpectations);
        review1.UpdateSelfAssessment("I feel I've grown significantly this year in my technical abilities.");
        review1.Submit();
        review1.Acknowledge();
        review1.Complete();
        PerformanceReviews.TryAdd(review1.Id, review1);

        // Create a draft review for second direct report
        if (directReportIds.Count > 1)
        {
            var review2 = new PerformanceReview(directReportIds[1], "2024 Annual Review", new DateTime(2024, 12, 15));
            PerformanceReviews.TryAdd(review2.Id, review2);
        }
    }

    private void SeedSkills()
    {
        // Technical skills
        var csharp = new Skill("C#", "Proficiency in C# programming language", SkillCategory.Technical);
        var dotnet = new Skill(".NET Core", "Experience with .NET Core framework", SkillCategory.Technical);
        var sql = new Skill("SQL", "Database querying and design skills", SkillCategory.Technical);
        var systemDesign = new Skill("System Design", "Ability to design scalable systems", SkillCategory.Technical);

        // Soft skills
        var communication = new Skill("Communication", "Written and verbal communication skills", SkillCategory.SoftSkills);
        var teamwork = new Skill("Teamwork", "Ability to collaborate effectively", SkillCategory.SoftSkills);

        // Leadership
        var mentoring = new Skill("Mentoring", "Ability to guide and develop others", SkillCategory.Leadership);
        var decisionMaking = new Skill("Decision Making", "Making timely and effective decisions", SkillCategory.Leadership);

        // Tools
        var git = new Skill("Git", "Version control with Git", SkillCategory.Tools);
        var docker = new Skill("Docker", "Containerization with Docker", SkillCategory.Tools);

        var skills = new[] { csharp, dotnet, sql, systemDesign, communication, teamwork, mentoring, decisionMaking, git, docker };
        foreach (var skill in skills)
        {
            Skills.TryAdd(skill.Id, skill);
        }

        // Seed sample assessments for direct reports
        var directReportIds = DirectReports.Keys.ToList();
        if (directReportIds.Count == 0) return;

        // Alice (Senior Engineer) - high technical skills
        var aliceAssessments = new[]
        {
            new SkillAssessment(directReportIds[0], csharp.Id, ProficiencyLevel.Expert, ProficiencyLevel.Expert),
            new SkillAssessment(directReportIds[0], dotnet.Id, ProficiencyLevel.Advanced, ProficiencyLevel.Expert),
            new SkillAssessment(directReportIds[0], sql.Id, ProficiencyLevel.Advanced, ProficiencyLevel.Advanced),
            new SkillAssessment(directReportIds[0], systemDesign.Id, ProficiencyLevel.Advanced, ProficiencyLevel.Expert, "Working on improving architecture skills"),
            new SkillAssessment(directReportIds[0], mentoring.Id, ProficiencyLevel.Intermediate, ProficiencyLevel.Advanced, "Starting to mentor junior devs")
        };
        foreach (var a in aliceAssessments) SkillAssessments.TryAdd(a.Id, a);

        // Bob (Software Engineer) - developing skills
        if (directReportIds.Count > 1)
        {
            var bobAssessments = new[]
            {
                new SkillAssessment(directReportIds[1], csharp.Id, ProficiencyLevel.Intermediate, ProficiencyLevel.Advanced),
                new SkillAssessment(directReportIds[1], dotnet.Id, ProficiencyLevel.Beginner, ProficiencyLevel.Intermediate, "Focus area for Q1"),
                new SkillAssessment(directReportIds[1], git.Id, ProficiencyLevel.Intermediate, ProficiencyLevel.Intermediate),
                new SkillAssessment(directReportIds[1], communication.Id, ProficiencyLevel.Intermediate, ProficiencyLevel.Advanced)
            };
            foreach (var a in bobAssessments) SkillAssessments.TryAdd(a.Id, a);
        }

        // Carol (Staff Engineer) - expert level
        if (directReportIds.Count > 2)
        {
            var carolAssessments = new[]
            {
                new SkillAssessment(directReportIds[2], csharp.Id, ProficiencyLevel.Expert, ProficiencyLevel.Expert),
                new SkillAssessment(directReportIds[2], systemDesign.Id, ProficiencyLevel.Expert, ProficiencyLevel.Expert),
                new SkillAssessment(directReportIds[2], mentoring.Id, ProficiencyLevel.Advanced, ProficiencyLevel.Expert),
                new SkillAssessment(directReportIds[2], decisionMaking.Id, ProficiencyLevel.Advanced, ProficiencyLevel.Advanced),
                new SkillAssessment(directReportIds[2], docker.Id, ProficiencyLevel.Advanced, ProficiencyLevel.Advanced)
            };
            foreach (var a in carolAssessments) SkillAssessments.TryAdd(a.Id, a);
        }
    }

    private void SeedOneOnOneMeetings()
    {
        var directReportIds = DirectReports.Keys.ToList();
        if (directReportIds.Count == 0) return;

        // Past completed meeting with Alice
        var meeting1 = new OneOnOneMeeting(
            directReportIds[0],
            DateTime.UtcNow.AddDays(-7),
            30,
            "Zoom",
            "Weekly sync - project updates, blockers");
        meeting1.Complete();
        OneOnOneMeetings.TryAdd(meeting1.Id, meeting1);

        // Add notes to the completed meeting
        var note1 = new MeetingNote(meeting1.Id, "Discussed progress on the API redesign project. On track for Q1 delivery.", NoteCategory.Discussion);
        var note2 = new MeetingNote(meeting1.Id, "Review and approve architecture proposal", NoteCategory.ActionItem);
        note2.SetActionDetails(DateTime.UtcNow.AddDays(-3), "Manager");
        note2.CompleteAction();
        var note3 = new MeetingNote(meeting1.Id, "Great job on mentoring the new team member!", NoteCategory.Achievement);
        var note4 = new MeetingNote(meeting1.Id, "Schedule tech talk on Clean Architecture", NoteCategory.ActionItem);
        note4.SetActionDetails(DateTime.UtcNow.AddDays(7), "Alice");

        MeetingNotes.TryAdd(note1.Id, note1);
        MeetingNotes.TryAdd(note2.Id, note2);
        MeetingNotes.TryAdd(note3.Id, note3);
        MeetingNotes.TryAdd(note4.Id, note4);

        // Upcoming meeting with Alice
        var meeting2 = new OneOnOneMeeting(
            directReportIds[0],
            DateTime.UtcNow.AddDays(2),
            30,
            "Conference Room A",
            "Weekly sync - follow up on action items");
        OneOnOneMeetings.TryAdd(meeting2.Id, meeting2);

        // Upcoming meeting with Bob
        if (directReportIds.Count > 1)
        {
            var meeting3 = new OneOnOneMeeting(
                directReportIds[1],
                DateTime.UtcNow.AddDays(1),
                45,
                "Zoom",
                "Career development discussion");
            OneOnOneMeetings.TryAdd(meeting3.Id, meeting3);

            // Past meeting with Bob
            var meeting4 = new OneOnOneMeeting(
                directReportIds[1],
                DateTime.UtcNow.AddDays(-14),
                30,
                "Zoom",
                "Project check-in");
            meeting4.Complete();
            OneOnOneMeetings.TryAdd(meeting4.Id, meeting4);

            var note5 = new MeetingNote(meeting4.Id, "Discussed .NET Core learning path", NoteCategory.CareerDevelopment);
            var note6 = new MeetingNote(meeting4.Id, "Complete Pluralsight course on .NET Core", NoteCategory.ActionItem);
            note6.SetActionDetails(DateTime.UtcNow.AddDays(-5), "Bob");
            MeetingNotes.TryAdd(note5.Id, note5);
            MeetingNotes.TryAdd(note6.Id, note6);
        }

        // Upcoming meeting with Carol
        if (directReportIds.Count > 2)
        {
            var meeting5 = new OneOnOneMeeting(
                directReportIds[2],
                DateTime.UtcNow.AddDays(3),
                30,
                "Coffee Shop",
                "Quarterly check-in");
            OneOnOneMeetings.TryAdd(meeting5.Id, meeting5);
        }
    }
}
