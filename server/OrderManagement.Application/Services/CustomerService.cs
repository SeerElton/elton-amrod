using OrderManagement.Contracts.Requests;
using OrderManagement.Contracts.Responses;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Repositories;

namespace OrderManagement.Application.Services;

public interface ICustomerService
{
    Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request);
    Task<CustomerResponse?> GetCustomerAsync(Guid id);
    Task<IEnumerable<CustomerResponse>> SearchCustomersAsync(string query);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var existing = await _customerRepository.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException($"A customer with email '{request.Email}' already exists");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            CountryCode = request.CountryCode,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _customerRepository.CreateAsync(customer);
        return MapToResponse(created);
    }

    public async Task<CustomerResponse?> GetCustomerAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        return customer == null ? null : MapToResponse(customer);
    }

    public async Task<IEnumerable<CustomerResponse>> SearchCustomersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var customers = await _customerRepository.SearchAsync(query);
        return customers.Select(MapToResponse);
    }

    private static CustomerResponse MapToResponse(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        CountryCode = customer.CountryCode,
        CreatedAt = customer.CreatedAt
    };
}
