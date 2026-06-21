using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Contracts.Requests;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Customer ID is required")]
    public Guid CustomerId { get; set; }

    [Required(ErrorMessage = "Currency code is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be 3 characters")]
    public string CurrencyCode { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
    public decimal TotalAmount { get; set; }

    public IEnumerable<OrderLineItemRequest>? LineItems { get; set; }
}

public class OrderLineItemRequest
{
    [Required(ErrorMessage = "Product SKU is required")]
    public string ProductSku { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }
}