using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Customer? Customer { get; set; }
    public ICollection<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();
}
