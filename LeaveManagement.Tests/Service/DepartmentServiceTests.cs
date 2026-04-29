using FluentAssertions;

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

    //For GetAllAsync function or getall

    [Fact]
    public async Task GetAllAsync_WhenNoDepartments_ReturnsEmptyList()
    {
        var db = DbContextFactory.Create();
        var service = new DepartmentService(db);

        var result = await service.GetAllAsync();
        result.Should().BeEmpty();
    }
}