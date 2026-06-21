namespace OrderManagement.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ configuration settings from appsettings.json
/// </summary>
public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public ushort PrefetchCount { get; set; } = 10;
}
