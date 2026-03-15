using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class ChecklistServiceTests
{
    private readonly Mock<IChecklistTemplateRepository> _templateRepositoryMock;
    private readonly Mock<IChecklistTemplateItemRepository> _templateItemRepositoryMock;
    private readonly Mock<IChecklistInstanceRepository> _instanceRepositoryMock;
    private readonly Mock<IChecklistInstanceItemRepository> _instanceItemRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly ChecklistService _service;

    public ChecklistServiceTests()
    {
        _templateRepositoryMock = new Mock<IChecklistTemplateRepository>();
        _templateItemRepositoryMock = new Mock<IChecklistTemplateItemRepository>();
        _instanceRepositoryMock = new Mock<IChecklistInstanceRepository>();
        _instanceItemRepositoryMock = new Mock<IChecklistInstanceItemRepository>();
        _activityServiceMock = new Mock<IActivityService>();

        _service = new ChecklistService(
            _templateRepositoryMock.Object,
            _templateItemRepositoryMock.Object,
            _instanceRepositoryMock.Object,
            _instanceItemRepositoryMock.Object,
            _activityServiceMock.Object);
    }

    // ============== Constructor Tests ==============

    [Fact]
    public void Constructor_WithNullTemplateRepository_ThrowsArgumentNullException()
    {
        var act = () => new ChecklistService(null!, _templateItemRepositoryMock.Object,
            _instanceRepositoryMock.Object, _instanceItemRepositoryMock.Object, _activityServiceMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("templateRepository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        var act = () => new ChecklistService(_templateRepositoryMock.Object, _templateItemRepositoryMock.Object,
            _instanceRepositoryMock.Object, _instanceItemRepositoryMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("activityService");
    }

    // ============== Template Tests ==============

    [Fact]
    public async Task GetTemplateByIdAsync_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        var template = new ChecklistTemplate("Interview Template", "Description", ChecklistType.Interview);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem>());

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Name.Should().Be("Interview Template");
        result.Type.Should().Be(ChecklistType.Interview);
        result.TypeName.Should().Be("Interview");
        result.IsActive.Should().BeTrue();
        result.ItemCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplate?)null);

        // Act
        var result = await _service.GetTemplateByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplateByIdAsync_CountsItemsCorrectly()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Onboarding);
        var items = new List<ChecklistTemplateItem>
        {
            new ChecklistTemplateItem(template.Id, 0, "Item 1", ChecklistItemType.Task),
            new ChecklistTemplateItem(template.Id, 1, "Item 2", ChecklistItemType.Question)
        };
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id);

        // Assert
        result!.ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTemplateWithItemsAsync_WhenExists_ReturnsDtoWithItems()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var items = new List<ChecklistTemplateItem>
        {
            new ChecklistTemplateItem(template.Id, 0, "Question 1", ChecklistItemType.Question, true, "Help text", 15)
        };
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var result = await _service.GetTemplateWithItemsAsync(template.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Content.Should().Be("Question 1");
        result.Items[0].ItemType.Should().Be(ChecklistItemType.Question);
        result.Items[0].ItemTypeName.Should().Be("Question");
        result.Items[0].IsRequired.Should().BeTrue();
        result.Items[0].HelpText.Should().Be("Help text");
        result.Items[0].EstimatedMinutes.Should().Be(15);
    }

    [Fact]
    public async Task GetTemplateWithItemsAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplate?)null);

        // Act
        var result = await _service.GetTemplateWithItemsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllTemplatesAsync_ReturnsMappedList()
    {
        // Arrange
        var templates = new List<ChecklistTemplate>
        {
            new ChecklistTemplate("Template A", "Desc A", ChecklistType.Interview),
            new ChecklistTemplate("Template B", "Desc B", ChecklistType.Onboarding)
        };
        _templateRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem>());

        // Act
        var result = await _service.GetAllTemplatesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.Name).Should().Contain(["Template A", "Template B"]);
    }

    [Fact]
    public async Task GetTemplatesByTypeAsync_CallsRepositoryWithCorrectType()
    {
        // Arrange
        var templates = new List<ChecklistTemplate>
        {
            new ChecklistTemplate("Interview T", "Desc", ChecklistType.Interview)
        };
        _templateRepositoryMock.Setup(r => r.GetByTypeAsync(ChecklistType.Interview, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem>());

        // Act
        var result = await _service.GetTemplatesByTypeAsync(ChecklistType.Interview);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ChecklistType.Interview);
    }

    [Fact]
    public async Task CreateTemplateAsync_WhenNameIsUnique_CreatesAndLogsActivity()
    {
        // Arrange
        var dto = new CreateChecklistTemplateDto
        {
            Name = "New Template",
            Description = "A new template",
            Type = ChecklistType.Interview
        };

        _templateRepositoryMock.Setup(r => r.NameExistsAsync(dto.Name, dto.Type, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _templateRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ChecklistTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplate t, CancellationToken _) => t);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem>());

        // Act
        var result = await _service.CreateTemplateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Template");
        result.Type.Should().Be(ChecklistType.Interview);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Created,
            EntityType.ChecklistTemplate,
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTemplateAsync_WhenNameAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateChecklistTemplateDto
        {
            Name = "Existing Template",
            Description = "Desc",
            Type = ChecklistType.Interview
        };
        _templateRepositoryMock.Setup(r => r.NameExistsAsync(dto.Name, dto.Type, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.CreateTemplateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateTemplateAsync_WhenExists_UpdatesAndLogsActivity()
    {
        // Arrange
        var template = new ChecklistTemplate("Old Name", "Old Desc", ChecklistType.Interview);
        var dto = new UpdateChecklistTemplateDto { Name = "New Name", Description = "New Desc" };

        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templateRepositoryMock.Setup(r => r.NameExistsAsync("New Name", ChecklistType.Interview, template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem>());

        // Act
        var result = await _service.UpdateTemplateAsync(template.Id, dto);

        // Assert
        result.Name.Should().Be("New Name");
        _templateRepositoryMock.Verify(r => r.UpdateAsync(template, It.IsAny<CancellationToken>()), Times.Once);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Updated,
            EntityType.ChecklistTemplate,
            template.Id,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplate?)null);

        // Act
        var act = async () => await _service.UpdateTemplateAsync(Guid.NewGuid(),
            new UpdateChecklistTemplateDto { Name = "Name", Description = "Desc" });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteTemplateAsync_WhenExistsWithNoInstances_DeletesAndLogsActivity()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _instanceRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstance>());

        // Act
        await _service.DeleteTemplateAsync(template.Id);

        // Assert
        _templateItemRepositoryMock.Verify(r => r.DeleteByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()), Times.Once);
        _templateRepositoryMock.Verify(r => r.DeleteAsync(template.Id, It.IsAny<CancellationToken>()), Times.Once);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Deleted, EntityType.ChecklistTemplate, template.Id,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTemplateAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplate?)null);

        // Act
        var act = async () => await _service.DeleteTemplateAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteTemplateAsync_WhenHasInstances_ThrowsInvalidOperationException()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var instance = ChecklistInstance.CreateInterview(template.Id, "Interview", "Candidate", "Dev", DateTime.UtcNow);

        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _instanceRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstance> { instance });

        // Act
        var act = async () => await _service.DeleteTemplateAsync(template.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*instance(s)*");
    }

    [Fact]
    public async Task ActivateTemplateAsync_WhenExists_ActivatesTemplate()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        template.Deactivate();
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem>());

        // Act
        var result = await _service.ActivateTemplateAsync(template.Id);

        // Assert
        result.IsActive.Should().BeTrue();
        _templateRepositoryMock.Verify(r => r.UpdateAsync(template, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateTemplateAsync_WhenExists_DeactivatesTemplate()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem>());

        // Act
        var result = await _service.DeactivateTemplateAsync(template.Id);

        // Assert
        result.IsActive.Should().BeFalse();
        _templateRepositoryMock.Verify(r => r.UpdateAsync(template, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============== Template Item Tests ==============

    [Fact]
    public async Task AddTemplateItemAsync_WhenTemplateExists_CreatesItem()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var dto = new CreateChecklistTemplateItemDto
        {
            Content = "Tell me about yourself",
            ItemType = ChecklistItemType.Question,
            IsRequired = true,
            HelpText = "Look for communication skills",
            EstimatedMinutes = 5
        };

        _templateRepositoryMock.Setup(r => r.ExistsAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _templateItemRepositoryMock.Setup(r => r.GetNextSortOrderAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _templateItemRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ChecklistTemplateItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplateItem item, CancellationToken _) => item);

        // Act
        var result = await _service.AddTemplateItemAsync(templateId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Tell me about yourself");
        result.ItemType.Should().Be(ChecklistItemType.Question);
        result.ItemTypeName.Should().Be("Question");
        result.IsRequired.Should().BeTrue();
        result.HelpText.Should().Be("Look for communication skills");
        result.EstimatedMinutes.Should().Be(5);
        result.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task AddTemplateItemAsync_WhenTemplateNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _templateRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _service.AddTemplateItemAsync(Guid.NewGuid(),
            new CreateChecklistTemplateItemDto { Content = "Item", ItemType = ChecklistItemType.Task });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateTemplateItemAsync_WhenExists_UpdatesItem()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var item = new ChecklistTemplateItem(templateId, 0, "Original Content", ChecklistItemType.Question);
        var dto = new UpdateChecklistTemplateItemDto
        {
            SortOrder = 1,
            Content = "Updated Content",
            ItemType = ChecklistItemType.Topic,
            IsRequired = false
        };
        _templateItemRepositoryMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var result = await _service.UpdateTemplateItemAsync(item.Id, dto);

        // Assert
        result.Content.Should().Be("Updated Content");
        result.ItemType.Should().Be(ChecklistItemType.Topic);
        result.SortOrder.Should().Be(1);
        _templateItemRepositoryMock.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTemplateItemAsync_WhenExists_DeletesItem()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        _templateItemRepositoryMock.Setup(r => r.ExistsAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteTemplateItemAsync(itemId);

        // Assert
        _templateItemRepositoryMock.Verify(r => r.DeleteAsync(itemId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTemplateItemAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _templateItemRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _service.DeleteTemplateItemAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ReorderTemplateItemsAsync_WhenTemplateExists_UpdatesSortOrders()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var item1 = new ChecklistTemplateItem(templateId, 0, "Item 1", ChecklistItemType.Task);
        var item2 = new ChecklistTemplateItem(templateId, 1, "Item 2", ChecklistItemType.Task);

        _templateRepositoryMock.Setup(r => r.ExistsAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem> { item1, item2 });

        // Reorder: item2 first, then item1
        var newOrder = new List<Guid> { item2.Id, item1.Id };

        // Act
        await _service.ReorderTemplateItemsAsync(templateId, newOrder);

        // Assert
        _templateItemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ChecklistTemplateItem>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        item2.SortOrder.Should().Be(0);
        item1.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task ReorderTemplateItemsAsync_WhenTemplateNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _templateRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _service.ReorderTemplateItemsAsync(Guid.NewGuid(), new List<Guid>());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ============== Instance Tests ==============

    [Fact]
    public async Task GetInstanceByIdAsync_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var instance = ChecklistInstance.CreateInterview(template.Id, "Frontend Interview",
            "John Smith", "Senior Dev", DateTime.UtcNow.AddDays(3));

        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _instanceItemRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem>());

        // Act
        var result = await _service.GetInstanceByIdAsync(instance.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Frontend Interview");
        result.CandidateName.Should().Be("John Smith");
        result.Position.Should().Be("Senior Dev");
        result.Type.Should().Be(ChecklistType.Interview);
        result.TypeName.Should().Be("Interview");
        result.Status.Should().Be(ChecklistInstanceStatus.NotStarted);
        result.StatusName.Should().Be("Not Started");
        result.TotalItems.Should().Be(0);
        result.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public async Task GetInstanceByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistInstance?)null);

        // Act
        var result = await _service.GetInstanceByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInstanceWithItemsAsync_CalculatesProgressCorrectly()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Onboarding);
        var instance = ChecklistInstance.CreateOnboarding(template.Id, "New Hire Onboarding",
            "Alice", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        var instanceId = instance.Id;
        var item1 = new ChecklistInstanceItem(instanceId, Guid.NewGuid(), 0, "Set up laptop", ChecklistItemType.Task, true);
        var item2 = new ChecklistInstanceItem(instanceId, Guid.NewGuid(), 1, "Meet the team", ChecklistItemType.Task, true);
        var item3 = new ChecklistInstanceItem(instanceId, Guid.NewGuid(), 2, "Review handbook", ChecklistItemType.Document, false);

        // Complete 2 of 3 items
        item1.MarkComplete("Done");
        item2.MarkSkipped(); // only if not required - but item2 IS required, so we'll use MarkComplete instead
        // Actually let's mark item2 complete and item3 skipped:
        item2.MarkComplete();

        // item3 is optional (IsRequired=false), mark as skipped
        var item3Optional = new ChecklistInstanceItem(instanceId, Guid.NewGuid(), 2, "Optional reading", ChecklistItemType.Document, false);
        item3Optional.MarkSkipped();

        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _instanceItemRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem> { item1, item2, item3Optional });

        // Act
        var result = await _service.GetInstanceWithItemsAsync(instance.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TotalItems.Should().Be(3);
        result.CompletedItems.Should().Be(3); // item1 completed, item2 completed, item3Optional skipped
        result.ProgressPercent.Should().Be(100);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetInstanceWithItemsAsync_WhenTemplateDeleted_UsesUnknownTemplateName()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var instance = ChecklistInstance.CreateInterview(template.Id, "Interview", "Cand", "Dev", DateTime.UtcNow);

        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplate?)null);
        _instanceItemRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem>());

        // Act
        var result = await _service.GetInstanceWithItemsAsync(instance.Id);

        // Assert
        result!.TemplateName.Should().Be("Unknown Template");
    }

    [Fact]
    public async Task CreateInterviewInstanceAsync_WithInterviewTemplate_CreatesInstanceAndCopiesItems()
    {
        // Arrange
        var template = new ChecklistTemplate("Interview Template", "Desc", ChecklistType.Interview);
        var templateItem = new ChecklistTemplateItem(template.Id, 0, "Intro question", ChecklistItemType.Question);

        var dto = new CreateInterviewInstanceDto
        {
            TemplateId = template.Id,
            Title = "Senior Dev Interview",
            CandidateName = "Jane Doe",
            Position = "Senior Engineer",
            InterviewDate = DateTime.UtcNow.AddDays(5)
        };

        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem> { templateItem });
        _instanceRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ChecklistInstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistInstance inst, CancellationToken _) => inst);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _instanceItemRepositoryMock.Setup(r => r.GetByInstanceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem>());

        // Act
        var result = await _service.CreateInterviewInstanceAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Senior Dev Interview");
        result.CandidateName.Should().Be("Jane Doe");
        result.Type.Should().Be(ChecklistType.Interview);
        _instanceItemRepositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ChecklistInstanceItem>>(), It.IsAny<CancellationToken>()), Times.Once);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Created, EntityType.ChecklistInstance,
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInterviewInstanceAsync_WithOnboardingTemplate_ThrowsInvalidOperationException()
    {
        // Arrange
        var template = new ChecklistTemplate("Onboarding Template", "Desc", ChecklistType.Onboarding);
        var dto = new CreateInterviewInstanceDto
        {
            TemplateId = template.Id,
            Title = "Interview",
            CandidateName = "Jane",
            Position = "Dev",
            InterviewDate = DateTime.UtcNow
        };

        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        var act = async () => await _service.CreateInterviewInstanceAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not an Interview type*");
    }

    [Fact]
    public async Task CreateInterviewInstanceAsync_WhenTemplateNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplate?)null);

        // Act
        var act = async () => await _service.CreateInterviewInstanceAsync(
            new CreateInterviewInstanceDto { TemplateId = Guid.NewGuid(), Title = "T", CandidateName = "C", Position = "P", InterviewDate = DateTime.UtcNow });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateOnboardingInstanceAsync_WithOnboardingTemplate_CreatesInstanceAndCopiesItems()
    {
        // Arrange
        var template = new ChecklistTemplate("Onboarding Template", "Desc", ChecklistType.Onboarding);
        var dto = new CreateOnboardingInstanceDto
        {
            TemplateId = template.Id,
            Title = "Alice Onboarding",
            NewHireName = "Alice Johnson",
            StartDate = DateTime.UtcNow.AddDays(7),
            TargetCompletionDate = DateTime.UtcNow.AddDays(37)
        };

        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem>());
        _instanceRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ChecklistInstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistInstance inst, CancellationToken _) => inst);
        _instanceItemRepositoryMock.Setup(r => r.GetByInstanceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem>());

        // Act
        var result = await _service.CreateOnboardingInstanceAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Alice Onboarding");
        result.NewHireName.Should().Be("Alice Johnson");
        result.Type.Should().Be(ChecklistType.Onboarding);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Created, EntityType.ChecklistInstance,
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOnboardingInstanceAsync_WithInterviewTemplate_ThrowsInvalidOperationException()
    {
        // Arrange
        var template = new ChecklistTemplate("Interview Template", "Desc", ChecklistType.Interview);
        var dto = new CreateOnboardingInstanceDto
        {
            TemplateId = template.Id,
            Title = "Onboarding",
            NewHireName = "Bob",
            StartDate = DateTime.UtcNow
        };
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        var act = async () => await _service.CreateOnboardingInstanceAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not an Onboarding type*");
    }

    [Fact]
    public async Task StartInstanceAsync_WhenNotStarted_StartsInstance()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var instance = ChecklistInstance.CreateInterview(template.Id, "Interview", "Cand", "Dev", DateTime.UtcNow);

        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _instanceItemRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem>());

        // Act
        var result = await _service.StartInstanceAsync(instance.Id);

        // Assert
        result.Status.Should().Be(ChecklistInstanceStatus.InProgress);
        _instanceRepositoryMock.Verify(r => r.UpdateAsync(instance, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteInstanceAsync_WhenInProgress_CompletesAndLogsActivity()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var instance = ChecklistInstance.CreateInterview(template.Id, "Interview", "Cand", "Dev", DateTime.UtcNow);
        instance.Start();

        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _instanceItemRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem>());

        // Act
        var result = await _service.CompleteInstanceAsync(instance.Id);

        // Assert
        result.Status.Should().Be(ChecklistInstanceStatus.Completed);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Completed, EntityType.ChecklistInstance,
            instance.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelInstanceAsync_WhenInProgress_CancelsInstance()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var instance = ChecklistInstance.CreateInterview(template.Id, "Interview", "Cand", "Dev", DateTime.UtcNow);
        instance.Start();

        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _instanceItemRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem>());

        // Act
        var result = await _service.CancelInstanceAsync(instance.Id);

        // Assert
        result.Status.Should().Be(ChecklistInstanceStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateInstanceNotesAsync_WhenExists_UpdatesNotes()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var instance = ChecklistInstance.CreateInterview(template.Id, "Interview", "Cand", "Dev", DateTime.UtcNow);

        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _instanceItemRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem>());

        // Act
        var result = await _service.UpdateInstanceNotesAsync(instance.Id, "Good candidate overall.");

        // Assert
        result.Notes.Should().Be("Good candidate overall.");
        _instanceRepositoryMock.Verify(r => r.UpdateAsync(instance, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteInstanceAsync_WhenExists_DeletesItemsAndInstanceAndLogsActivity()
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var instance = ChecklistInstance.CreateInterview(template.Id, "Interview", "Cand", "Dev", DateTime.UtcNow);

        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instance.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        // Act
        await _service.DeleteInstanceAsync(instance.Id);

        // Assert
        _instanceItemRepositoryMock.Verify(r => r.DeleteByInstanceIdAsync(instance.Id, It.IsAny<CancellationToken>()), Times.Once);
        _instanceRepositoryMock.Verify(r => r.DeleteAsync(instance.Id, It.IsAny<CancellationToken>()), Times.Once);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Deleted, EntityType.ChecklistInstance,
            instance.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteInstanceAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistInstance?)null);

        // Act
        var act = async () => await _service.DeleteInstanceAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ============== Instance Item Tests ==============

    [Fact]
    public async Task CompleteItemAsync_WhenExists_MarksCompleteAndReturnsDto()
    {
        // Arrange
        var instanceId = Guid.NewGuid();
        var item = new ChecklistInstanceItem(instanceId, Guid.NewGuid(), 0, "Review docs", ChecklistItemType.Document, true);
        var dto = new CompleteChecklistItemDto { Notes = "Reviewed thoroughly", Score = 4 };

        _instanceItemRepositoryMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Auto-start: instance is already InProgress
        var template = new ChecklistTemplate("T", "D", ChecklistType.Onboarding);
        var instance = ChecklistInstance.CreateOnboarding(template.Id, "Onboarding", "Alice", DateTime.UtcNow);
        instance.Start();
        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        // Act
        var result = await _service.CompleteItemAsync(item.Id, dto);

        // Assert
        result.Status.Should().Be(ChecklistItemStatus.Completed);
        result.Notes.Should().Be("Reviewed thoroughly");
        result.Score.Should().Be(4);
        _instanceItemRepositoryMock.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteItemAsync_AutoStartsNotStartedInstance()
    {
        // Arrange
        var instanceId = Guid.NewGuid();
        var item = new ChecklistInstanceItem(instanceId, Guid.NewGuid(), 0, "Task", ChecklistItemType.Task, true);
        var dto = new CompleteChecklistItemDto();

        _instanceItemRepositoryMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var template = new ChecklistTemplate("T", "D", ChecklistType.Onboarding);
        var instance = ChecklistInstance.CreateOnboarding(template.Id, "Onboarding", "Bob", DateTime.UtcNow);
        // Instance is NotStarted

        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        // Act
        await _service.CompleteItemAsync(item.Id, dto);

        // Assert - instance should be auto-started
        _instanceRepositoryMock.Verify(r => r.UpdateAsync(instance, It.IsAny<CancellationToken>()), Times.Once);
        instance.Status.Should().Be(ChecklistInstanceStatus.InProgress);
    }

    [Fact]
    public async Task SkipItemAsync_WhenItemIsNotRequired_SkipsItem()
    {
        // Arrange
        var instanceId = Guid.NewGuid();
        var item = new ChecklistInstanceItem(instanceId, Guid.NewGuid(), 0, "Optional task", ChecklistItemType.Task, false);

        _instanceItemRepositoryMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var result = await _service.SkipItemAsync(item.Id, "Not applicable");

        // Assert
        result.Status.Should().Be(ChecklistItemStatus.Skipped);
        result.StatusName.Should().Be("Skipped");
    }

    [Fact]
    public async Task UpdateItemAsync_UpdatesNotesScoreAndAssignee()
    {
        // Arrange
        var instanceId = Guid.NewGuid();
        var item = new ChecklistInstanceItem(instanceId, Guid.NewGuid(), 0, "Task", ChecklistItemType.Task, true);
        var dueDate = DateTime.UtcNow.AddDays(7);
        var dto = new UpdateChecklistItemDto
        {
            Notes = "Updated notes",
            Score = 3,
            Assignee = "Bob Smith",
            DueDate = dueDate
        };

        _instanceItemRepositoryMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var result = await _service.UpdateItemAsync(item.Id, dto);

        // Assert
        result.Notes.Should().Be("Updated notes");
        result.Score.Should().Be(3);
        result.Assignee.Should().Be("Bob Smith");
        result.DueDate.Should().Be(dueDate);
        _instanceItemRepositoryMock.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOverdueItemsAsync_ReturnsMappedOverdueItems()
    {
        // Arrange
        var instanceId = Guid.NewGuid();
        var item = new ChecklistInstanceItem(instanceId, Guid.NewGuid(), 0, "Overdue task", ChecklistItemType.Task, true);
        item.SetAssignee("Alice", DateTime.UtcNow.AddDays(-1)); // Due yesterday

        _instanceItemRepositoryMock.Setup(r => r.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceItem> { item });

        // Act
        var result = await _service.GetOverdueItemsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Overdue task");
        result[0].IsOverdue.Should().BeTrue();
    }

    // ============== Type Name Mapping Tests ==============

    [Theory]
    [InlineData(ChecklistType.Interview, "Interview")]
    [InlineData(ChecklistType.Onboarding, "Onboarding")]
    public async Task GetTemplateByIdAsync_MapsTypeNameCorrectly(ChecklistType type, string expectedTypeName)
    {
        // Arrange
        var template = new ChecklistTemplate("Template", "Desc", type);
        _templateRepositoryMock.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templateItemRepositoryMock.Setup(r => r.GetByTemplateIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateItem>());

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id);

        // Assert
        result!.TypeName.Should().Be(expectedTypeName);
    }

    [Theory]
    [InlineData(ChecklistItemType.Question, "Question")]
    [InlineData(ChecklistItemType.Topic, "Topic")]
    [InlineData(ChecklistItemType.Task, "Task")]
    [InlineData(ChecklistItemType.Document, "Document")]
    [InlineData(ChecklistItemType.Training, "Training")]
    public async Task AddTemplateItemAsync_MapsItemTypeNameCorrectly(ChecklistItemType itemType, string expectedTypeName)
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var dto = new CreateChecklistTemplateItemDto { Content = "Item", ItemType = itemType };

        _templateRepositoryMock.Setup(r => r.ExistsAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _templateItemRepositoryMock.Setup(r => r.GetNextSortOrderAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _templateItemRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ChecklistTemplateItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplateItem i, CancellationToken _) => i);

        // Act
        var result = await _service.AddTemplateItemAsync(templateId, dto);

        // Assert
        result.ItemTypeName.Should().Be(expectedTypeName);
    }
}
