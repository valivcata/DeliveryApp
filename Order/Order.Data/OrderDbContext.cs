using Microsoft.EntityFrameworkCore;
using Order.Data.Models;

namespace Order.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<RestaurantEntity> Restaurants => Set<RestaurantEntity>();
    public DbSet<ProcessedMessageEntity> ProcessedMessages => Set<ProcessedMessageEntity>();

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

        modelBuilder.Entity<RestaurantEntity>(entity =>
        {
            entity.HasKey(e => e.RestaurantId);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Cuisine)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.MinimumOrder)
                .HasPrecision(18, 2);
            
            entity.Property(e => e.DeliveryFee)
                .HasPrecision(18, 2);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<ProcessedMessageEntity>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            
            entity.Property(e => e.MessageId)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.ProcessorName)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.ProcessedAt);
        });
    }
}
