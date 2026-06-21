using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace OrderManagement.Infrastructure.Messaging;

/// <summary>
/// Factory for creating and managing RabbitMQ connections.
/// </summary>
public interface IRabbitMqConnectionFactory
{
    IConnection CreateConnection();
}

public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;

    public RabbitMqConnectionFactory(RabbitMqSettings settings, ILogger<RabbitMqConnectionFactory> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            AutomaticRecoveryEnabled = true,
            RequestedChannelMax = 2048,
            DispatchConsumersAsync = true
        };

        try
        {
            var connection = factory.CreateConnection();
            _logger.LogInformation("RabbitMQ connection established: {HostName}:{Port}", 
                _settings.HostName, _settings.Port);
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ connection to {HostName}:{Port}", 
                _settings.HostName, _settings.Port);
            throw;
        }
    }
}
