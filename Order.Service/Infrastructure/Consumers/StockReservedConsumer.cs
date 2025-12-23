using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderService.Application.Saga;
using Shared.Contracts.Events;
using RabbitMQ.Client.Exceptions;

namespace OrderService.Infrastructure.Consumers;

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

			await channel.BasicAckAsync(ea.DeliveryTag, false);
		};

		await channel.BasicConsumeAsync(
			queue: nameof(StockReserved),
			autoAck: false,
			consumer: consumer);
	}
}

