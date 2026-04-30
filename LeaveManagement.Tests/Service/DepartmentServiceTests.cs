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

    // for UpdateAsync function or update

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesName()
    {
        // Arrange
        var db = DbContextFactory.Create();
        var dept = await SeedDepartment(db, "Old Name");
        var service = new DepartmentService(db);
        var dto = new UpdateDepartmentDto { Name = "New Name" };

        // Act
        var result = await service.UpdateAsync(dept.Id, dto);

        // Assert
        result.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_WhenExists_PersistsChangesToDatabase()
    {
        // Arrange
        var db = DbContextFactory.Create();
        var dept = await SeedDepartment(db, "Old Name");
        var service = new DepartmentService(db);
        var dto = new UpdateDepartmentDto { Name = "New Name" };

        // Act
        await service.UpdateAsync(dept.Id, dto);

        // Assert — fresh query to confirm DB was actually updated
        var updated = await db.Departments.FindAsync(dept.Id);
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_NullFields_DoesNotOverwriteExistingValues()
    {
        // Arrange — existing dept with name + description
        var db = DbContextFactory.Create();
        var dept = await SeedDepartment(db, "Engineering", "Builds software");
        var service = new DepartmentService(db);

        // Send update with only description changed, name is null (not provided)
        var dto = new UpdateDepartmentDto { Description = "Updated desc" };

        // Act
        var result = await service.UpdateAsync(dept.Id, dto);

        // Assert — name unchanged, description updated
        result.Name.Should().Be("Engineering");
        result.Description.Should().Be("Updated desc");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var db = DbContextFactory.Create();
        var service = new DepartmentService(db);
        var dto = new UpdateDepartmentDto { Name = "Anything" };

        // Act
        var act = async () => await service.UpdateAsync(new Guid(), dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenNewNameConflicts_ThrowsConflictException()
    {
        // Arrange — two departments exist
        var db = DbContextFactory.Create();
        await SeedDepartment(db, "Engineering");
        var dept2 = await SeedDepartment(db, "Marketing");
        var service = new DepartmentService(db);

        // Try renaming Marketing → Engineering (already taken)
        var dto = new UpdateDepartmentDto { Name = "Engineering" };

        // Act
        var act = async () => await service.UpdateAsync(dept2.Id, dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_SameNameForSameDepartment_DoesNotThrow()
    {
        // Arrange — updating a department with its own existing name should be fine
        var db = DbContextFactory.Create();
        var dept = await SeedDepartment(db, "Engineering");
        var service = new DepartmentService(db);
        var dto = new UpdateDepartmentDto { Name = "Engineering" };

        // Act — should NOT throw even though name "exists"
        var act = async () => await service.UpdateAsync(dept.Id, dto);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ════════════════════════════════════════════════════════════════════
    // DeleteAsync
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_WhenExists_RemovesFromDatabase()
    {
        // Arrange
        var db = DbContextFactory.Create();
        var dept = await SeedDepartment(db);
        var service = new DepartmentService(db);

        // Act
        await service.DeleteAsync(dept.Id);

        // Assert — should be gone
        var deleted = await db.Departments.FindAsync(dept.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var db = DbContextFactory.Create();
        var service = new DepartmentService(db);

        // Act
        var act = async () => await service.DeleteAsync(new Guid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenDepartmentHasActiveUsers_ThrowsConflictException()
    {
        // Arrange — department with one active user assigned
        var db = DbContextFactory.Create();
        var dept = await SeedDepartment(db);

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Alice",
            Email = "alice@test.com",
            PasswordHash = "hash",
            Role = UserRole.EMPLOYEE,
            DepartmentId = dept.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        // Act
        var act = async () => await service.DeleteAsync(dept.Id);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*active users*");
    }

    [Fact]
    public async Task DeleteAsync_WhenDepartmentHasOnlyInactiveUsers_Succeeds()
    {
        // Arrange — department with one INACTIVE user — should allow deletion
        var db = DbContextFactory.Create();
        var dept = await SeedDepartment(db);

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Bob",
            Email = "bob@test.com",
            PasswordHash = "hash",
            Role = UserRole.EMPLOYEE,
            DepartmentId = dept.Id,
            IsActive = false,   // ← inactive
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        // Act
        var act = async () => await service.DeleteAsync(dept.Id);

        // Assert — inactive users should not block deletion
        await act.Should().NotThrowAsync();
    }
}