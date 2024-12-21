using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<WebhookLog> WebhookLogs { get; set; }
    public DbSet<TaskLog> TaskLogs { get; set; }
}
