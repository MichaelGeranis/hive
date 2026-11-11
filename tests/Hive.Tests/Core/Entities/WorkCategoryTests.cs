using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class WorkCategoryTests
{
    [Theory]
    [InlineData("Tech Maintenance", WorkCategory.TechMaintenance)]
    [InlineData("TECH-MAINTENANCE 2026", WorkCategory.TechMaintenance)]
    [InlineData("Tech Debt", WorkCategory.TechDebt)]
    [InlineData("Q3 tech debt", WorkCategory.TechDebt)]
    [InlineData("Tech Roadmap", WorkCategory.TechRoadmap)]
    [InlineData("System Improvements", WorkCategory.SystemImprovements)]
    [InlineData("System improvement", WorkCategory.SystemImprovements)]
    [InlineData("Rights Manager onboarding", WorkCategory.Productisation)]
    [InlineData("Technical design", WorkCategory.Productisation)]
    [InlineData("", WorkCategory.Productisation)]
    [InlineData(null, WorkCategory.Productisation)]
    public void FromEpicName_NamesTheCategory(string? name, WorkCategory expected)
    {
        WorkCategories.FromEpicName(name).Should().Be(expected);
    }

    [Fact]
    public void For_SubTask_TakesTheCategoryOfItsStorysEpic()
    {
        var epic = Parent.FromJira("MAR-1", "Epic", "Tech Debt");
        var story = new TeamTask("Story", type: TaskType.Story, tags: "jira:MAR-2", parentId: epic.Id);
        var storyParent = Parent.FromJira("MAR-2", "Story", "Story");
        var subTask = new TeamTask("Sub-task", type: TaskType.SubTask, parentId: storyParent.Id);

        var categories = new WorkCategories([epic, storyParent], [story, subTask]);

        categories.FindEpic(subTask).Should().BeSameAs(epic);
        categories.For(subTask).Should().Be(WorkCategory.TechDebt);
        categories.For(story).Should().Be(WorkCategory.TechDebt);
    }

    [Fact]
    public void For_ParentLinkedByItsTask_FollowsTheLink()
    {
        var epic = new Parent("System Improvements");
        var story = new TeamTask("Story", parentId: epic.Id);
        var storyParent = new Parent("Story", teamTaskId: story.Id);
        var subTask = new TeamTask("Sub-task", parentId: storyParent.Id);

        new WorkCategories([epic, storyParent], [story, subTask]).For(subTask)
            .Should().Be(WorkCategory.SystemImprovements);
    }

    [Fact]
    public void For_TaskWithNoEpic_IsProductisation()
    {
        var task = new TeamTask("Loose");

        var categories = new WorkCategories([], [task]);

        categories.FindEpic(task).Should().BeNull();
        categories.For(task).Should().Be(WorkCategory.Productisation);
    }

    [Fact]
    public void For_ParentCycle_StopsWithoutLooping()
    {
        var parent = Parent.FromJira("MAR-5", "Story", "Tech Roadmap");
        var task = new TeamTask("Loop", tags: "jira:MAR-5", parentId: parent.Id);

        new WorkCategories([parent], [task]).For(task).Should().Be(WorkCategory.TechRoadmap);
    }

    [Fact]
    public void FromEpicName_WithTheManagersNames_UsesThem()
    {
        var names = new Dictionary<WorkCategory, IReadOnlyList<string>>
        {
            [WorkCategory.TechMaintenance] = ["KTLO", "Keep the lights on"],
            [WorkCategory.TechDebt] = []
        };

        WorkCategories.FromEpicName("Q4 KTLO", names).Should().Be(WorkCategory.TechMaintenance);
        WorkCategories.FromEpicName("Keep the lights on", names).Should().Be(WorkCategory.TechMaintenance);
        WorkCategories.FromEpicName("Tech Debt", names).Should().Be(WorkCategory.Productisation, "Tech Debt has no names now");
    }

    [Fact]
    public void FromEpicName_WhenNamesOfTwoCategoriesFit_TheLongestWins()
    {
        var names = new Dictionary<WorkCategory, IReadOnlyList<string>>
        {
            [WorkCategory.TechRoadmap] = ["Tech"],
            [WorkCategory.TechDebt] = ["Tech Debt"]
        };

        WorkCategories.FromEpicName("Tech Debt 2026", names).Should().Be(WorkCategory.TechDebt);
        WorkCategories.FromEpicName("Tech platform", names).Should().Be(WorkCategory.TechRoadmap);
    }

    [Fact]
    public void For_WithTheManagersNames_ReadsTheEpicWithThem()
    {
        var epic = new Parent("Keep the lights on");
        var task = new TeamTask("Upgrade", parentId: epic.Id);
        var names = new Dictionary<WorkCategory, IReadOnlyList<string>> { [WorkCategory.TechMaintenance] = ["Keep the lights on"] };

        new WorkCategories([epic], [task], names).For(task).Should().Be(WorkCategory.TechMaintenance);
    }
}
