public interface IDepartmentService
{
    Task<List<DepartmentResponseDto>> GetAllAsync();
    Task<DepartmentResponseDto> GetByIdAsync(Guid id);
    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto);
    Task<DepartmentResponseDto?> UpdateAsync(Guid id, UpdateDepartmentDto dto);
    Task<bool> DeleteAsync(Guid id);
}