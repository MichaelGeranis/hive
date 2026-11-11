namespace Hive.Core.Entities;

/// <summary>
/// The 0–5 scale a skill on a career plan is rated on. 0 is "not started"; 1–5 line up with the
/// knowledge levels of <see cref="ProjectKnowledge"/>.
/// </summary>
public static class SkillLevel
{
    public const int Min = 0;
    public const int Max = 5;

    /// <summary>
    /// Practising a skill without help: from here a person has it.
    /// </summary>
    public const int Practising = 3;

    public static string GetLabel(int level) => level switch
    {
        0 => "Not started",
        1 => "Aware",
        2 => "Learning",
        3 => "Practising",
        4 => "Proficient",
        5 => "Expert",
        _ => "Unknown"
    };

    /// <summary>
    /// Throws when <paramref name="level"/> is outside 0–5.
    /// </summary>
    public static void Validate(int level, string paramName)
    {
        if (level < Min || level > Max)
        {
            throw new ArgumentException($"Skill level must be between {Min} and {Max}.", paramName);
        }
    }
}
