using OrderService.Application.Saga;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using System.Text;
using System.Text.Json;

namespace OrderService.Infrastructure.Consumers;

public class PaymentResultConsumer : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IChannel _channel;

	public PaymentResultConsumer(
		IServiceScopeFactory scopeFactory,
		IChannel channel)
	{
		_scopeFactory = scopeFactory;
		_channel = channel;
	}

	/// <summary>
	/// Comsumer слушает очереди PaymentSucceeded и Failed, меняет статус заказа
	/// </summary>
	/// <param name="stoppingToken"></param>
	/// <returns></returns>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await _channel.QueueDeclareAsync(
			queue: nameof(PaymentSucceeded),
			durable: true,
			exclusive: false,
			autoDelete: false);

		await _channel.QueueDeclareAsync(
			nameof(PaymentFailed),
			true,
			false,
			false);

		var consumer = new AsyncEventingBasicConsumer(_channel);

		consumer.ReceivedAsync += async (_, ea) =>
		{
			using var scope = _scopeFactory.CreateScope();
			var saga = scope.ServiceProvider.GetRequiredService<OrderSaga>();

			var json = Encoding.UTF8.GetString(ea.Body.Span);

			if (ea.RoutingKey == nameof(PaymentSucceeded))
			{
				var evt = JsonSerializer.Deserialize<PaymentSucceeded>(json)!;
				await saga.HandlePaymentSucceeded(evt.OrderId);
			}

			if (ea.RoutingKey == nameof(PaymentFailed))
			{
				var evt = JsonSerializer.Deserialize<PaymentFailed>(json)!;
				await saga.HandleFailure(evt.OrderId);
			}

			await _channel.BasicAckAsync(ea.DeliveryTag, false);
		};

		await _channel.BasicConsumeAsync(nameof(PaymentSucceeded), false, consumer);

		await _channel.BasicConsumeAsync(nameof(PaymentFailed), false, consumer);

		await Task.Delay(Timeout.Infinite, stoppingToken);
	}
}
