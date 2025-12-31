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
    public DbSet<SkillAssessment> SkillAssessments => Set<SkillAssessment>();
    public DbSet<OneOnOneMeeting> OneOnOneMeetings => Set<OneOnOneMeeting>();
    public DbSet<MeetingNote> MeetingNotes => Set<MeetingNote>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TeamTask> TeamTasks => Set<TeamTask>();
    public DbSet<Leave> Leaves => Set<Leave>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<ManagerNote> ManagerNotes => Set<ManagerNote>();

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
            entity.Property(e => e.GoalsForNextPeriod).HasMaxLength(4000);
            entity.Property(e => e.ManagerNotes).HasMaxLength(4000);
            entity.Property(e => e.EmployeeSelfAssessment).HasMaxLength(4000);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => new { e.DirectReportId, e.ReviewPeriod }).IsUnique();
        });

        // Skill configuration
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(1000);
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
        modelBuilder.Entity<OneOnOneMeeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.Agenda).HasMaxLength(4000);
            entity.HasIndex(e => e.DirectReportId);
            entity.HasIndex(e => e.ScheduledDate);
        });

        // MeetingNote configuration
        modelBuilder.Entity<MeetingNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.ActionAssignee).HasMaxLength(200);
            entity.HasIndex(e => e.MeetingId);
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
            entity.Property(e => e.Sprint).HasMaxLength(200);
            entity.HasIndex(e => e.AssigneeId);
            entity.HasIndex(e => e.ProjectId);
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
            entity.Ignore(e => e.DaysCount); // Computed property
            entity.Ignore(e => e.BusinessDaysCount); // Computed property
        });

        // AppSettings configuration
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StoryPointMappings).HasMaxLength(4000).IsRequired();
        });

        // ManagerNote configuration
        modelBuilder.Entity<ManagerNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).HasMaxLength(4000);
            entity.HasIndex(e => e.IsCompleted);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.DueDate);
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

        // Create skills
        var skills = new[]
        {
            new Skill("C#", "C# programming language", SkillCategory.Technical),
            new Skill("TypeScript", "TypeScript programming", SkillCategory.Technical),
            new Skill("React", "React frontend framework", SkillCategory.Technical),
            new Skill("SQL", "SQL databases", SkillCategory.Technical),
            new Skill("Communication", "Verbal and written communication", SkillCategory.SoftSkills),
            new Skill("Problem Solving", "Analytical problem solving", SkillCategory.SoftSkills),
            new Skill("Leadership", "Team leadership", SkillCategory.Leadership),
            new Skill("Mentoring", "Mentoring junior developers", SkillCategory.Leadership),
            new Skill("Git", "Git version control", SkillCategory.Tools),
            new Skill("Docker", "Docker containerization", SkillCategory.Tools)
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

        reviews[0].UpdateContent("Excellent technical skills", "Could improve documentation", "Lead a major feature", "Great team player", PerformanceRating.ExceedsExpectations);
        reviews[0].Submit();
        reviews[0].Acknowledge();
        reviews[0].Complete();

        reviews[1].UpdateContent("Good progress", "Need more ownership", "Complete certification", "Improving steadily", PerformanceRating.MeetsExpectations);
        reviews[1].Submit();

        reviews[2].UpdateContent("Outstanding leadership", "Delegate more", "Mentor 2 engineers", "Role model for the team", PerformanceRating.Outstanding);
        reviews[2].Submit();
        reviews[2].Acknowledge();
        reviews[2].Complete();

        PerformanceReviews.AddRange(reviews);
        SaveChanges();

        // Create 1:1 meetings
        var meetings = new[]
        {
            new OneOnOneMeeting(alice.Id, DateTime.UtcNow.AddDays(-14), 30, "Conference Room A", "Weekly sync"),
            new OneOnOneMeeting(alice.Id, DateTime.UtcNow.AddDays(7), 30, "Conference Room A", "Weekly sync"),
            new OneOnOneMeeting(bob.Id, DateTime.UtcNow.AddDays(-7), 30, "Virtual", "Bi-weekly check-in"),
            new OneOnOneMeeting(bob.Id, DateTime.UtcNow.AddDays(14), 30, "Virtual", "Bi-weekly check-in"),
            new OneOnOneMeeting(carol.Id, DateTime.UtcNow.AddDays(-21), 45, "Office", "Monthly review")
        };

        meetings[0].Complete();
        meetings[2].Complete();
        meetings[4].Complete();

        OneOnOneMeetings.AddRange(meetings);
        SaveChanges();

        // Create meeting notes
        MeetingNotes.AddRange(
            new MeetingNote(meetings[0].Id, "Discussed project timeline", NoteCategory.Discussion),
            new MeetingNote(meetings[0].Id, "Review PR #123 by Friday", NoteCategory.ActionItem),
            new MeetingNote(meetings[2].Id, "Career growth discussion", NoteCategory.CareerDevelopment),
            new MeetingNote(meetings[4].Id, "Completed major milestone", NoteCategory.Achievement)
        );
        SaveChanges();

        // Create projects
        var projects = new[]
        {
            new Project("API Modernization", "Upgrade legacy APIs to .NET 8", DateTime.UtcNow.AddMonths(-2), DateTime.UtcNow.AddMonths(2)),
            new Project("Mobile App", "New mobile application", DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow.AddMonths(4)),
            new Project("Infrastructure Migration", "Move to Kubernetes", null, DateTime.UtcNow.AddMonths(6)),
            new Project("Documentation", "Update all technical docs", DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow.AddMonths(-1))
        };

        projects[0].Activate();
        projects[1].Activate();
        projects[3].Activate();
        projects[3].Complete();

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
        tasks[5].Complete(3);

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
