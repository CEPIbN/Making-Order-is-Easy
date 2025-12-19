using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderService.Application.Saga;
using Shared.Contracts.Events;

namespace OrderService.Infrastructure.Consumers;

public class StockReservedConsumer : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IChannel _channel;

	public StockReservedConsumer(
		IServiceScopeFactory scopeFactory,
		IChannel channel)
	{
		_scopeFactory = scopeFactory;
		_channel = channel;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await _channel.QueueDeclareAsync(
			queue: nameof(StockReserved),
			durable: true,
			exclusive: false,
			autoDelete: false);

		var consumer = new AsyncEventingBasicConsumer(_channel);

		consumer.ReceivedAsync += async (_, ea) =>
		{
			using var scope = _scopeFactory.CreateScope();
			var saga = scope.ServiceProvider.GetRequiredService<OrderSaga>();

			var json = Encoding.UTF8.GetString(ea.Body.Span);

			if (ea.RoutingKey == nameof(StockReserved))
			{
				var evt = JsonSerializer.Deserialize<StockReserved>(json)!;
				await saga.HandleStockReserved(evt.OrderId);
			}

			if (ea.RoutingKey == nameof(StockFailed))
			{
				var evt = JsonSerializer.Deserialize<StockFailed>(json)!;
				await saga.HandleFailure(evt.OrderId);
			}

			await _channel.BasicAckAsync(ea.DeliveryTag, false);
		};

		await _channel.BasicConsumeAsync(
			queue: nameof(StockReserved),
			autoAck: false,
			consumer: consumer);

		await Task.Delay(Timeout.Infinite, stoppingToken);
	}
}

