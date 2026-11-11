using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hive.Tests.Api.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _serviceMock = new();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_serviceMock.Object, Mock.Of<ILogger<ProductsController>>());
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithProducts()
    {
        // Arrange
        var products = new List<ProductDto> { new() { Name = "Matching" } };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(products);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        // Arrange
        var created = new ProductDto { Id = Guid.NewGuid(), Name = "Internal tooling" };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateProductDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        var result = await _controller.Create(new CreateProductDto { Name = "Internal tooling" }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().Be(created);
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateProductDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("A product with name 'Matching' already exists."));

        // Act
        var result = await _controller.Create(new CreateProductDto { Name = "Matching" }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateProductDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Product name cannot be empty."));

        // Act
        var result = await _controller.Create(new CreateProductDto(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_ProductFromJira_ReturnsBadRequest()
    {
        // Arrange
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateProductDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Product 'Matching' comes from Jira."));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateProductDto { Name = "Renamed" }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateProductDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Product", id));

        // Act
        var result = await _controller.Update(id, new UpdateProductDto { Name = "Renamed" }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ProductFromJira_ReturnsBadRequest()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Product 'Matching' comes from Jira."));

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
