using Microsoft.EntityFrameworkCore;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.Db;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Food>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired().HasMaxLength(200);
            entity.Property(f => f.Description).HasMaxLength(1000);
            entity.Property(f => f.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasOne(m => m.Food)
                  .WithMany()
                  .HasForeignKey(m => m.FoodId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(m => m.RowVersion)
                  .IsRowVersion()
                  .IsConcurrencyToken();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasOne(o => o.MenuItem)
                  .WithMany()
                  .HasForeignKey(o => o.MenuItemId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(o => o.Status)
                  .HasConversion<string>();
        });
    }
}
