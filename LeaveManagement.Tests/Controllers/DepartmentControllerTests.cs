// LeaveManagement.Tests/Controllers/DepartmentsControllerTests.cs
using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;

public class DepartmentsControllerTests
{
    private readonly Mock<IDepartmentService> _mockService;
    private readonly DepartmentsController _controller;

    public DepartmentsControllerTests()
    {
        _mockService = new Mock<IDepartmentService>();
        _controller = new DepartmentsController(_mockService.Object);
    }

    // ── GetAll ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        // Arrange
        var departments = new List<DepartmentResponseDto>
        {
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Engineering" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "HR" }
        };
        _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(departments);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(departments);
    }

    // ── GetById ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var departmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var dept = new DepartmentResponseDto { Id = departmentId, Name = "Engineering" };
        _mockService.Setup(s => s.GetByIdAsync(departmentId)).ReturnsAsync(dept);

        // Act
        var result = await _controller.GetById(departmentId);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(dept);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        // Arrange
        var missingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        _mockService.Setup(s => s.GetByIdAsync(missingId))
                    .ThrowsAsync(new NotFoundException($"Department {missingId} not found."));

        // Act
        var result = await _controller.GetById(missingId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Create ────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidDto_Returns201Created()
    {
        // Arrange
        var dto = new CreateDepartmentDto { Name = "Legal" };
        var response = new DepartmentResponseDto { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Legal" };
        _mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(response);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Create_WhenNameConflicts_Returns409()
    {
        // Arrange
        var dto = new CreateDepartmentDto { Name = "Engineering" };
        _mockService.Setup(s => s.CreateAsync(dto))
                    .ThrowsAsync(new ConflictException("Department 'Engineering' already exists."));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── Delete ────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenExists_Returns204NoContent()
    {
        // Arrange
        var departmentId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        _mockService.Setup(s => s.DeleteAsync(departmentId)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(departmentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenHasActiveUsers_Returns409()
    {
        // Arrange
        var departmentId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        _mockService.Setup(s => s.DeleteAsync(departmentId))
                    .ThrowsAsync(new ConflictException("Cannot delete a department with active users."));

        // Act
        var result = await _controller.Delete(departmentId);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }
}