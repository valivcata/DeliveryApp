using Order.Data.Models;
using Order.Domain.Models;
using Order.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using static Order.Domain.Models.Order;

namespace Order.Data.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<IOrder?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Orders.FindAsync(id);
        return entity != null ? MapToOrder(entity) : null;
    }

    public async Task<IEnumerable<IOrder>> GetByPhoneAsync(string phone)
    {
        var entities = await _context.Orders
            .Where(o => o.CustomerPhone == phone)
            .ToListAsync();
        return entities.Select(MapToOrder);
    }

    public async Task<IEnumerable<IOrder>> GetAllAsync()
    {
        var entities = await _context.Orders.ToListAsync();
        return entities.Select(MapToOrder);
    }

    public async Task<Guid> SaveAsync(IOrder order)
    {
        // Validate that only final states are persisted
        if (order is UnvalidatedOrder)
        {
            throw new InvalidOperationException("Cannot save unvalidated order. Complete validation first.");
        }
        
        if (order is ValidatedOrder)
        {
            throw new InvalidOperationException("Cannot save validated order. Complete the placement operation first.");
        }

        var entity = MapToEntity(order);
        
        _context.Orders.Add(entity);
        await _context.SaveChangesAsync();
        
        return entity.Id;
    }

    private static IOrder MapToOrder(OrderEntity entity)
    {
        return entity.Status switch
        {
            "Placed" => new OrderPlaced(
                RestaurantId.Create(entity.RestaurantId),
                CustomerPhone.Create(entity.CustomerPhone),
                DeliveryAddress.Create(entity.DeliveryAddress),
                OrderAmount.Create(entity.OrderAmount),
                entity.PlacedAt ?? entity.CreatedAt
            ),
            "Validated" => new ValidatedOrder(
                RestaurantId.Create(entity.RestaurantId),
                CustomerPhone.Create(entity.CustomerPhone),
                DeliveryAddress.Create(entity.DeliveryAddress),
                OrderAmount.Create(entity.OrderAmount)
            ),
            "Invalid" => new InvalidOrder(entity.ErrorReason ?? "Unknown error"),
            _ => new UnvalidatedOrder(
                entity.RestaurantId,
                entity.CustomerPhone,
                entity.DeliveryAddress,
                entity.OrderAmount
            )
        };
    }

    private static OrderEntity MapToEntity(IOrder order)
    {
        return order switch
        {
            OrderPlaced placed => new OrderEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = placed.Restaurant.Value,
                CustomerPhone = placed.Phone.Value,
                DeliveryAddress = placed.Address.Value,
                OrderAmount = placed.Amount.Value,
                Status = "Placed",
                CreatedAt = DateTime.UtcNow,
                PlacedAt = placed.PlacedAt
            },
            ValidatedOrder validated => new OrderEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = validated.Restaurant.Value,
                CustomerPhone = validated.Phone.Value,
                DeliveryAddress = validated.Address.Value,
                OrderAmount = validated.Amount.Value,
                Status = "Validated",
                CreatedAt = DateTime.UtcNow
            },
            InvalidOrder invalid => new OrderEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = string.Empty,
                CustomerPhone = string.Empty,
                DeliveryAddress = string.Empty,
                OrderAmount = 0,
                Status = "Invalid",
                CreatedAt = DateTime.UtcNow,
                ErrorReason = invalid.Reason
            },
            UnvalidatedOrder unvalidated => new OrderEntity
            {
                Id = Guid.NewGuid(),
                RestaurantId = unvalidated.RestaurantId,
                CustomerPhone = unvalidated.CustomerPhone,
                DeliveryAddress = unvalidated.DeliveryAddress,
                OrderAmount = unvalidated.OrderAmount,
                Status = "Unvalidated",
                CreatedAt = DateTime.UtcNow
            },
            _ => throw new InvalidOperationException($"Unknown order state: {order.GetType().Name}")
        };
    }
}
