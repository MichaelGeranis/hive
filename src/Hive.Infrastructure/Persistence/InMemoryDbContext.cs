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
    public ConcurrentDictionary<Guid, SkillCategoryEntity> SkillCategories { get; } = new();
    public ConcurrentDictionary<Guid, Skill> Skills { get; } = new();
    public ConcurrentDictionary<Guid, SkillAssessment> SkillAssessments { get; } = new();
    public ConcurrentDictionary<Guid, OneOnOneMeeting> OneOnOneMeetings { get; } = new();
    public ConcurrentDictionary<Guid, MeetingNote> MeetingNotes { get; } = new();
    public ConcurrentDictionary<Guid, Project> Projects { get; } = new();
    public ConcurrentDictionary<Guid, TeamTask> TeamTasks { get; } = new();
    public ConcurrentDictionary<Guid, Leave> Leaves { get; } = new();
    public ConcurrentDictionary<Guid, ManagerNote> ManagerNotes { get; } = new();
    public ConcurrentDictionary<Guid, Sprint> Sprints { get; } = new();
    public ConcurrentDictionary<Guid, SprintCapacity> SprintCapacities { get; } = new();
    public ConcurrentDictionary<Guid, Document> Documents { get; } = new();
    public ConcurrentDictionary<Guid, Parent> Parents { get; } = new();
    public ConcurrentDictionary<Guid, Activity> Activities { get; } = new();
    public ConcurrentDictionary<Guid, ChecklistTemplate> ChecklistTemplates { get; } = new();
    public ConcurrentDictionary<Guid, ChecklistTemplateItem> ChecklistTemplateItems { get; } = new();
    public ConcurrentDictionary<Guid, ChecklistInstance> ChecklistInstances { get; } = new();
    public ConcurrentDictionary<Guid, ChecklistInstanceItem> ChecklistInstanceItems { get; } = new();
    public ConcurrentDictionary<Guid, ProjectKnowledge> ProjectKnowledge { get; } = new();
    public ConcurrentDictionary<Guid, SentimentAnalysisCache> SentimentAnalysisCache { get; } = new();
    public ConcurrentDictionary<Guid, Quarter> Quarters { get; } = new();
    public ConcurrentDictionary<Guid, Initiative> Initiatives { get; } = new();
    public ConcurrentDictionary<Guid, Allocation> Allocations { get; } = new();
    public ConcurrentDictionary<Guid, SprintGoal> SprintGoals { get; } = new();
    public ConcurrentDictionary<Guid, InitiativeDependency> InitiativeDependencies { get; } = new();
    public ConcurrentDictionary<Guid, KnowledgePoint> KnowledgePoints { get; } = new();
    public List<AppSettings> AppSettings { get; } = new();

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

        // Seed projects and tasks
        SeedProjectsAndTasks();

        // Seed leaves
        SeedLeaves();
    }

    private void SeedLeaves()
    {
        var directReportIds = DirectReports.Keys.ToList();
        if (directReportIds.Count == 0) return;

        // Past vacation for Alice
        var leave1 = new Leave(
            directReportIds[0],
            LeaveType.Vacation,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-25),
            "Summer vacation");
        Leaves.TryAdd(leave1.Id, leave1);

        // Upcoming vacation for Alice
        var leave2 = new Leave(
            directReportIds[0],
            LeaveType.Vacation,
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow.AddDays(15),
            "Personal time");
        Leaves.TryAdd(leave2.Id, leave2);

        if (directReportIds.Count > 1)
        {
            // Sick leave for Bob (currently on leave)
            var leave3 = new Leave(
                directReportIds[1],
                LeaveType.Sick,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                "Not feeling well");
            Leaves.TryAdd(leave3.Id, leave3);

            // Upcoming vacation for Bob
            var leave4 = new Leave(
                directReportIds[1],
                LeaveType.Vacation,
                DateTime.UtcNow.AddDays(30),
                DateTime.UtcNow.AddDays(40),
                "Winter holiday");
            Leaves.TryAdd(leave4.Id, leave4);
        }

        if (directReportIds.Count > 2)
        {
            // Past vacation for Carol
            var leave5 = new Leave(
                directReportIds[2],
                LeaveType.Vacation,
                DateTime.UtcNow.AddDays(-60),
                DateTime.UtcNow.AddDays(-58),
                "Family event");
            Leaves.TryAdd(leave5.Id, leave5);

            // Upcoming conference
            var leave6 = new Leave(
                directReportIds[2],
                LeaveType.Other,
                DateTime.UtcNow.AddDays(7),
                DateTime.UtcNow.AddDays(9),
                "Tech conference");
            Leaves.TryAdd(leave6.Id, leave6);
        }
    }

    private void SeedPerformanceReviews()
    {
        var directReportIds = DirectReports.Keys.ToList();
        if (directReportIds.Count == 0) return;

        // Create a sample review for first direct report
        var review1 = new PerformanceReview(directReportIds[0], "2024 Annual Review", new DateTime(2024, 12, 15));
        review1.UpdateContent(
            "Strong technical leadership and mentoring skills",
            "Could improve delegation and documentation",
            "Excellent year overall",
            PerformanceRating.ExceedsExpectations);
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
        // Create skill categories first
        var technicalCategory = SkillCategoryEntity.CreateWithId(
            new Guid("10000000-0000-0000-0000-000000000001"),
            "Technical", "Technical skills and programming knowledge", 0);
        var softSkillsCategory = SkillCategoryEntity.CreateWithId(
            new Guid("10000000-0000-0000-0000-000000000002"),
            "Soft Skills", "Communication and interpersonal skills", 1);
        var leadershipCategory = SkillCategoryEntity.CreateWithId(
            new Guid("10000000-0000-0000-0000-000000000003"),
            "Leadership", "Leadership and management skills", 2);
        var domainCategory = SkillCategoryEntity.CreateWithId(
            new Guid("10000000-0000-0000-0000-000000000004"),
            "Domain Knowledge", "Industry and domain expertise", 3);
        var toolsCategory = SkillCategoryEntity.CreateWithId(
            new Guid("10000000-0000-0000-0000-000000000005"),
            "Tools", "Development tools and platforms", 4);

        SkillCategories.TryAdd(technicalCategory.Id, technicalCategory);
        SkillCategories.TryAdd(softSkillsCategory.Id, softSkillsCategory);
        SkillCategories.TryAdd(leadershipCategory.Id, leadershipCategory);
        SkillCategories.TryAdd(domainCategory.Id, domainCategory);
        SkillCategories.TryAdd(toolsCategory.Id, toolsCategory);

        // Technical skills
        var csharp = new Skill("C#", "Proficiency in C# programming language", technicalCategory.Id);
        var dotnet = new Skill(".NET Core", "Experience with .NET Core framework", technicalCategory.Id);
        var sql = new Skill("SQL", "Database querying and design skills", technicalCategory.Id);
        var systemDesign = new Skill("System Design", "Ability to design scalable systems", technicalCategory.Id);

        // Soft skills
        var communication = new Skill("Communication", "Written and verbal communication skills", softSkillsCategory.Id);
        var teamwork = new Skill("Teamwork", "Ability to collaborate effectively", softSkillsCategory.Id);

        // Leadership
        var mentoring = new Skill("Mentoring", "Ability to guide and develop others", leadershipCategory.Id);
        var decisionMaking = new Skill("Decision Making", "Making timely and effective decisions", leadershipCategory.Id);

        // Tools
        var git = new Skill("Git", "Version control with Git", toolsCategory.Id);
        var docker = new Skill("Docker", "Containerization with Docker", toolsCategory.Id);

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

        // Past meeting with Alice
        var meeting1 = new OneOnOneMeeting(
            directReportIds[0],
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            "Weekly sync - project updates, blockers");
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
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            "Weekly sync - follow up on action items");
        OneOnOneMeetings.TryAdd(meeting2.Id, meeting2);

        // Upcoming meeting with Bob
        if (directReportIds.Count > 1)
        {
            var meeting3 = new OneOnOneMeeting(
                directReportIds[1],
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                "Career development discussion");
            OneOnOneMeetings.TryAdd(meeting3.Id, meeting3);

            // Past meeting with Bob
            var meeting4 = new OneOnOneMeeting(
                directReportIds[1],
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)),
                "Project check-in");
            OneOnOneMeetings.TryAdd(meeting4.Id, meeting4);

            var note5 = new MeetingNote(meeting4.Id, "Discussed .NET Core learning path", NoteCategory.Discussion);
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
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                "Quarterly check-in");
            OneOnOneMeetings.TryAdd(meeting5.Id, meeting5);
        }
    }

    private void SeedProjectsAndTasks()
    {
        var directReportIds = DirectReports.Keys.ToList();

        // Create sample projects
        var apiRedesign = new Project(
            "API Redesign",
            "Redesign the REST API to follow clean architecture patterns",
            "backend,api",
            "https://github.com/example/api-redesign");
        Projects.TryAdd(apiRedesign.Id, apiRedesign);

        var mobileApp = new Project(
            "Mobile App MVP",
            "Build minimum viable product for mobile application",
            "mobile,frontend",
            "https://github.com/example/mobile-app");
        Projects.TryAdd(mobileApp.Id, mobileApp);

        var documentation = new Project(
            "Documentation Overhaul",
            "Update and improve all technical documentation",
            "docs");
        Projects.TryAdd(documentation.Id, documentation);

        var techDebt = new Project(
            "Tech Debt Sprint",
            "Address accumulated technical debt",
            "backend,tech-debt");
        Projects.TryAdd(techDebt.Id, techDebt);

        // Create sample tasks for API Redesign project
        var task1 = new TeamTask(
            "Design new API endpoints",
            "Create OpenAPI specification for the new endpoints",
            TaskType.Story,
            TaskPriority.High,
            directReportIds.Count > 2 ? directReportIds[2] : null,
            apiRedesign.Id,
            DateTime.UtcNow.AddDays(-5),
            8,
            2,
            string.Empty);
        task1.Start();
        task1.MoveToReview();
        task1.Complete();
        TeamTasks.TryAdd(task1.Id, task1);

        var task2 = new TeamTask(
            "Implement authentication middleware",
            "Add JWT authentication to all protected endpoints",
            TaskType.Story,
            TaskPriority.Critical,
            directReportIds.Count > 0 ? directReportIds[0] : null,
            apiRedesign.Id,
            DateTime.UtcNow.AddDays(7),
            16,
            3,
            "security,auth");
        task2.Start();
        TeamTasks.TryAdd(task2.Id, task2);

        var task3 = new TeamTask(
            "Fix pagination bug",
            "Pagination returns incorrect results for filtered queries",
            TaskType.Bug,
            TaskPriority.High,
            directReportIds.Count > 1 ? directReportIds[1] : null,
            apiRedesign.Id,
            DateTime.UtcNow.AddDays(-2),
            4,
            1,
            "bug,urgent");
        task3.MoveToTodo();
        TeamTasks.TryAdd(task3.Id, task3);

        var task4 = new TeamTask(
            "Add rate limiting",
            "Implement rate limiting for public endpoints",
            TaskType.Story,
            TaskPriority.Medium,
            null,
            apiRedesign.Id,
            DateTime.UtcNow.AddDays(14),
            8,
            2,
            "performance,security");
        TeamTasks.TryAdd(task4.Id, task4);

        // Create sample tasks for Mobile App project
        var task5 = new TeamTask(
            "Setup React Native project",
            "Initialize project with TypeScript template",
            TaskType.Task,
            TaskPriority.High,
            directReportIds.Count > 1 ? directReportIds[1] : null,
            mobileApp.Id,
            DateTime.UtcNow.AddDays(-7),
            4,
            1,
            "mobile,setup");
        task5.Start();
        task5.Complete();
        TeamTasks.TryAdd(task5.Id, task5);

        var task6 = new TeamTask(
            "Design login screen",
            "Create UI/UX design for login and registration flow",
            TaskType.Story,
            TaskPriority.High,
            directReportIds.Count > 0 ? directReportIds[0] : null,
            mobileApp.Id,
            DateTime.UtcNow.AddDays(3),
            6,
            2,
            "mobile,ui");
        task6.MoveToTodo();
        TeamTasks.TryAdd(task6.Id, task6);

        var task7 = new TeamTask(
            "Research offline sync options",
            "Evaluate options for offline data synchronization",
            TaskType.Spike,
            TaskPriority.Medium,
            directReportIds.Count > 2 ? directReportIds[2] : null,
            mobileApp.Id,
            DateTime.UtcNow.AddDays(10),
            8,
            2,
            "research,mobile");
        TeamTasks.TryAdd(task7.Id, task7);

        // Create unassigned backlog tasks
        var task8 = new TeamTask(
            "Update API documentation",
            "Update Swagger documentation with new endpoints",
            TaskType.Task,
            TaskPriority.Low,
            null,
            documentation.Id,
            DateTime.UtcNow.AddDays(21),
            4,
            1,
            "docs");
        TeamTasks.TryAdd(task8.Id, task8);

        var task9 = new TeamTask(
            "Refactor database queries",
            "Optimize slow database queries identified in profiling",
            TaskType.Task,
            TaskPriority.Medium,
            null,
            null,
            DateTime.UtcNow.AddDays(14),
            12,
            3,
            "performance,database");
        TeamTasks.TryAdd(task9.Id, task9);
    }
}
