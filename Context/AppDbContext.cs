using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<WebHookRecive> WebHookRecives { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebHookRecive>()
            .Property(w => w.Type)
            .HasMaxLength(20);

        modelBuilder.Entity<WebHookRecive>()
            .Property(w => w.TicketId)
            .HasMaxLength(50);

        base.OnModelCreating(modelBuilder);
    }
}