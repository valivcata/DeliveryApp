using static Delivery.Domain.Models.Delivery;

namespace Delivery.Domain.Repositories;

public interface IDeliveryRepository
{
    Task<IDelivery?> GetByIdAsync(Guid id);
    Task<IEnumerable<IDelivery>> GetByCustomerPhoneAsync(string phone);
    Task<Guid> SaveAsync(IDelivery delivery);
    Task<IEnumerable<IDelivery>> GetAllAsync();
}
