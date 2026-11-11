using Hive.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for SQLite persistence.
/// </summary>
public class HiveDbContext : DbContext
{
    public HiveDbContext(DbContextOptions<HiveDbContext> options) : base(options)
    {
    }

    public DbSet<DirectReport> DirectReports => Set<DirectReport>();
    public DbSet<OneOnOneMeeting> OneOnOneMeetings => Set<OneOnOneMeeting>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProjectProduct> ProjectProducts => Set<ProjectProduct>();
    public DbSet<TeamTask> TeamTasks => Set<TeamTask>();
    public DbSet<Leave> Leaves => Set<Leave>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<ManagerNote> ManagerNotes => Set<ManagerNote>();
    public DbSet<NoteFolder> NoteFolders => Set<NoteFolder>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<SprintCapacity> SprintCapacities => Set<SprintCapacity>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ProjectKnowledge> ProjectKnowledge => Set<ProjectKnowledge>();
    public DbSet<SentimentAnalysisCache> SentimentAnalysisCache => Set<SentimentAnalysisCache>();
    public DbSet<RetroSummary> RetroSummaries => Set<RetroSummary>();
    public DbSet<TaskReadinessReview> TaskReadinessReviews => Set<TaskReadinessReview>();
    public DbSet<Quarter> Quarters => Set<Quarter>();
    public DbSet<Initiative> Initiatives => Set<Initiative>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<SprintGoal> SprintGoals => Set<SprintGoal>();
    public DbSet<InitiativeDependency> InitiativeDependencies => Set<InitiativeDependency>();
    public DbSet<InitiativeMember> InitiativeMembers => Set<InitiativeMember>();
    public DbSet<ExternalDependency> ExternalDependencies => Set<ExternalDependency>();
    public DbSet<QuarterMember> QuarterMembers => Set<QuarterMember>();
    public DbSet<InitiativeProduct> InitiativeProducts => Set<InitiativeProduct>();
    public DbSet<SkillRoadmap> SkillRoadmaps => Set<SkillRoadmap>();
    public DbSet<RoadmapSkill> RoadmapSkills => Set<RoadmapSkill>();
    public DbSet<CareerPlan> CareerPlans => Set<CareerPlan>();
    public DbSet<CareerPlanSkill> CareerPlanSkills => Set<CareerPlanSkill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DirectReport configuration
        modelBuilder.Entity<DirectReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.JobTitle).HasMaxLength(200);
            entity.Property(e => e.Department).HasMaxLength(200);
            entity.Ignore(e => e.FullName); // Computed property
        });

        // OneOnOneMeeting configuration
        // Simplified for note tracking - no scheduling workflow
        modelBuilder.Entity<OneOnOneMeeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            // The body is deliberately uncapped: a 1:1 is a place to write freely.
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => e.MeetingDate);
        });

        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(4000);
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.JiraComponentId).HasMaxLength(50);
            entity.HasIndex(e => e.JiraComponentId).IsUnique();
            entity.Ignore(e => e.IsFromJira);
        });

        // ProjectProduct configuration
        modelBuilder.Entity<ProjectProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.ProjectId, e.ProductId }).IsUnique();
        });

        // TeamTask configuration
        modelBuilder.Entity<TeamTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            // No limit: a Jira description is stored whole
            entity.Property(e => e.Description);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Labels).HasMaxLength(1000);
            entity.Property(e => e.Components).HasMaxLength(1000);
            entity.Property(e => e.Sprint).HasMaxLength(200);
            entity.Property(e => e.OverriddenFields).HasMaxLength(500);
            entity.HasIndex(e => e.AssigneeId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
        });

        // Leave configuration
        // Simple tracking for capacity planning - approvals handled externally (e.g., HiBob)
        modelBuilder.Entity<Leave>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.EndDate);
            entity.Property(e => e.Status).IsRequired().HasDefaultValue(LeaveStatus.Active);
            entity.Ignore(e => e.DaysCount); // Computed property
            entity.Ignore(e => e.BusinessDaysCount); // Computed property
        });

        // AppSettings configuration
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClaudeApiKey).HasMaxLength(500);
            entity.Property(e => e.SentimentAnalysisDays).HasDefaultValue(90);
            entity.Property(e => e.SentimentAnalysisEnabled).HasDefaultValue(false);
            entity.Property(e => e.RetroSummaryEnabled).HasDefaultValue(false);
            entity.Property(e => e.RetroSummaryCount).HasDefaultValue(Hive.Core.Entities.AppSettings.DefaultRetroSummaryCount);
            entity.Property(e => e.TaskReviewEnabled).HasDefaultValue(false);
            entity.Property(e => e.OneOnOneCadenceDays).HasDefaultValue(OneOnOneCadence.DefaultDays);
            entity.Property(e => e.StaleTaskDays).HasDefaultValue(TeamTask.DefaultStaleAfterDays);
            entity.Property(e => e.MinMemberProducts).HasDefaultValue(Hive.Core.Entities.AppSettings.DefaultMinMemberProducts);
            entity.Ignore(e => e.HasClaudeApiKey); // Computed property
            entity.Ignore(e => e.IsSentimentProviderConfigured); // Computed property
            entity.Property(e => e.SentimentProvider).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.OllamaBaseUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.OllamaModel).HasMaxLength(200).IsRequired();
            entity.Property(e => e.JiraEmail).HasMaxLength(255);
            entity.Property(e => e.JiraApiToken).HasMaxLength(500);
            entity.Ignore(e => e.HasJiraApiToken); // Computed property
            entity.Property(e => e.JiraProjectKey).HasMaxLength(20);
            entity.Property(e => e.GoogleCalendarUrl).HasMaxLength(2000);
            entity.Ignore(e => e.HasGoogleCalendarUrl); // Computed property
        });

        // ManagerNote configuration
        modelBuilder.Entity<ManagerNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            // The body is deliberately uncapped: a note is a place to write freely.
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.HasIndex(e => e.FolderId);
            entity.HasIndex(e => e.IsPinned);
        });

        // NoteFolder configuration
        modelBuilder.Entity<NoteFolder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.ParentFolderId);
        });

        // Sprint configuration
        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.TeamName).HasMaxLength(50);
            entity.HasIndex(e => e.TeamName);
            entity.HasIndex(e => new { e.Year, e.Quarter });
        });

        // SprintCapacity configuration
        modelBuilder.Entity<SprintCapacity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SprintId).IsUnique();
        });

        // Parent configuration
        modelBuilder.Entity<Parent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Labels).HasMaxLength(1000);
            entity.Property(e => e.JiraKey).HasMaxLength(50);
            entity.Property(e => e.JiraIssueType).HasMaxLength(50);
            entity.Property(e => e.Components).HasMaxLength(1000);
            entity.HasIndex(e => e.JiraKey).IsUnique();
        });

        // Activity configuration
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => new { e.Timestamp, e.EntityType });
        });

        // ProjectKnowledge configuration
        modelBuilder.Entity<ProjectKnowledge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.DirectReportId, e.ProjectId }).IsUnique();
        });

        // SentimentAnalysisCache configuration
        modelBuilder.Entity<SentimentAnalysisCache>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OverallSentiment).HasMaxLength(50).IsRequired();
            entity.Property(e => e.KeyThemesJson).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.TrendDataJson).HasMaxLength(8000).IsRequired();
            entity.Property(e => e.Reasoning).HasMaxLength(Hive.Core.Entities.SentimentAnalysisCache.MaxReasoningLength).IsRequired();
            entity.HasIndex(e => e.DirectReportId).IsUnique();
            entity.HasIndex(e => e.AnalyzedAt);
        });

        // RetroSummary configuration
        modelBuilder.Entity<RetroSummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Summary).HasMaxLength(Hive.Core.Entities.RetroSummary.MaxSummaryLength).IsRequired();
            entity.Property(e => e.WarningsJson).HasMaxLength(Hive.Core.Entities.RetroSummary.MaxJsonLength).IsRequired();
            entity.Property(e => e.ActionItemsJson).HasMaxLength(Hive.Core.Entities.RetroSummary.MaxJsonLength).IsRequired();
            entity.Property(e => e.SuggestionsJson).HasMaxLength(Hive.Core.Entities.RetroSummary.MaxJsonLength).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(Hive.Core.Entities.RetroSummary.MaxModelLength).IsRequired();
            entity.Property(e => e.SourceFingerprint).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.AnalyzedAt);
        });

        // TaskReadinessReview configuration
        modelBuilder.Entity<TaskReadinessReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Verdict).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Summary).HasMaxLength(Hive.Core.Entities.TaskReadinessReview.MaxSummaryLength).IsRequired();
            entity.Property(e => e.MissingJson).HasMaxLength(Hive.Core.Entities.TaskReadinessReview.MaxJsonLength).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(Hive.Core.Entities.TaskReadinessReview.MaxModelLength).IsRequired();
            entity.Property(e => e.SourceFingerprint).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TeamTaskId).IsUnique();
        });

        // Quarter configuration
        modelBuilder.Entity<Quarter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OkrReference).HasMaxLength(2000);
            // A share of 0% is a real value, not "unset": the sentinel stops EF inserting the default instead
            entity.Property(e => e.ProductRoadmapPercent).HasDefaultValue(Quarter.DefaultProductRoadmapPercent).HasSentinel(-1);
            entity.Property(e => e.TechRoadmapPercent).HasDefaultValue(Quarter.DefaultTechRoadmapPercent).HasSentinel(-1);
            entity.Property(e => e.MaintenancePercent).HasDefaultValue(Quarter.DefaultMaintenancePercent).HasSentinel(-1);
            entity.Property(e => e.Baseline).IsRequired().HasDefaultValue(string.Empty);
            entity.HasIndex(e => new { e.Year, e.QuarterNumber }).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        // Initiative configuration
        modelBuilder.Entity<Initiative>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.TshirtSize).HasMaxLength(8);
            // Maintenance is 0, a real value: without the sentinel EF would store the default instead
            entity.Property(e => e.WorkType).HasDefaultValue(WorkType.ProductRoadmap).HasSentinel((WorkType)(-1));
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.DecisionNote).HasMaxLength(Initiative.MaxDecisionNoteLength).IsRequired().HasDefaultValue(string.Empty);
            entity.HasIndex(e => e.QuarterId);
        });

        // ExternalDependency configuration
        modelBuilder.Entity<ExternalDependency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Team).HasMaxLength(ExternalDependency.MaxTeamLength).IsRequired();
            entity.Property(e => e.Need).HasMaxLength(ExternalDependency.MaxNeedLength).IsRequired();
            entity.HasIndex(e => e.InitiativeId);
        });

        // QuarterMember configuration
        modelBuilder.Entity<QuarterMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.QuarterId, e.DirectReportId }).IsUnique();
        });

        // InitiativeProduct configuration
        modelBuilder.Entity<InitiativeProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.InitiativeId, e.ProductId }).IsUnique();
        });

        // InitiativeMember configuration
        modelBuilder.Entity<InitiativeMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InitiativeId);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => new { e.InitiativeId, e.DirectReportId }).IsUnique();
        });

        // Allocation configuration
        modelBuilder.Entity<Allocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InitiativeId);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => e.SprintId);
            entity.Property(e => e.Percent).HasDefaultValue(100);
            entity.HasIndex(e => new { e.InitiativeId, e.DirectReportId, e.SprintId }).IsUnique();
        });

        // SprintGoal configuration
        modelBuilder.Entity<SprintGoal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Goal).HasMaxLength(4000);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.QuarterId);
            entity.HasIndex(e => e.SprintId);
            entity.HasIndex(e => new { e.QuarterId, e.SprintId }).IsUnique();
        });

        // InitiativeDependency configuration
        modelBuilder.Entity<InitiativeDependency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.DependentInitiativeId);
            entity.HasIndex(e => e.DependencyInitiativeId);
            entity.HasIndex(e => new { e.DependentInitiativeId, e.DependencyInitiativeId }).IsUnique();
        });

        // SkillRoadmap configuration
        modelBuilder.Entity<SkillRoadmap>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(SkillRoadmap.MaxNameLength).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(SkillRoadmap.MaxDescriptionLength);
            entity.Property(e => e.LibraryKey).HasMaxLength(SkillRoadmap.MaxLibraryKeyLength);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // RoadmapSkill configuration
        modelBuilder.Entity<RoadmapSkill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Section).HasMaxLength(RoadmapSkill.MaxSectionLength).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(RoadmapSkill.MaxTitleLength).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(RoadmapSkill.MaxDescriptionLength);
            entity.Property(e => e.FromStage).HasDefaultValue(CareerStage.Junior).HasSentinel((CareerStage)(-1));
            entity.HasIndex(e => e.RoadmapId);
        });

        // CareerPlan configuration
        modelBuilder.Entity<CareerPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoadmapName).HasMaxLength(CareerPlan.MaxRoadmapNameLength).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(CareerPlan.MaxNotesLength);
            entity.Property(e => e.CurrentStage).HasDefaultValue(CareerStage.Mid).HasSentinel((CareerStage)(-1));
            entity.HasIndex(e => e.DirectReportId).IsUnique();
        });

        // CareerPlanSkill configuration
        modelBuilder.Entity<CareerPlanSkill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Section).HasMaxLength(RoadmapSkill.MaxSectionLength).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(RoadmapSkill.MaxTitleLength).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(RoadmapSkill.MaxDescriptionLength);
            entity.Property(e => e.Evidence).HasMaxLength(CareerPlanSkill.MaxEvidenceLength);
            entity.Property(e => e.FromStage).HasDefaultValue(CareerStage.Junior).HasSentinel((CareerStage)(-1));
            entity.Ignore(e => e.Has);
            entity.Ignore(e => e.Status);
            entity.HasIndex(e => e.CareerPlanId);
        });
    }

    /// <summary>
    /// Seeds initial data into the database.
    /// </summary>
    public void SeedData()
    {
        if (DirectReports.Any()) return; // Already seeded

        // Create direct reports
        var alice = new DirectReport("Alice", "Johnson", "alice.johnson@company.com", "Senior Software Engineer", "Engineering", new DateTime(2022, 3, 15));
        var bob = new DirectReport("Bob", "Smith", "bob.smith@company.com", "Software Engineer", "Engineering", new DateTime(2023, 1, 10));
        var carol = new DirectReport("Carol", "Williams", "carol.williams@company.com", "Staff Engineer", "Engineering", new DateTime(2021, 6, 1));

        DirectReports.AddRange(alice, bob, carol);
        SaveChanges();

        // Create 1:1 meetings - each one is a single markdown note tagged with the person
        var meetings = new[]
        {
            new OneOnOneMeeting(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)),
                "Weekly sync\n\n- Discussed the project timeline, still on track\n- Reviewing PR #123 together this week",
                "alice",
                alice.Id),
            new OneOnOneMeeting(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                "Bi-weekly check-in\n\nCareer growth discussion - wants to move towards a staff role.",
                "bob",
                bob.Id),
            new OneOnOneMeeting(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-21)),
                "Monthly review\n\nCompleted a major milestone. Recognised it in the team channel.",
                "carol",
                carol.Id)
        };

        OneOnOneMeetings.AddRange(meetings);
        SaveChanges();

        // Create projects
        var projects = new[]
        {
            new Project("API Modernization", "Upgrade legacy APIs to .NET 8", "https://github.com/example/api-modernization"),
            new Project("Mobile App", "New mobile application", "https://github.com/example/mobile-app"),
            new Project("Infrastructure Migration", "Move to Kubernetes"),
            new Project("Documentation", "Update all technical docs")
        };

        Projects.AddRange(projects);
        SaveChanges();

        // Create tasks
        var tasks = new[]
        {
            new TeamTask("Implement user authentication", description: "Add JWT auth to API", type: TaskType.Story, priority: TaskPriority.High, assigneeId: alice.Id, projectId: projects[0].Id, dueDate: DateTime.UtcNow.AddDays(7), storyPoints: 3),
            new TeamTask("Fix login bug", description: "Users can't login with SSO", type: TaskType.Bug, priority: TaskPriority.Critical, assigneeId: bob.Id, projectId: projects[0].Id, dueDate: DateTime.UtcNow.AddDays(2), storyPoints: 1),
            new TeamTask("Write unit tests", description: "Add tests for auth module", type: TaskType.Task, priority: TaskPriority.Medium, assigneeId: alice.Id, projectId: projects[0].Id, dueDate: DateTime.UtcNow.AddDays(14), storyPoints: 2),
            new TeamTask("Design mobile UI", description: "Create mockups for mobile app", type: TaskType.Story, priority: TaskPriority.High, assigneeId: null, projectId: projects[1].Id, dueDate: DateTime.UtcNow.AddDays(21), storyPoints: 5),
            new TeamTask("Research K8s options", description: "Evaluate managed K8s providers", type: TaskType.Spike, priority: TaskPriority.Medium, assigneeId: carol.Id, projectId: projects[2].Id, dueDate: DateTime.UtcNow.AddDays(30), storyPoints: 3),
            new TeamTask("Update README", description: "Update project documentation", type: TaskType.Task, priority: TaskPriority.Low, assigneeId: bob.Id, projectId: projects[3].Id, dueDate: DateTime.UtcNow.AddDays(-5), storyPoints: 1)
        };

        tasks[0].MoveToTodo();
        tasks[0].Start();
        tasks[1].MoveToTodo();
        tasks[1].Start();
        tasks[1].MoveToReview();
        tasks[2].MoveToTodo();
        tasks[5].MoveToTodo();
        tasks[5].Start();
        tasks[5].Complete();

        TeamTasks.AddRange(tasks);
        SaveChanges();

        // Create leaves - simple tracking for capacity planning
        var leave1 = new Leave(alice.Id, LeaveType.Vacation, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-25), "Summer vacation");
        var leave2 = new Leave(alice.Id, LeaveType.Vacation, DateTime.UtcNow.AddDays(14), DateTime.UtcNow.AddDays(15), "Personal time");
        var leave3 = new Leave(bob.Id, LeaveType.Sick, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), "Not feeling well");
        var leave4 = new Leave(bob.Id, LeaveType.Vacation, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(40), "Winter holiday");
        var leave5 = new Leave(carol.Id, LeaveType.Vacation, DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-58), "Family event");
        var leave6 = new Leave(carol.Id, LeaveType.Other, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(9), "Tech conference");

        Leaves.AddRange(leave1, leave2, leave3, leave4, leave5, leave6);
        SaveChanges();
    }
}
