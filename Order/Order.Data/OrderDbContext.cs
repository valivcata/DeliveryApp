using Microsoft.EntityFrameworkCore;
using Order.Data.Models;

namespace Order.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RestaurantId)
                .IsRequired()
                .HasMaxLength(20);
            
            entity.Property(e => e.CustomerPhone)
                .IsRequired()
                .HasMaxLength(20);
            
            entity.Property(e => e.DeliveryAddress)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.OrderAmount)
                .HasPrecision(18, 2);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.RestaurantId);
            entity.HasIndex(e => e.CustomerPhone);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
