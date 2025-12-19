using Microsoft.EntityFrameworkCore;
using OrderService.Application.Saga;
using OrderService.Infrastructure.Consumers;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Persistence;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog();

// -------------------- Controllers --------------------
builder.Services.AddControllers();

// -------------------- Database --------------------
builder.Services.AddDbContext<OrderDbContext>(options =>
{
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("Postgres"));
});

// -------------------- Saga --------------------
builder.Services.AddScoped<OrderSaga>();

// -------------------- RabbitMQ --------------------
builder.Services.AddSingleton<IConnection>(_ =>
{
	var factory = new ConnectionFactory
	{
		HostName = "rabbitmq"
	};

	return factory.CreateConnectionAsync()
					.GetAwaiter()
					.GetResult();
});

builder.Services.AddSingleton<IChannel>(sp =>
{
	var connection = sp.GetRequiredService<IConnection>();
	return connection.CreateChannelAsync()
						.GetAwaiter()
						.GetResult();
});

// -------------------- Outbox Publisher --------------------
builder.Services.AddHostedService<OutboxPublisher>();

// -------------------- Consumers --------------------
builder.Services.AddHostedService<StockReservedConsumer>();
builder.Services.AddHostedService<PaymentResultConsumer>();

var app = builder.Build();

app.MapControllers();
app.Run();