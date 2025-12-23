using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using System.Text;
using System.Text.Json;
using InventoryService.Application.Services;
using RabbitMQ.Client.Exceptions;

namespace InventoryService.Infrastructure.Consumers;

public class OrderCreatedConsumer : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IConnectionFactory _factory;
	private readonly ILogger<OrderCreatedConsumer> _logger;

	public OrderCreatedConsumer(
		IServiceScopeFactory scopeFactory,
		IConnectionFactory factory,
		ILogger<OrderCreatedConsumer> logger)
	{
		_scopeFactory = scopeFactory;
		_factory = factory;
		_logger = logger;
	}

	/// <summary>
	/// Comsumer слушает очередь OrderCreated, запускает логику резервирования товаров
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
				_logger.LogError(ex, "Unexpected error in OrderCreatedConsumer");
				await Task.Delay(5000, stoppingToken);
			}
		}
	}

	private async Task SetupConsumer(IChannel channel, CancellationToken stoppingToken)
	{
		await channel.QueueDeclareAsync(
			queue: nameof(OrderCreated),
			durable: true,
			exclusive: false,
			autoDelete: false);

		var consumer = new AsyncEventingBasicConsumer(channel);

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

			await channel.BasicAckAsync(ea.DeliveryTag, false);
		};

		await channel.BasicConsumeAsync(
			queue: nameof(OrderCreated),
			autoAck: false,
			consumer: consumer);
	}
}
