public class DepartmentResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }  //computed field
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}