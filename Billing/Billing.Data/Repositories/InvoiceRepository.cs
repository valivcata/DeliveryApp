using Billing.Data.Models;
using Billing.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using static Billing.Domain.Models.Invoice;

namespace Billing.Data.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly BillingDbContext _context;

    public InvoiceRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<IInvoice?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Invoices.FindAsync(id);
        return entity != null ? MapToInvoice(entity) : null;
    }

    public async Task<IEnumerable<IInvoice>> GetByCustomerPhoneAsync(string phone)
    {
        var entities = await _context.Invoices
            .Where(i => i.CustomerPhone == phone)
            .ToListAsync();
        return entities.Select(MapToInvoice);
    }

    public async Task<IEnumerable<IInvoice>> GetAllAsync()
    {
        var entities = await _context.Invoices.ToListAsync();
        return entities.Select(MapToInvoice);
    }

    public async Task<Guid> SaveAsync(IInvoice invoice)
    {
        // Validate that only final states are persisted
        if (invoice is UnprocessedInvoice)
        {
            throw new InvalidOperationException("Cannot save unprocessed invoice. Complete calculation first.");
        }
        
        if (invoice is CalculatedInvoice)
        {
            throw new InvalidOperationException("Cannot save calculated invoice. Complete the issue operation first.");
        }

        var entity = MapToEntity(invoice);
        
        _context.Invoices.Add(entity);
        await _context.SaveChangesAsync();
        
        return entity.Id;
    }

    private static IInvoice MapToInvoice(InvoiceEntity entity)
    {
        return new UnprocessedInvoice(
            entity.RestaurantId,
            entity.CustomerPhone,
            entity.Amount
        );
    }

    private static InvoiceEntity MapToEntity(IInvoice invoice)
    {
        return invoice switch
        {
            InvoiceIssued issued => new InvoiceEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = issued.OrderRef.RestaurantId,
                CustomerPhone = issued.OrderRef.CustomerPhone,
                Amount = issued.Amount.Value,
                Tax = issued.Tax.Value,
                Total = issued.Total.Value,
                Status = "Issued",
                CreatedAt = DateTime.UtcNow,
                IssuedAt = issued.IssuedAt
            },
            CalculatedInvoice calculated => new InvoiceEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = calculated.OrderRef.RestaurantId,
                CustomerPhone = calculated.OrderRef.CustomerPhone,
                Amount = calculated.Amount.Value,
                Tax = calculated.Tax.Value,
                Total = calculated.Total.Value,
                Status = "Calculated",
                CreatedAt = DateTime.UtcNow
            },
            InvalidInvoice invalid => new InvoiceEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = string.Empty,
                CustomerPhone = string.Empty,
                Amount = 0,
                Tax = 0,
                Total = 0,
                Status = "Invalid",
                CreatedAt = DateTime.UtcNow,
                ErrorReason = invalid.Reason
            },
            UnprocessedInvoice unprocessed => new InvoiceEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = unprocessed.OrderRestaurantId,
                CustomerPhone = unprocessed.OrderCustomerPhone,
                Amount = unprocessed.OrderAmount,
                Tax = 0,
                Total = 0,
                Status = "Unprocessed",
                CreatedAt = DateTime.UtcNow
            },
            _ => throw new InvalidOperationException($"Unknown invoice state: {invoice.GetType().Name}")
        };
    }
}
