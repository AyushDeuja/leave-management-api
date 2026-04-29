using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class DepartmentServiceTests
{
    //seeding department into the db
    private async Task<Department> SeedDepartment(AppDbContext db, string name = "Engineering", string desc = "Builds things")
    {
        var dept = new Department
        {
            Name = name,
            Description = desc,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();
        return dept;
    }

    //For GetAllAsync function or getAll

    [Fact]
    public async Task GetAllAsync_WhenNoDepartments_ReturnsEmptyList()
    {
        var db = DbContextFactory.Create();
        var service = new DepartmentService(db);

        var result = await service.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDepartmentsOrderedByName()
    {
        var db = DbContextFactory.Create();
        await SeedDepartment(db, "Marketing");
        await SeedDepartment(db, "Engineering");
        await SeedDepartment(db, "HR");
        var service = new DepartmentService(db);

        var result = await service.GetAllAsync();

        result.Should().HaveCount(3);
        result.Select(d => d.Name).Should().ContainInOrder("Engineering", "HR", "Marketing");
    }

    [Fact]
    public async Task GetTaskAsync_MapsAllFieldsCorrectly()
    {
        // Arrange
        var db = DbContextFactory.Create();
        var dept = await SeedDepartment(db, "Finance", "Handles money");
        var service = new DepartmentService(db);

        // Act
        var result = await service.GetAllAsync();
        var dto = result.Single();

        //Assert
        dto.Id.Should().Be(dept.Id);
        dto.Name.Should().Be(dept.Name);
        dto.Description.Should().Be(dept.Description);
    }

    //For GetByIdAsync function or getById
    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDepartment()
    {
        //Arrange
        var db = DbContextFactory.Create();
        var dept = await SeedDepartment(db);
        var service = new DepartmentService(db);

        //Act
        var result = await service.GetByIdAsync(dept.Id);

        //Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(dept.Id);
        result.Name.Should().Be("Engineering");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
    {
        var db = DbContextFactory.Create();
        var service = new DepartmentService(db);

        var act = async () => await service.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*{Guid.NewGuid()}*");
    }

    // for CreateAsync function or create

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsDepartmentWithId()
    {
        // Arrange
        var db = DbContextFactory.Create();
        var service = new DepartmentService(db);
        var dto = new CreateDepartmentDto { Name = "Legal", Description = "Legal team" };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.Name.Should().Be("Legal");
        result.Description.Should().Be("Legal team");
    }

    [Fact]
    public async Task CreateAsync_PersistsToDatabasee()
    {
        // Arrange
        var db = DbContextFactory.Create();
        var service = new DepartmentService(db);
        var dto = new CreateDepartmentDto { Name = "Design" };

        // Act
        await service.CreateAsync(dto);

        // Assert — query the DB directly to confirm it was saved
        var saved = await db.Departments.SingleAsync(d => d.Name == "Design");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var db = DbContextFactory.Create();
        await SeedDepartment(db, "Engineering");
        var service = new DepartmentService(db);
        var dto = new CreateDepartmentDto { Name = "Engineering" };

        // Act
        var act = async () => await service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Engineering*");
    }

    [Fact]
    public async Task CreateAsync_NameIsCaseInsensitiveForDuplicateCheck()
    {
        // Arrange — seed "engineering" lowercase
        var db = DbContextFactory.Create();
        await SeedDepartment(db, "engineering");
        var service = new DepartmentService(db);

        // Try creating "ENGINEERING" uppercase
        var dto = new CreateDepartmentDto { Name = "ENGINEERING" };

        // Act
        var act = async () => await service.CreateAsync(dto);

        // Assert — should still be a conflict
        await act.Should().ThrowAsync<ConflictException>();
    }
}