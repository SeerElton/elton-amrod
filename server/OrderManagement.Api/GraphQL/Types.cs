using OrderManagement.Domain.Entities;

namespace OrderManagement.Api.GraphQL;

public class OrderType
{
    private readonly Order _order;

    public OrderType(Order order)
    {
        _order = order;
    }

    public string Id => _order.Id.ToString();
    public string CustomerId => _order.CustomerId.ToString();
    public string Status => _order.Status.ToString();
    public string CurrencyCode => _order.CurrencyCode;
    public decimal TotalAmount => _order.TotalAmount;
    public DateTime CreatedAt => _order.CreatedAt;

    /// <summary>
    /// Get line items for this order
    /// </summary>
    public IEnumerable<OrderLineItemType> LineItems =>
        _order.LineItems?.Select(li => new OrderLineItemType(li)) ?? Enumerable.Empty<OrderLineItemType>();

    /// <summary>
    /// Get customer for this order (N+1 prevention with DataLoader recommended)
    /// </summary>
    public CustomerType? Customer =>
        _order.Customer != null ? new CustomerType(_order.Customer) : null;
}

public class OrderLineItemType
{
    private readonly OrderLineItem _lineItem;

    public OrderLineItemType(OrderLineItem lineItem)
    {
        _lineItem = lineItem;
    }

    public string Id => _lineItem.Id.ToString();
    public string ProductSku => _lineItem.ProductSku;
    public int Quantity => _lineItem.Quantity;
    public decimal UnitPrice => _lineItem.UnitPrice;
    public decimal LineTotal => _lineItem.Quantity * _lineItem.UnitPrice;
}

public class CustomerType
{
    private readonly Customer _customer;

    public CustomerType(Customer customer)
    {
        _customer = customer;
    }

    public string Id => _customer.Id.ToString();
    public string Name => _customer.Name;
    public string Email => _customer.Email;
    public string CountryCode => _customer.CountryCode;
    public DateTime CreatedAt => _customer.CreatedAt;

    /// <summary>
    /// Get orders for this customer (N+1 prevention with DataLoader recommended)
    /// </summary>
    public IEnumerable<OrderType> Orders =>
        _customer.Orders?.Select(o => new OrderType(o)) ?? Enumerable.Empty<OrderType>();
}
