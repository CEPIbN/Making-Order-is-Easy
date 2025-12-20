using PaymentService.Application.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace PaymentService.Infrastructure.Consumers;

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

	/// <summary>
	/// Comsumer слушает очередь StockReserved, запускает логику резервирования товаров
	/// </summary>
	/// <param name="stoppingToken"></param>
	/// <returns></returns>
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
			var service = scope.ServiceProvider.GetRequiredService<PaymentServiceType>();

			var json = Encoding.UTF8.GetString(ea.Body.Span);
			var evt = JsonSerializer.Deserialize<StockReserved>(json)!;

			await service.HandleStockReserved(
				evt.OrderId,
				amount: evt.Price // для примера, дальше можно брать из Order
			);

			await _channel.BasicAckAsync(ea.DeliveryTag, false);
		};

		await _channel.BasicConsumeAsync(
			queue: nameof(StockReserved),
			autoAck: false,
			consumer: consumer);

		await Task.Delay(Timeout.Infinite, stoppingToken);
	}
}