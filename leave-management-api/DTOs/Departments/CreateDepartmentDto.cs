using System.ComponentModel.DataAnnotations;

public class CreateDepartmentDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}