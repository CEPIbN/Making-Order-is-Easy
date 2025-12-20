using InventoryService.Application.Services;
using InventoryService.Infrastructure.Consumers;
using InventoryService.Infrastructure.Messaging;
using InventoryService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Serilog;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Logging --------------------
builder.Host.UseSerilog((ctx, lc) =>
{
	lc.ReadFrom.Configuration(ctx.Configuration)
	  .WriteTo.Console();
});

// -------------------- Database --------------------
builder.Services.AddDbContext<InventoryDbContext>(options =>
{
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("Postgres"));
});

// -------------------- Application --------------------
builder.Services.AddScoped<InventoryServiceType>();

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
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<OrderCreatedConsumer>();

var app = builder.Build();
app.Run();