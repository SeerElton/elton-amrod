using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly OrderManagementDbContext _dbContext;

    public CustomerRepository(OrderManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        return await _dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<IEnumerable<Customer>> SearchAsync(string query)
    {
        var lower = query.ToLower();
        return await _dbContext.Customers
            .Where(c => c.Email.ToLower().Contains(lower) || c.Name.ToLower().Contains(lower))
            .Take(10)
            .ToListAsync();
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();
        return customer;
    }
}
