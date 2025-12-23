using PaymentService.Application.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Shared.Contracts.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace PaymentService.Infrastructure.Consumers;

public class StockReservedConsumer : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IConnectionFactory _factory;
	private readonly ILogger<StockReservedConsumer> _logger;

	public StockReservedConsumer(
		IServiceScopeFactory scopeFactory,
		IConnectionFactory factory,
		ILogger<StockReservedConsumer> logger)
	{
		_scopeFactory = scopeFactory;
		_factory = factory;
		_logger = logger;
	}

	/// <summary>
	/// Comsumer слушает очередь StockReserved, запускает логику оплаты заказа
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
				_logger.LogError(ex, "Unexpected error in StockReservedConsumer");
				await Task.Delay(5000, stoppingToken);
			}
		}
	}

	private async Task SetupConsumer(IChannel channel, CancellationToken stoppingToken)
	{
		await channel.QueueDeclareAsync(
			queue: nameof(StockReserved),
			durable: true,
			exclusive: false,
			autoDelete: false);

		var consumer = new AsyncEventingBasicConsumer(channel);

		consumer.ReceivedAsync += async (_, ea) =>
		{
			using var scope = _scopeFactory.CreateScope();
			var service = scope.ServiceProvider.GetRequiredService<PaymentServiceType>();

			var json = Encoding.UTF8.GetString(ea.Body.Span);
			var evt = JsonSerializer.Deserialize<StockReserved>(json)!;

			await service.HandleStockReserved(
				evt.OrderId,
				amount: evt.Price * evt.Quantity
			);

			await channel.BasicAckAsync(ea.DeliveryTag, false);
		};

		await channel.BasicConsumeAsync(
			queue: nameof(StockReserved),
			autoAck: false,
			consumer: consumer);
	}
}