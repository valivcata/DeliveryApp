using static Billing.Domain.Models.Invoice;

namespace Billing.Domain.Repositories;

public interface IInvoiceRepository
{
    Task<IInvoice?> GetByIdAsync(Guid id);
    Task<IEnumerable<IInvoice>> GetByCustomerPhoneAsync(string phone);
    Task<Guid> SaveAsync(IInvoice invoice);
    Task<IEnumerable<IInvoice>> GetAllAsync();
}
