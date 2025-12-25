using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Prometheus;
using System.Text;

namespace Shared.OutBox;

public class OutboxPublisher<TDbContext> : BackgroundService
	where TDbContext : DbContext, IOutboxDbContext
{
	private readonly IConnectionFactory _factory;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<OutboxPublisher<TDbContext>> _logger;

	private IConnection? _connection;
	private IChannel? _channel;

	public OutboxPublisher(IServiceScopeFactory scopeFactory,
		IConnectionFactory factory,
		ILogger<OutboxPublisher<TDbContext>> logger)
	{
		_scopeFactory = scopeFactory;
		_factory = factory;
		_logger = logger;
	}

	private async Task EnsureConnectedAsync()
	{
		if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
			return;

		_connection = await _factory.CreateConnectionAsync();
		_channel =  await _connection.CreateChannelAsync();
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await EnsureConnectedAsync();

				using var scope = _scopeFactory.CreateScope();
				var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

				List<OutboxMessage>? messages;
				try
				{
					messages = await db.OutboxMessages
					.Where(x => x.ProcessedAt == null)
					.Take(10)
					.ToListAsync(stoppingToken);
				}

				catch (Exception)
				{
					_logger.LogWarning("Outbox table not ready yet, retrying...");
					await Task.Delay(2000, stoppingToken);
					continue;
				}
	
				foreach (var message in messages)
				{
					var body = Encoding.UTF8.GetBytes(message.Payload);

					await _channel!.BasicPublishAsync(
						exchange: "",
						routingKey: message.Type,
						mandatory: false,
						body: body,
						cancellationToken: stoppingToken);

					message.ProcessedAt = DateTime.UtcNow;
				}

				await db.SaveChangesAsync(stoppingToken);
			}

			catch (OperationCanceledException)
			{
				break;
			}

			catch (BrokerUnreachableException)
			{
				_logger.LogError("Outbox publish failed, retrying");
				await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error in OutboxPublisher");
				await Task.Delay(5000, stoppingToken);
			}

			await Task.Delay(1000, stoppingToken);
		}
	}
}
