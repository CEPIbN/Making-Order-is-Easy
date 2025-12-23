using OrderService.Application.Saga;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Shared.Contracts.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace OrderService.Infrastructure.Consumers;

public class PaymentResultConsumer : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IConnectionFactory _factory;
	private readonly ILogger<PaymentResultConsumer> _logger;

	public PaymentResultConsumer(
		IServiceScopeFactory scopeFactory,
		IConnectionFactory factory,
		ILogger<PaymentResultConsumer> logger)
	{
		_scopeFactory = scopeFactory;
		_factory = factory;
		_logger = logger;
	}

	/// <summary>
	/// Comsumer слушает очереди PaymentSucceeded и Failed, меняет статус заказа
	/// </summary>
	/// <param name="stoppingToken"></param>
	/// <returns></returns>
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
				_logger.LogError(ex, "Unexpected error in PaymentResultConsumer");
				await Task.Delay(5000, stoppingToken);
			}
		}
	}

	private async Task SetupConsumer(IChannel channel, CancellationToken stoppingToken)
	{
		await channel.QueueDeclareAsync(
			queue: nameof(PaymentSucceeded),
			durable: true,
			exclusive: false,
			autoDelete: false);

		await channel.QueueDeclareAsync(
			nameof(PaymentFailed),
			true,
			false,
			false);

		var consumer = new AsyncEventingBasicConsumer(channel);

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

			await channel.BasicAckAsync(ea.DeliveryTag, false);
		};

		await channel.BasicConsumeAsync(nameof(PaymentSucceeded), false, consumer);

		await channel.BasicConsumeAsync(nameof(PaymentFailed), false, consumer);
	}
}
