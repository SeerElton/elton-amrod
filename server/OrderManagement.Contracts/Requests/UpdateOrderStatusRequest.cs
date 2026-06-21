using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Contracts.Requests;

public class UpdateOrderStatusRequest
{
    /// <summary>New order status (Pending, Paid, Fulfilled, Cancelled)</summary>
    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Optional reason for status change</summary>
    public string? Reason { get; set; }
}
