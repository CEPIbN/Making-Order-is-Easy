using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using System.Text;
using System.Text.Json;
using InventoryService.Application.Services;

namespace InventoryService.Infrastructure.Consumers;

public class OrderCreatedConsumer : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IChannel _channel;

	public OrderCreatedConsumer(
		IServiceScopeFactory scopeFactory,
		IChannel channel)
	{
		_scopeFactory = scopeFactory;
		_channel = channel;
	}

	/// <summary>
	/// Comsumer слушает очередь OrderCreated, запускает логику резервирования товаров
	/// </summary>
	/// <param name="stoppingToken"></param>
	/// <returns></returns>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await _channel.QueueDeclareAsync(
			queue: nameof(OrderCreated),
			durable: true,
			exclusive: false,
			autoDelete: false);

		var consumer = new AsyncEventingBasicConsumer(_channel);

		consumer.ReceivedAsync += async (_, ea) =>
		{
			using var scope = _scopeFactory.CreateScope();
			var service = scope.ServiceProvider.GetRequiredService<InventoryServiceType>();

			var json = Encoding.UTF8.GetString(ea.Body.Span);
			var evt = JsonSerializer.Deserialize<OrderCreated>(json)!;

			await service.HandleOrderCreated(
				evt.OrderId,
				evt.ProductId,
				evt.Quantity,
				evt.Price);

			await _channel.BasicAckAsync(ea.DeliveryTag, false);
		};

		await _channel.BasicConsumeAsync(
			queue: nameof(OrderCreated),
			autoAck: false,
			consumer: consumer);

		await Task.Delay(Timeout.Infinite, stoppingToken);
	}
}
