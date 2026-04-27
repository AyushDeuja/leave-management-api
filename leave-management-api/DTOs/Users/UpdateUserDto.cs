public class UpdateUserDto
{
    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public UserRole? Role { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }


    public bool? IsActive { get; set; }
}