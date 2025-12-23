using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Shared.Contracts.Events;

namespace Notification.Service.Infrastructure.Consumers;

public class OrderNotificationsConsumer : BackgroundService
{
	private readonly IConnectionFactory _factory;
	private readonly ILogger<OrderNotificationsConsumer> _logger;

	public OrderNotificationsConsumer(IConnectionFactory factory,
		ILogger<OrderNotificationsConsumer> logger)
	{
		_factory = factory;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				_logger.LogInformation("Connecting to RabbitMQ...");

				var connection = await _factory.CreateConnectionAsync();
				var channel = await connection.CreateChannelAsync();

				_logger.LogInformation("RabbitMQ connected");

				await SetupConsumer(channel, stoppingToken);

				await Task.Delay(Timeout.Infinite, stoppingToken);
			}

			catch (OperationCanceledException)
			{
				break;
			}

			catch (BrokerUnreachableException)
			{
				_logger.LogError("RabbitMQ connection failed, retrying in 5s");
				await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error in OrderNotificationConsumer");
				await Task.Delay(5000, stoppingToken);
			}
		}
	}

	private async Task SetupConsumer(IChannel channel, CancellationToken stoppingToken)
	{
		await DeclareQueue<PaymentSucceeded>(channel);
		await DeclareQueue<PaymentFailed>(channel);
		await DeclareQueue<StockFailed>(channel);

		var consumer = new AsyncEventingBasicConsumer(channel);

		consumer.ReceivedAsync += async (_, ea) =>
		{
			try
			{
				var message = Encoding.UTF8.GetString(ea.Body.Span);

				switch (ea.RoutingKey)
				{
					case nameof(PaymentSucceeded):
						HandlePaymentSucceeded(message);
						break;

					case nameof(PaymentFailed):
						HandlePaymentFailed(message);
						break;

					case nameof(StockFailed):
						HandleStockFailed(message);
						break;

					default:
						_logger.LogWarning("Unknown routing key: {RoutingKey}", ea.RoutingKey);
						break;
				}

				await channel.BasicAckAsync(ea.DeliveryTag, false);
				await Task.CompletedTask;
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to process notification {RoutingKey}", ea.RoutingKey);

				await channel.BasicNackAsync(
					ea.DeliveryTag,
					multiple: false,
					requeue: true);
			}

		};

		await channel.BasicConsumeAsync(nameof(PaymentSucceeded), false, consumer);
		await channel.BasicConsumeAsync(nameof(PaymentFailed), false, consumer);
		await channel.BasicConsumeAsync(nameof(StockFailed), false, consumer);
	}

	private async Task DeclareQueue<T>(IChannel channel)
	{
		await channel.QueueDeclareAsync(
			queue: typeof(T).Name,
			durable: true,
			exclusive: false,
			autoDelete: false);
	}

	private void HandlePaymentSucceeded(string json)
	{
		var evt = JsonSerializer.Deserialize<PaymentSucceeded>(json)!;

		_logger.LogInformation(
			"📧 Order {OrderId}: payment successful, amount {Amount}",
			evt.OrderId,
			evt.Amount);
	}

	private void HandlePaymentFailed(string json)
	{
		var evt = JsonSerializer.Deserialize<PaymentFailed>(json)!;

		_logger.LogWarning(
			"📧 Order {OrderId}: payment failed ({Reason})",
			evt.OrderId,
			evt.Reason);
	}

	private void HandleStockFailed(string json)
	{
		var evt = JsonSerializer.Deserialize<StockFailed>(json)!;

		_logger.LogWarning(
			"📧 Order {OrderId}: stock reservation failed ({Reason})",
			evt.OrderId,
			evt.Reason);
	}
}
