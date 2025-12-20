using Notification.Service.Infrastructure.Consumers;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Logging --------------------
builder.Host.UseSerilog((ctx, lc) =>
{
	lc.ReadFrom.Configuration(ctx.Configuration)
	  .WriteTo.Console();
});

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

// -------------------- Consumer --------------------
builder.Services.AddHostedService<OrderNotificationsConsumer>();

var app = builder.Build();
app.Run();
