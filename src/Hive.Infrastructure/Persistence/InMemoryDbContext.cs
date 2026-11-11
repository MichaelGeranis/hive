using System.Collections.Concurrent;
using Hive.Core.Entities;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// In-memory database context for development/testing.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public class InMemoryDbContext
{
    public ConcurrentDictionary<Guid, DirectReport> DirectReports { get; } = new();
    public ConcurrentDictionary<Guid, OneOnOneMeeting> OneOnOneMeetings { get; } = new();
    public ConcurrentDictionary<Guid, Project> Projects { get; } = new();
    public ConcurrentDictionary<Guid, Product> Products { get; } = new();
    public ConcurrentDictionary<Guid, ProjectProduct> ProjectProducts { get; } = new();
    public ConcurrentDictionary<Guid, TeamTask> TeamTasks { get; } = new();
    public ConcurrentDictionary<Guid, Leave> Leaves { get; } = new();
    public ConcurrentDictionary<Guid, ManagerNote> ManagerNotes { get; } = new();
    public ConcurrentDictionary<Guid, NoteFolder> NoteFolders { get; } = new();
    public ConcurrentDictionary<Guid, Sprint> Sprints { get; } = new();
    public ConcurrentDictionary<Guid, SprintCapacity> SprintCapacities { get; } = new();
    public ConcurrentDictionary<Guid, Parent> Parents { get; } = new();
    public ConcurrentDictionary<Guid, Activity> Activities { get; } = new();
    public ConcurrentDictionary<Guid, ProjectKnowledge> ProjectKnowledge { get; } = new();
    public ConcurrentDictionary<Guid, SentimentAnalysisCache> SentimentAnalysisCache { get; } = new();
    public ConcurrentDictionary<Guid, RetroSummary> RetroSummaries { get; } = new();
    public ConcurrentDictionary<Guid, TaskReadinessReview> TaskReadinessReviews { get; } = new();
    public ConcurrentDictionary<Guid, Quarter> Quarters { get; } = new();
    public ConcurrentDictionary<Guid, Initiative> Initiatives { get; } = new();
    public ConcurrentDictionary<Guid, Allocation> Allocations { get; } = new();
    public ConcurrentDictionary<Guid, SprintGoal> SprintGoals { get; } = new();
    public ConcurrentDictionary<Guid, InitiativeDependency> InitiativeDependencies { get; } = new();
    public ConcurrentDictionary<Guid, InitiativeMember> InitiativeMembers { get; } = new();
    public ConcurrentDictionary<Guid, ExternalDependency> ExternalDependencies { get; } = new();
    public ConcurrentDictionary<Guid, QuarterMember> QuarterMembers { get; } = new();
    public ConcurrentDictionary<Guid, InitiativeProduct> InitiativeProducts { get; } = new();
    public ConcurrentDictionary<Guid, SkillRoadmap> SkillRoadmaps { get; } = new();
    public ConcurrentDictionary<Guid, RoadmapSkill> RoadmapSkills { get; } = new();
    public ConcurrentDictionary<Guid, CareerPlan> CareerPlans { get; } = new();
    public ConcurrentDictionary<Guid, CareerPlanSkill> CareerPlanSkills { get; } = new();
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

    private void SeedOneOnOneMeetings()
    {
        var reports = DirectReports.Values.OrderBy(r => r.FirstName).ToList();
        if (reports.Count == 0) return;

        void Seed(DirectReport report, int daysAgo, string content)
        {
            var meeting = new OneOnOneMeeting(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo)),
                content,
                report.FirstName.ToLowerInvariant(),
                report.Id);
            OneOnOneMeetings.TryAdd(meeting.Id, meeting);
        }

        Seed(reports[0], 7, """
            Weekly sync

            - API redesign is on track for Q1 delivery
            - Wants to mentor the new joiner, happy to pair on it
            - Asked about the architecture proposal, said it reads well
            """.Trim());

        Seed(reports[0], 21, """
            Weekly sync

            Quieter week. Picked up the migration work and is enjoying it.
            """.Trim());

        if (reports.Count > 1)
        {
            Seed(reports[1], 14, """
                Project check-in

                - Working through the .NET Core learning path
                - Wants more review time before the next release
                """.Trim());
        }

        if (reports.Count > 2)
        {
            Seed(reports[2], 30, """
                Quarterly check-in

                Feeling stretched across two projects. Agreed to drop one next quarter.
                """.Trim());
        }
    }

    private void SeedProjectsAndTasks()
    {
        var directReportIds = DirectReports.Keys.ToList();

        // Create sample projects
        var apiRedesign = new Project(
            "API Redesign",
            "Redesign the REST API to follow clean architecture patterns",
            "https://github.com/example/api-redesign");
        Projects.TryAdd(apiRedesign.Id, apiRedesign);

        var mobileApp = new Project(
            "Mobile App MVP",
            "Build minimum viable product for mobile application",
            "https://github.com/example/mobile-app");
        Projects.TryAdd(mobileApp.Id, mobileApp);

        var documentation = new Project(
            "Documentation Overhaul",
            "Update and improve all technical documentation");
        Projects.TryAdd(documentation.Id, documentation);

        var techDebt = new Project(
            "Tech Debt Sprint",
            "Address accumulated technical debt");
        Projects.TryAdd(techDebt.Id, techDebt);

        // Create sample tasks for API Redesign project
        var task1 = new TeamTask(
            "Design new API endpoints",
            description: "Create OpenAPI specification for the new endpoints",
            type: TaskType.Story,
            priority: TaskPriority.High,
            assigneeId: directReportIds.Count > 2 ? directReportIds[2] : null,
            projectId: apiRedesign.Id,
            dueDate: DateTime.UtcNow.AddDays(-5),
            storyPoints: 2,
            tags: string.Empty);
        task1.Start();
        task1.MoveToReview();
        task1.Complete();
        TeamTasks.TryAdd(task1.Id, task1);

        var task2 = new TeamTask(
            "Implement authentication middleware",
            description: "Add JWT authentication to all protected endpoints",
            type: TaskType.Story,
            priority: TaskPriority.Critical,
            assigneeId: directReportIds.Count > 0 ? directReportIds[0] : null,
            projectId: apiRedesign.Id,
            dueDate: DateTime.UtcNow.AddDays(7),
            storyPoints: 3,
            tags: "security,auth");
        task2.Start();
        TeamTasks.TryAdd(task2.Id, task2);

        var task3 = new TeamTask(
            "Fix pagination bug",
            description: "Pagination returns incorrect results for filtered queries",
            type: TaskType.Bug,
            priority: TaskPriority.High,
            assigneeId: directReportIds.Count > 1 ? directReportIds[1] : null,
            projectId: apiRedesign.Id,
            dueDate: DateTime.UtcNow.AddDays(-2),
            storyPoints: 1,
            tags: "bug,urgent");
        task3.MoveToTodo();
        TeamTasks.TryAdd(task3.Id, task3);

        var task4 = new TeamTask(
            "Add rate limiting",
            description: "Implement rate limiting for public endpoints",
            type: TaskType.Story,
            priority: TaskPriority.Medium,
            assigneeId: null,
            projectId: apiRedesign.Id,
            dueDate: DateTime.UtcNow.AddDays(14),
            storyPoints: 2,
            tags: "performance,security");
        TeamTasks.TryAdd(task4.Id, task4);

        // Create sample tasks for Mobile App project
        var task5 = new TeamTask(
            "Setup React Native project",
            description: "Initialize project with TypeScript template",
            type: TaskType.Task,
            priority: TaskPriority.High,
            assigneeId: directReportIds.Count > 1 ? directReportIds[1] : null,
            projectId: mobileApp.Id,
            dueDate: DateTime.UtcNow.AddDays(-7),
            storyPoints: 1,
            tags: "mobile,setup");
        task5.Start();
        task5.Complete();
        TeamTasks.TryAdd(task5.Id, task5);

        var task6 = new TeamTask(
            "Design login screen",
            description: "Create UI/UX design for login and registration flow",
            type: TaskType.Story,
            priority: TaskPriority.High,
            assigneeId: directReportIds.Count > 0 ? directReportIds[0] : null,
            projectId: mobileApp.Id,
            dueDate: DateTime.UtcNow.AddDays(3),
            storyPoints: 2,
            tags: "mobile,ui");
        task6.MoveToTodo();
        TeamTasks.TryAdd(task6.Id, task6);

        var task7 = new TeamTask(
            "Research offline sync options",
            description: "Evaluate options for offline data synchronization",
            type: TaskType.Spike,
            priority: TaskPriority.Medium,
            assigneeId: directReportIds.Count > 2 ? directReportIds[2] : null,
            projectId: mobileApp.Id,
            dueDate: DateTime.UtcNow.AddDays(10),
            storyPoints: 2,
            tags: "research,mobile");
        TeamTasks.TryAdd(task7.Id, task7);

        // Create unassigned backlog tasks
        var task8 = new TeamTask(
            "Update API documentation",
            description: "Update Swagger documentation with new endpoints",
            type: TaskType.Task,
            priority: TaskPriority.Low,
            assigneeId: null,
            projectId: documentation.Id,
            dueDate: DateTime.UtcNow.AddDays(21),
            storyPoints: 1,
            tags: "docs");
        TeamTasks.TryAdd(task8.Id, task8);

        var task9 = new TeamTask(
            "Refactor database queries",
            description: "Optimize slow database queries identified in profiling",
            type: TaskType.Task,
            priority: TaskPriority.Medium,
            assigneeId: null,
            projectId: null,
            dueDate: DateTime.UtcNow.AddDays(14),
            storyPoints: 3,
            tags: "performance,database");
        TeamTasks.TryAdd(task9.Id, task9);
    }
}
