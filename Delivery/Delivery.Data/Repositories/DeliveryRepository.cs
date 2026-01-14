using Delivery.Data.Models;
using Delivery.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using static Delivery.Domain.Models.Delivery;

namespace Delivery.Data.Repositories;

public class DeliveryRepository : IDeliveryRepository
{
    private readonly DeliveryDbContext _context;

    public DeliveryRepository(DeliveryDbContext context)
    {
        _context = context;
    }

    public async Task<IDelivery?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Deliveries.FindAsync(id);
        return entity != null ? MapToDelivery(entity) : null;
    }

    public async Task<IEnumerable<IDelivery>> GetByCustomerPhoneAsync(string phone)
    {
        var entities = await _context.Deliveries
            .Where(d => d.CustomerPhone == phone)
            .ToListAsync();
        return entities.Select(MapToDelivery);
    }

    public async Task<IEnumerable<IDelivery>> GetAllAsync()
    {
        var entities = await _context.Deliveries.ToListAsync();
        return entities.Select(MapToDelivery);
    }

    public async Task<Guid> SaveAsync(IDelivery delivery)
    {
        var entity = MapToEntity(delivery);
        
        _context.Deliveries.Add(entity);
        await _context.SaveChangesAsync();
        
        return entity.Id;
    }

    private static IDelivery MapToDelivery(DeliveryEntity entity)
    {
        return new RequestedDelivery(
            entity.RestaurantId,
            entity.CustomerPhone,
            entity.DeliveryAddress,
            0 // Amount not stored in entity for this MVP
        );
    }

    private static DeliveryEntity MapToEntity(IDelivery delivery)
    {
        return delivery switch
        {
            DeliveryStarted started => new DeliveryEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = started.InvoiceRef.RestaurantId,
                CustomerPhone = started.InvoiceRef.CustomerPhone,
                DeliveryAddress = started.Destination.Value,
                DriverId = started.Driver.Value,
                Route = started.Route.Value,
                Status = "Started",
                CreatedAt = DateTime.UtcNow,
                StartedAt = started.StartedAt
            },
            AssignedDelivery assigned => new DeliveryEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = assigned.InvoiceRef.RestaurantId,
                CustomerPhone = assigned.InvoiceRef.CustomerPhone,
                DeliveryAddress = assigned.Destination.Value,
                DriverId = assigned.Driver.Value,
                Route = assigned.Route.Value,
                Status = "Assigned",
                CreatedAt = DateTime.UtcNow
            },
            FailedDelivery failed => new DeliveryEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = string.Empty,
                CustomerPhone = string.Empty,
                DeliveryAddress = string.Empty,
                Status = "Failed",
                CreatedAt = DateTime.UtcNow,
                ErrorReason = failed.Reason
            },
            RequestedDelivery requested => new DeliveryEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = requested.RestaurantId,
                CustomerPhone = requested.CustomerPhone,
                DeliveryAddress = requested.DeliveryAddress,
                Status = "Requested",
                CreatedAt = DateTime.UtcNow
            },
            _ => throw new InvalidOperationException($"Unknown delivery state: {delivery.GetType().Name}")
        };
    }
}
