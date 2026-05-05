using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ILeaveBalanceService _leaveBalanceService;

    public UserService(AppDbContext db, ILeaveBalanceService leaveBalanceService)
    {
        _db = db;
        _leaveBalanceService = leaveBalanceService;
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto, UserRole creatorRole)
    {
        var allowed = creatorRole switch
        {
            UserRole.ADMIN => new[] { UserRole.ADMIN, UserRole.MANAGER, UserRole.EMPLOYEE },
            UserRole.MANAGER => new[] { UserRole.EMPLOYEE },
            _ => Array.Empty<UserRole>()
        };

        if (!allowed.Contains(dto.Role))
            throw new ForbiddenException("You do not have permission to create this type of user.");

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailTaken) throw new ConflictException($"Email '{dto.Email}' is already registered.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash),
            Role = dto.Role,
            DepartmentId = dto.DepartmentId,
            ManagerId = dto.ManagerId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (user.IsActive)
        {
            await _leaveBalanceService.AssignBalancesForUserAsync(user.Id, DateTime.UtcNow.Year);
        }

        return MapToDto(user);
    }

    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        var users = await _db.Users.Include(u => u.Department).Where(u => u.IsActive).ToListAsync();
        return users.Select(u => MapToDto(u)).ToList();
    }

    public async Task<UserResponseDto> GetByIdAsync(Guid id)
    {
        var user = await _db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException($"User {id} not found.");
        return MapToDto(user);
    }

    public async Task<UserResponseDto> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id)
                   ?? throw new NotFoundException($"User {id} not found.");

        if (dto.FullName is not null) user.FullName = dto.FullName;

        if (dto.Email is not null)
        {
            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
            if (exists) throw new ConflictException("Email already exists");

            user.Email = dto.Email.ToLower().Trim();
        }

        if (dto.PasswordHash is not null)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash);
        }

        if (dto.Role is not null) user.Role = dto.Role.Value;
        if (dto.DepartmentId is not null) user.DepartmentId = dto.DepartmentId;
        if (dto.ManagerId is not null) user.ManagerId = dto.ManagerId;
        if (dto.IsActive is not null) user.IsActive = dto.IsActive.Value;

        if (user.Department == null && user.DepartmentId.HasValue)
            await _db.Entry(user).Reference(u => u.Department).LoadAsync();

        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id) ?? throw new NotFoundException($"User {id} not found.");
        user.IsActive = false; //soft delete user
        await _db.SaveChangesAsync();
    }
    private static UserResponseDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        DepartmentId = user.DepartmentId,
        ManagerId = user.ManagerId,
        DepartmentName = user.Department?.Name,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}