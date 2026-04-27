using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<LeaveApproval> LeaveApprovals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.PasswordHash)
            .HasColumnName("PasswordHash");

        modelBuilder.Entity<LeaveRequest>()
            .Property(lr => lr.Status)
            .HasConversion<string>();

        modelBuilder.Entity<LeaveApproval>()
            .Property(la => la.Action)
            .HasConversion<string>();

        // RemainingDays is computed — it is not stored in the database, so we ignore it in EF Core
        modelBuilder.Entity<LeaveBalance>()
            .Ignore(b => b.RemainingDays);

        //Auto-set CreatedAt to current time when inserting new records
        modelBuilder.Entity<User>()
        .Property(u => u.CreatedAt)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<Department>()
        .Property(d => d.CreatedAt)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<LeaveType>()
        .Property(lt => lt.CreatedAt)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<LeaveRequest>()
        .Property(lr => lr.CreatedAt)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<LeaveBalance>()
        .Property(lb => lb.CreatedAt)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<LeaveApproval>()
        .Property(la => la.CreatedAt)
            .HasDefaultValueSql("NOW()");


    }
}