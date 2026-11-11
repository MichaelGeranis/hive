namespace Hive.Core.Entities;

/// <summary>
/// The game on top of one direct report's career plan: XP, a player level with a title, sections
/// cleared and badges. It is shown on that person's own plan only, never next to anyone else's.
/// Everything here is derived from the plan's skills and never stored.
/// </summary>
public sealed class CareerProgress
{
    /// <summary>
    /// XP for each level a skill has reached.
    /// </summary>
    public const int XpPerSkillLevel = 10;

    /// <summary>
    /// Bonus XP for each skill the person has (Practising or above).
    /// </summary>
    public const int XpPerSkillHad = 25;

    /// <summary>
    /// How far back "recent" level-ups are counted, for the On a roll badge.
    /// </summary>
    public const int MomentumDays = 30;

    private static readonly string[] Titles =
    [
        "Rookie", "Explorer", "Adventurer", "Pathfinder", "Ranger", "Hero", "Champion", "Legend"
    ];

    public int Xp { get; private init; }
    public int PlayerLevel { get; private init; }
    public string PlayerTitle { get; private init; } = string.Empty;

    /// <summary>
    /// XP needed to reach the current player level.
    /// </summary>
    public int CurrentLevelXp { get; private init; }

    /// <summary>
    /// XP needed to reach the next player level.
    /// </summary>
    public int NextLevelXp { get; private init; }

    public int SkillCount { get; private init; }
    public int HasCount { get; private init; }
    public int WorkingOnCount { get; private init; }
    public int NotYetCount { get; private init; }
    public int SkillsLevelledUpRecently { get; private init; }

    /// <summary>
    /// The last time any skill changed: rated, or put in or out of focus.
    /// </summary>
    public DateTime? LastSkillChangeAt { get; private init; }

    public IReadOnlyList<CareerSection> Sections { get; private init; } = [];
    public IReadOnlyList<CareerBadge> Badges { get; private init; } = [];

    private CareerProgress() { }

    /// <summary>
    /// XP needed to reach <paramref name="playerLevel"/>: 0, 150, 450, 900, 1500… Each level costs
    /// 150 XP more than the one before, so a full library roadmap reaches Legend (level 8, 4200
    /// XP) only near the top of the ladder.
    /// </summary>
    public static int XpForPlayerLevel(int playerLevel) =>
        playerLevel <= 1 ? 0 : 75 * playerLevel * (playerLevel - 1);

    public static string GetTitle(int playerLevel) =>
        Titles[Math.Clamp(playerLevel, 1, Titles.Length) - 1];

    public static CareerProgress Calculate(IEnumerable<CareerPlanSkill> planSkills, CareerLadder ladder, DateTime now)
    {
        var skills = planSkills.OrderBy(s => s.SortOrder).ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase).ToList();

        var xp = skills.Sum(s => s.Level * XpPerSkillLevel + (s.Has ? XpPerSkillHad : 0));
        var playerLevel = 1;
        while (XpForPlayerLevel(playerLevel + 1) <= xp)
        {
            playerLevel++;
        }

        var sections = skills
            .GroupBy(s => s.Section, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var sectionSkills = g.ToList();
                var counted = sectionSkills.Where(s => !s.IsOptional).ToList();
                if (counted.Count == 0) counted = sectionSkills;
                return new CareerSection(
                    g.First().Section,
                    sectionSkills.Count,
                    sectionSkills.Count(s => s.Has),
                    sectionSkills.Count(s => s.IsFocus),
                    counted.All(s => s.Has));
            })
            .ToList();

        var recentLevelUps = skills.Count(s => s.LastLevelUpAt >= now.AddDays(-MomentumDays));
        var currentStageFit = ladder.Stages.Single(s => s.Stage == ladder.CurrentStage);

        var badges = new List<CareerBadge>
        {
            new("first-steps", "First steps", "Levelled up a first skill", skills.Any(s => s.Level > 0)),
            new("got-it", "Got it", "Has a first skill, at Practising or above", skills.Any(s => s.Has)),
            new("deep-diver", "Deep diver", "Took a skill all the way to Expert", skills.Any(s => s.Level == SkillLevel.Max)),
            new("on-a-roll", "On a roll", $"Levelled up 3 skills in the last {MomentumDays} days", recentLevelUps >= 3),
            new("trailblazer", "Trailblazer", "Has every skill in 3 sections", sections.Count(s => s.IsCleared) >= 3),
            new("tech-wizard", "Tech wizard", "Has 10 hard skills", skills.Count(s => s.Has && s.Category == SkillCategory.Hard) >= 10),
            new("people-person", "People person", "Has 5 soft skills", skills.Count(s => s.Has && s.Category == SkillCategory.Soft) >= 5),
            new("role-ready", "Role ready", "Meets everything their stage expects",
                currentStageFit.ExpectedCount > 0 && currentStageFit.MetCount == currentStageFit.ExpectedCount),
            new("ahead-of-the-curve", "Ahead of the curve", "Has a skill of a later stage",
                skills.Any(s => s.FromStage > ladder.CurrentStage && s.Has)),
        };

        return new CareerProgress
        {
            Xp = xp,
            PlayerLevel = playerLevel,
            PlayerTitle = GetTitle(playerLevel),
            CurrentLevelXp = XpForPlayerLevel(playerLevel),
            NextLevelXp = XpForPlayerLevel(playerLevel + 1),
            SkillCount = skills.Count,
            HasCount = skills.Count(s => s.Status == CareerSkillStatus.Has),
            WorkingOnCount = skills.Count(s => s.Status == CareerSkillStatus.WorkingOn),
            NotYetCount = skills.Count(s => s.Status == CareerSkillStatus.NotYet),
            SkillsLevelledUpRecently = recentLevelUps,
            LastSkillChangeAt = skills.Max(s => s.UpdatedAt),
            Sections = sections,
            Badges = badges
        };
    }
}

/// <summary>
/// One section of a career plan: how many of its skills the person has and is working on. A
/// section is cleared when they have every required skill in it.
/// </summary>
public sealed record CareerSection(string Name, int SkillCount, int HasCount, int WorkingOnCount, bool IsCleared);

/// <summary>
/// An achievement on a career plan, earned or still locked.
/// </summary>
public sealed record CareerBadge(string Key, string Name, string Description, bool IsEarned);
