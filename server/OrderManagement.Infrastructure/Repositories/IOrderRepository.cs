using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(Guid customerId);
    Task<Order> CreateAsync(Order order);
    Task UpdateAsync(Order order);
    Task SaveChangesAsync();
}
