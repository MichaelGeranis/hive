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
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillCategoryEntity> SkillCategories => Set<SkillCategoryEntity>();
    public DbSet<SkillAssessment> SkillAssessments => Set<SkillAssessment>();
    public DbSet<OneOnOneMeeting> OneOnOneMeetings => Set<OneOnOneMeeting>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TeamTask> TeamTasks => Set<TeamTask>();
    public DbSet<Leave> Leaves => Set<Leave>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<ManagerNote> ManagerNotes => Set<ManagerNote>();
    public DbSet<NoteFolder> NoteFolders => Set<NoteFolder>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<SprintCapacity> SprintCapacities => Set<SprintCapacity>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<ChecklistTemplateItem> ChecklistTemplateItems => Set<ChecklistTemplateItem>();
    public DbSet<ChecklistInstance> ChecklistInstances => Set<ChecklistInstance>();
    public DbSet<ChecklistInstanceItem> ChecklistInstanceItems => Set<ChecklistInstanceItem>();
    public DbSet<ProjectKnowledge> ProjectKnowledge => Set<ProjectKnowledge>();
    public DbSet<SentimentAnalysisCache> SentimentAnalysisCache => Set<SentimentAnalysisCache>();
    public DbSet<Quarter> Quarters => Set<Quarter>();
    public DbSet<Initiative> Initiatives => Set<Initiative>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<SprintGoal> SprintGoals => Set<SprintGoal>();
    public DbSet<InitiativeDependency> InitiativeDependencies => Set<InitiativeDependency>();
    public DbSet<InitiativeMember> InitiativeMembers => Set<InitiativeMember>();
    public DbSet<KnowledgePoint> KnowledgePoints => Set<KnowledgePoint>();

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

        // PerformanceReview configuration
        modelBuilder.Entity<PerformanceReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReviewPeriod).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Strengths).HasMaxLength(4000);
            entity.Property(e => e.AreasForImprovement).HasMaxLength(4000);
            entity.Property(e => e.ManagerNotes).HasMaxLength(4000);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => new { e.DirectReportId, e.ReviewPeriod }).IsUnique();
        });

        // SkillCategory configuration
        modelBuilder.Entity<SkillCategoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Skill configuration
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.SkillCategoryId);
        });

        // SkillAssessment configuration
        modelBuilder.Entity<SkillAssessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => e.SkillId);
            entity.HasIndex(e => new { e.DirectReportId, e.SkillId }).IsUnique();
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
            entity.Property(e => e.Labels).HasMaxLength(1000);
        });

        // TeamTask configuration
        modelBuilder.Entity<TeamTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);
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
            entity.Property(e => e.StoryPointMappings).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.ClaudeApiKey).HasMaxLength(500);
            entity.Property(e => e.SentimentAnalysisDays).HasDefaultValue(90);
            entity.Property(e => e.SentimentAnalysisEnabled).HasDefaultValue(false);
            entity.Ignore(e => e.HasClaudeApiKey); // Computed property
        });

        // ManagerNote configuration
        modelBuilder.Entity<ManagerNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            // The body is deliberately uncapped: a note is a place to write freely.
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.HasIndex(e => e.IsCompleted);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.DueDate);
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

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).HasMaxLength(4000);
            entity.Property(e => e.Url).HasMaxLength(2000);
            entity.Property(e => e.Tags).HasMaxLength(1000);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Parent configuration
        modelBuilder.Entity<Parent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Labels).HasMaxLength(1000);
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

        // ChecklistTemplate configuration
        modelBuilder.Entity<ChecklistTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.Type, e.IsActive });
        });

        // ChecklistTemplateItem configuration
        modelBuilder.Entity<ChecklistTemplateItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.HelpText).HasMaxLength(1000);
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => new { e.TemplateId, e.SortOrder });
        });

        // ChecklistInstance configuration
        modelBuilder.Entity<ChecklistInstance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.CandidateName).HasMaxLength(200);
            entity.Property(e => e.Position).HasMaxLength(200);
            entity.Property(e => e.NewHireName).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(4000);
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.Type, e.Status });
        });

        // ChecklistInstanceItem configuration
        modelBuilder.Entity<ChecklistInstanceItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(4000);
            entity.Property(e => e.Assignee).HasMaxLength(200);
            entity.HasIndex(e => e.InstanceId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.InstanceId, e.SortOrder });
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
            entity.HasIndex(e => e.DirectReportId).IsUnique();
            entity.HasIndex(e => e.AnalyzedAt);
        });

        // Quarter configuration
        modelBuilder.Entity<Quarter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OkrReference).HasMaxLength(2000);
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
            entity.Property(e => e.TshirtSize).HasMaxLength(5);
            entity.Property(e => e.WorkType).HasDefaultValue(WorkType.ProductRoadmap);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.HasIndex(e => e.QuarterId);
            entity.HasIndex(e => e.ProjectId);
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

        // KnowledgePoint configuration
        modelBuilder.Entity<KnowledgePoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(4000);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.DirectReportId, e.ProjectId }).IsUnique();
        });
    }
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

        // Create skill categories
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

        SkillCategories.AddRange(technicalCategory, softSkillsCategory, leadershipCategory, domainCategory, toolsCategory);
        SaveChanges();

        // Create skills
        var skills = new[]
        {
            new Skill("C#", "C# programming language", technicalCategory.Id),
            new Skill("TypeScript", "TypeScript programming", technicalCategory.Id),
            new Skill("React", "React frontend framework", technicalCategory.Id),
            new Skill("SQL", "SQL databases", technicalCategory.Id),
            new Skill("Communication", "Verbal and written communication", softSkillsCategory.Id),
            new Skill("Problem Solving", "Analytical problem solving", softSkillsCategory.Id),
            new Skill("Leadership", "Team leadership", leadershipCategory.Id),
            new Skill("Mentoring", "Mentoring junior developers", leadershipCategory.Id),
            new Skill("Git", "Git version control", toolsCategory.Id),
            new Skill("Docker", "Docker containerization", toolsCategory.Id)
        };

        Skills.AddRange(skills);
        SaveChanges();

        // Create skill assessments
        SkillAssessments.AddRange(
            new SkillAssessment(alice.Id, skills[0].Id, ProficiencyLevel.Expert, ProficiencyLevel.Expert),
            new SkillAssessment(alice.Id, skills[1].Id, ProficiencyLevel.Advanced, ProficiencyLevel.Expert),
            new SkillAssessment(bob.Id, skills[0].Id, ProficiencyLevel.Intermediate, ProficiencyLevel.Advanced),
            new SkillAssessment(bob.Id, skills[2].Id, ProficiencyLevel.Advanced, ProficiencyLevel.Expert),
            new SkillAssessment(carol.Id, skills[0].Id, ProficiencyLevel.Expert, ProficiencyLevel.Expert),
            new SkillAssessment(carol.Id, skills[6].Id, ProficiencyLevel.Advanced, ProficiencyLevel.Expert)
        );
        SaveChanges();

        // Create performance reviews
        var reviews = new[]
        {
            new PerformanceReview(alice.Id, "2024 H1", DateTime.UtcNow.AddMonths(-6)),
            new PerformanceReview(bob.Id, "2024 H1", DateTime.UtcNow.AddMonths(-6)),
            new PerformanceReview(carol.Id, "2024 H1", DateTime.UtcNow.AddMonths(-6))
        };

        reviews[0].UpdateContent("Excellent technical skills", "Could improve documentation", "Great team player", PerformanceRating.ExceedsExpectations);
        reviews[1].UpdateContent("Good progress", "Need more ownership", "Improving steadily", PerformanceRating.MeetsExpectations);
        reviews[2].UpdateContent("Outstanding leadership", "Delegate more", "Role model for the team", PerformanceRating.Outstanding);

        PerformanceReviews.AddRange(reviews);
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
            new Project("API Modernization", "Upgrade legacy APIs to .NET 8", "backend,api", "https://github.com/example/api-modernization"),
            new Project("Mobile App", "New mobile application", "mobile,frontend", "https://github.com/example/mobile-app"),
            new Project("Infrastructure Migration", "Move to Kubernetes", "infrastructure,devops"),
            new Project("Documentation", "Update all technical docs", "docs")
        };

        Projects.AddRange(projects);
        SaveChanges();

        // Create tasks
        var tasks = new[]
        {
            new TeamTask("Implement user authentication", "Add JWT auth to API", TaskType.Story, TaskPriority.High, alice.Id, projects[0].Id, DateTime.UtcNow.AddDays(7), 16),
            new TeamTask("Fix login bug", "Users can't login with SSO", TaskType.Bug, TaskPriority.Critical, bob.Id, projects[0].Id, DateTime.UtcNow.AddDays(2), 4),
            new TeamTask("Write unit tests", "Add tests for auth module", TaskType.Task, TaskPriority.Medium, alice.Id, projects[0].Id, DateTime.UtcNow.AddDays(14), 8),
            new TeamTask("Design mobile UI", "Create mockups for mobile app", TaskType.Story, TaskPriority.High, null, projects[1].Id, DateTime.UtcNow.AddDays(21), 24),
            new TeamTask("Research K8s options", "Evaluate managed K8s providers", TaskType.Spike, TaskPriority.Medium, carol.Id, projects[2].Id, DateTime.UtcNow.AddDays(30), 16),
            new TeamTask("Update README", "Update project documentation", TaskType.Task, TaskPriority.Low, bob.Id, projects[3].Id, DateTime.UtcNow.AddDays(-5), 2)
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
