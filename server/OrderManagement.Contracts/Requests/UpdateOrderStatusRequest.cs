using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Contracts.Requests;

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; } = string.Empty;
}
