using static Order.Domain.Models.Order;

namespace Order.Domain.Repositories;

public interface IOrderRepository
{
    Task<IOrder?> GetByIdAsync(Guid id);
    Task<IEnumerable<IOrder>> GetByPhoneAsync(string phone);
    Task<Guid> SaveAsync(IOrder order);
    Task<IEnumerable<IOrder>> GetAllAsync();
}
