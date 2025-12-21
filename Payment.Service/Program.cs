using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Services;
using PaymentService.Infrastructure.Consumers;
using PaymentService.Infrastructure.Persistence;
using RabbitMQ.Client;
using Serilog;
using Shared.OutBox;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Logging --------------------
builder.Host.UseSerilog((ctx, lc) =>
{
	lc.ReadFrom.Configuration(ctx.Configuration)
	  .WriteTo.Console();
});

// -------------------- Database --------------------
builder.Services.AddDbContext<PaymentDbContext>(options =>
{
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("Postgres"));
});

// -------------------- Application --------------------
builder.Services.AddScoped<PaymentServiceType>();

// -------------------- RabbitMQ --------------------
builder.Services.AddSingleton<IConnection>(_ =>
{
	var factory = new ConnectionFactory { HostName = "rabbitmq" };
	return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IChannel>(sp =>
{
	var connection = sp.GetRequiredService<IConnection>();
	return connection.CreateChannelAsync().GetAwaiter().GetResult();
});

// -------------------- Messaging --------------------
builder.Services.AddHostedService<OutboxPublisher<PaymentDbContext>>();
builder.Services.AddHostedService<StockReservedConsumer>();

var app = builder.Build();
app.Run();
