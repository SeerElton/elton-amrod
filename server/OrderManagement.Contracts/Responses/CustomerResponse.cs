namespace OrderManagement.Contracts.Responses;

public class CustomerResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
