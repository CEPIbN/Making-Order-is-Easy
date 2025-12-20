using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.OutBox;

public class OutboxPublisher<TDbContext> : BackgroundService
	where TDbContext : DbContext, IOutboxDbContext
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IChannel _channel;

	public OutboxPublisher(IServiceScopeFactory scopeFactory, IChannel channel)
	{
		_scopeFactory = scopeFactory;
		_channel = channel;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			using var scope = _scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

			var messages = await db.OutboxMessages
				.Where(x => x.ProcessedAt == null)
				.Take(10)
				.ToListAsync(stoppingToken);

			foreach (var message in messages)
			{
				var body = Encoding.UTF8.GetBytes(message.Payload);

				await _channel.BasicPublishAsync(
					exchange: "",
					routingKey: message.Type,
					mandatory: false,
					body: body,
					cancellationToken: stoppingToken);

				message.ProcessedAt = DateTime.UtcNow;
			}

			await db.SaveChangesAsync(stoppingToken);
			await Task.Delay(1000, stoppingToken);
		}
	}
}
