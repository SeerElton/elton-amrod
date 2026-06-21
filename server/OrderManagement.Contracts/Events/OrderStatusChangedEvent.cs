namespace OrderManagement.Contracts.Events;

/// <summary>
/// Event published when an order status changes.
/// Used for outbox pattern and RabbitMQ integration.
/// </summary>
public class OrderStatusChangedEvent
{
    /// <summary>Unique order identifier</summary>
    public Guid OrderId { get; set; }

    /// <summary>Customer ID</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Previous order status</summary>
    public string PreviousStatus { get; set; } = string.Empty;

    /// <summary>New order status</summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>Reason for status change</summary>
    public string? Reason { get; set; }

    /// <summary>Timestamp when status changed</summary>
    public DateTime ChangedAt { get; set; }
}
