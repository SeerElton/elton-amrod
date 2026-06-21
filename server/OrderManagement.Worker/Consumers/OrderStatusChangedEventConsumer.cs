using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Contracts.Events;
using OrderManagement.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderManagement.Worker.Consumers;

/// <summary>
/// Consumes OrderStatusChangedEvent messages from RabbitMQ.
/// Handles order lifecycle updates and notifications.
/// </summary>
public class OrderStatusChangedEventConsumer : BackgroundService
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<OrderStatusChangedEventConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private string? _consumerTag;

    private const string ExchangeName = "amrod.events";
    private const string QueueName = "amrod.order.status.changed";
    private const string RoutingKey = "order.orderstatuschangedevent";

    public OrderStatusChangedEventConsumer(
        IRabbitMqConnectionFactory connectionFactory,
        RabbitMqSettings settings,
        ILogger<OrderStatusChangedEventConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderStatusChangedEventConsumer starting");

        try
        {
            // Initialize connection
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            // Configure exchange
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            // Declare queue
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Bind queue to exchange
            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey
            );

            // Set QoS
            _channel.BasicQos(0, _settings.PrefetchCount, false);

            _logger.LogInformation("OrderStatusChangedEventConsumer: Queue {QueueName} configured and bound to {ExchangeName}",
                QueueName, ExchangeName);

            // Start consuming with async consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += HandleMessageAsync;

            consumer.Shutdown += async (model, ea) =>
            {
                _logger.LogWarning("Consumer shutdown: {ShutdownInitiator}", ea.Initiator);
                await Task.CompletedTask;
            };

            _consumerTag = _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumerTag: "order-status-changed-consumer",
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer
            );

            _logger.LogInformation("Started consuming messages from queue: {QueueName}", QueueName);

            // Keep the consumer running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderStatusChangedEventConsumer cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in OrderStatusChangedEventConsumer");
            throw;
        }
    }

    private async Task HandleMessageAsync(object model, BasicDeliverEventArgs ea)
    {
        await HandleMessage(ea);
    }

    private async Task HandleMessage(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            var @event = JsonSerializer.Deserialize<OrderStatusChangedEvent>(message);
            if (@event == null)
            {
                _logger.LogWarning("Failed to deserialize OrderStatusChangedEvent from message");
                _channel?.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            _logger.LogInformation("Processing OrderStatusChangedEvent: OrderId={OrderId}, PreviousStatus={PreviousStatus}, NewStatus={NewStatus}",
                @event.OrderId, @event.PreviousStatus, @event.NewStatus);

            // TODO: Implement actual workflow logic here
            // - Send notifications (email, SMS, etc.)
            // - Update external systems
            // - Trigger fulfillment workflows based on status
            // etc.

            await Task.Delay(100); // Simulate processing

            _logger.LogInformation("Completed processing OrderStatusChangedEvent: OrderId={OrderId}", @event.OrderId);

            // Acknowledge message only after successful processing
            _channel?.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from queue {QueueName}", QueueName);
            // Nack the message to retry later
            _channel?.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("OrderStatusChangedEventConsumer disposing gracefully");

        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}
