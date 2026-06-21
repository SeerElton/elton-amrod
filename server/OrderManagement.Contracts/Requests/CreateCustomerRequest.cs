using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Contracts.Requests;

public class CreateCustomerRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, ErrorMessage = "Name must not exceed 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "A valid email address is required")]
    [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country code is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be exactly 2 characters")]
    public string CountryCode { get; set; } = string.Empty;
}
