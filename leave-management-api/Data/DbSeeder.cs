using Microsoft.EntityFrameworkCore;

public static class DbSeeder
{
    public static async Task SeedLeaveTypesAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.LeaveTypes.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        db.LeaveTypes.AddRange(
            new LeaveType
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Annual Leave",
                Description = "Paid annual leave for vacation or personal time off.",
                DefaultDaysPerYear = 21,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new LeaveType
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Sick Leave",
                Description = "Leave for illness, medical appointments, or recovery.",
                DefaultDaysPerYear = 10,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new LeaveType
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Casual Leave",
                Description = "Short-notice personal leave for urgent matters.",
                DefaultDaysPerYear = 7,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

        await db.SaveChangesAsync();
    }
}