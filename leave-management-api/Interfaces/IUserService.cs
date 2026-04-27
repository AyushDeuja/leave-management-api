public interface IUserService
{
    Task<UserResponseDto> CreateAsync(CreateUserDto dto, UserRole creatorRole);
    Task<UserResponseDto> GetByIdAsync(Guid id);
    Task<List<UserResponseDto>> GetAllAsync();
    Task<UserResponseDto> UpdateAsync(Guid id, UpdateUserDto dto);
    Task DeleteAsync(Guid id);
}