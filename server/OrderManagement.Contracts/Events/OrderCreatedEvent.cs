namespace OrderManagement.Contracts.Events;

/// <summary>
/// Event published when an order is created.
/// Used for outbox pattern and RabbitMQ integration.
/// </summary>
public class OrderCreatedEvent
{
    /// <summary>Unique order identifier</summary>
    public Guid OrderId { get; set; }

    /// <summary>Customer ID who created the order</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Total amount for the order</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Currency code (e.g., ZAR, USD)</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Number of line items in the order</summary>
    public int LineItemCount { get; set; }

    /// <summary>Timestamp when order was created</summary>
    public DateTime CreatedAt { get; set; }
}
