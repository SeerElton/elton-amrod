using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByEmailAsync(string email);
    Task<IEnumerable<Customer>> SearchAsync(string query);
    Task<Customer> CreateAsync(Customer customer);
}
