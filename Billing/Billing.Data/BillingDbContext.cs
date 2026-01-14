using Microsoft.EntityFrameworkCore;
using Billing.Data.Models;

namespace Billing.Data;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
    }

    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InvoiceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RestaurantId)
                .IsRequired()
                .HasMaxLength(20);
            
            entity.Property(e => e.CustomerPhone)
                .IsRequired()
                .HasMaxLength(20);
            
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);
            
            entity.Property(e => e.Tax)
                .HasPrecision(18, 2);
            
            entity.Property(e => e.Total)
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
