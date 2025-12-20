using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Shared.Contracts.Events;

namespace Notification.Service.Infrastructure.Consumers;

public class OrderNotificationsConsumer : BackgroundService
{
	private readonly IChannel _channel;
	private readonly Serilog.ILogger _logger;

	public OrderNotificationsConsumer(IChannel channel)
	{
		_channel = channel;
		_logger = Log.ForContext<OrderNotificationsConsumer>();
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await DeclareQueue<PaymentSucceeded>();
		await DeclareQueue<PaymentFailed>();
		await DeclareQueue<StockFailed>();

		var consumer = new AsyncEventingBasicConsumer(_channel);

		consumer.ReceivedAsync += async (_, ea) =>
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
			}

			await _channel.BasicAckAsync(ea.DeliveryTag, false);
			await Task.CompletedTask;
		};

		await _channel.BasicConsumeAsync(nameof(PaymentSucceeded), false, consumer);
		await _channel.BasicConsumeAsync(nameof(PaymentFailed), false, consumer);
		await _channel.BasicConsumeAsync(nameof(StockFailed), false, consumer);

		await Task.Delay(Timeout.Infinite, stoppingToken);
	}

	private async Task DeclareQueue<T>()
	{
		await _channel.QueueDeclareAsync(
			queue: typeof(T).Name,
			durable: true,
			exclusive: false,
			autoDelete: false);
	}

	private void HandlePaymentSucceeded(string json)
	{
		var evt = JsonSerializer.Deserialize<PaymentSucceeded>(json)!;

		_logger.Information(
			"📧 Order {OrderId}: payment successful, amount {Amount}",
			evt.OrderId,
			evt.Amount);
	}

	private void HandlePaymentFailed(string json)
	{
		var evt = JsonSerializer.Deserialize<PaymentFailed>(json)!;

		_logger.Warning(
			"📧 Order {OrderId}: payment failed ({Reason})",
			evt.OrderId,
			evt.Reason);
	}

	private void HandleStockFailed(string json)
	{
		var evt = JsonSerializer.Deserialize<StockFailed>(json)!;

		_logger.Warning(
			"📧 Order {OrderId}: stock reservation failed ({Reason})",
			evt.OrderId,
			evt.Reason);
	}
}
