using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SkillRoadmapTests
{
    [Fact]
    public void Constructor_WithValidData_TrimsAndSetsFields()
    {
        var roadmap = new SkillRoadmap("  Backend Engineer ", " APIs and databases ", "backend");

        roadmap.Id.Should().NotBeEmpty();
        roadmap.Name.Should().Be("Backend Engineer");
        roadmap.Description.Should().Be("APIs and databases");
        roadmap.LibraryKey.Should().Be("backend");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string name)
    {
        var act = () => new SkillRoadmap(name);

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_NameTooLong_Throws()
    {
        var act = () => new SkillRoadmap(new string('a', SkillRoadmap.MaxNameLength + 1));

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_DescriptionTooLong_Throws()
    {
        var act = () => new SkillRoadmap("Backend", new string('a', SkillRoadmap.MaxDescriptionLength + 1));

        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Constructor_BlankLibraryKey_Throws()
    {
        var act = () => new SkillRoadmap("Backend", libraryKey: " ");

        act.Should().Throw<ArgumentException>().WithParameterName("libraryKey");
    }

    [Fact]
    public void Update_ChangesNameAndDescription()
    {
        var roadmap = new SkillRoadmap("Backend");

        roadmap.Update("Backend Engineer", null);

        roadmap.Name.Should().Be("Backend Engineer");
        roadmap.Description.Should().BeEmpty();
        roadmap.UpdatedAt.Should().NotBeNull();
    }
}
