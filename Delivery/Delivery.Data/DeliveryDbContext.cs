using Microsoft.EntityFrameworkCore;
using Delivery.Data.Models;

namespace Delivery.Data;

public class DeliveryDbContext : DbContext
{
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
    {
    }

    public DbSet<DeliveryEntity> Deliveries => Set<DeliveryEntity>();
    public DbSet<ProcessedMessageEntity> ProcessedMessages => Set<ProcessedMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeliveryEntity>(entity =>
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
            
            entity.Property(e => e.DriverId)
                .HasMaxLength(20);
            
            entity.Property(e => e.Route)
                .HasMaxLength(1000);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.RestaurantId);
            entity.HasIndex(e => e.CustomerPhone);
            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.CreatedAt);
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
