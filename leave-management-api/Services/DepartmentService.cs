
using Microsoft.EntityFrameworkCore;

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _db;

    public DepartmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DepartmentResponseDto>> GetAllAsync()
    {
        return await _db.Departments
        .OrderBy(d => d.Name)
        .Select(d => new DepartmentResponseDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            UserCount = d.Users.Count(u => u.IsActive),
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        })
        .ToListAsync();
    }

    public async Task<DepartmentResponseDto> GetByIdAsync(Guid id)
    {
        var dept = await _db.Departments
        .Include(d => d.Users.Where(u => u.IsActive))
        .FirstOrDefaultAsync(d => d.Id == id)
        ?? throw new NotFoundException($"Department {id} not found.");

        return MapToDto(dept);
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto)
    {
        var nameTaken = await _db.Departments.AnyAsync(d => d.Name.ToLower() == dto.Name.ToLower());

        if (nameTaken)
            throw new BadRequestException($"Department name '{dto.Name}' is already taken.");

        var dept = new Department
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();

        return MapToDto(dept);
    }

    public async Task<DepartmentResponseDto?> UpdateAsync(Guid id, UpdateDepartmentDto dto)
    {
        var dept = await _db.Departments.FindAsync(id) ?? throw new NotFoundException($"Department {id} not found.");

        if (dto.Name is not null && dto.Name.Trim().ToLower() != dept.Name.ToLower())
        {
            var nameTaken = await _db.Departments.AnyAsync(d => d.Id != id && d.Name.ToLower() == dto.Name.Trim().ToLower());
            if (nameTaken)
                throw new BadRequestException($"Department name '{dto.Name}' is already taken.");
            dept.Name = dto.Name.Trim();
        }
        if (dto.Description is not null)
        {
            dept.Description = dto.Description.Trim();
        }
        dept.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToDto(dept);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var dept = await _db.Departments.FindAsync(id) ?? throw new NotFoundException($"Department {id} not found.");

        if (dept.Users != null && dept.Users.Any(u => u.IsActive))
            throw new BadRequestException("Cannot delete department with active users.");

        _db.Departments.Remove(dept);
        await _db.SaveChangesAsync();
        return true;
    }
    private static DepartmentResponseDto MapToDto(Department d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Description = d.Description,
        UserCount = d.Users?.Count(u => u.IsActive) ?? 0,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };
}