using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Api.GraphQL;

public class Query
{
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;

    public Query(IOrderService orderService, ICustomerService customerService)
    {
        _orderService = orderService;
        _customerService = customerService;
    }

    /// <summary>
    /// Get all orders with optional filtering
    /// </summary>
    public async Task<IEnumerable<OrderType>> GetOrders(
        string? customerId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetOrdersAsync(cancellationToken);

        if (!string.IsNullOrEmpty(customerId) && Guid.TryParse(customerId, out var customerGuid))
        {
            orders = orders.Where(o => o.CustomerId == customerGuid);
        }

        if (!string.IsNullOrEmpty(status))
        {
            orders = orders.Where(o => o.Status.ToString() == status);
        }

        return orders.Select(o => new OrderType(o));
    }

    /// <summary>
    /// Get specific order by ID
    /// </summary>
    public async Task<OrderType?> GetOrder(string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var orderId))
            return null;

        var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        return order != null ? new OrderType(order) : null;
    }

    /// <summary>
    /// Get all customers
    /// </summary>
    public async Task<IEnumerable<CustomerType>> GetCustomers(CancellationToken cancellationToken = default)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        return customers.Select(c => new CustomerType(c));
    }

    /// <summary>
    /// Get specific customer by ID
    /// </summary>
    public async Task<CustomerType?> GetCustomer(string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var customerId))
            return null;

        var customer = await _customerService.GetByIdAsync(customerId, cancellationToken);
        return customer != null ? new CustomerType(customer) : null;
    }
}
