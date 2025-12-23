using Notification.Service.Infrastructure.Consumers;
using Prometheus;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Logging --------------------
builder.Host.UseSerilog((ctx, lc) =>
{
	lc.ReadFrom.Configuration(ctx.Configuration)
	  .WriteTo.Console();
});

// -------------------- Фабрика подключений к RabbitMQ --------------------
builder.Services.AddSingleton<IConnectionFactory>(_ =>
	new ConnectionFactory
	{
		HostName = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq",
		AutomaticRecoveryEnabled = true,
		NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
	});

// -------------------- Consumer --------------------
builder.Services.AddHostedService<OrderNotificationsConsumer>();

var app = builder.Build();

app.UseRouting();

// endpoint /metrics
app.UseEndpoints(endpoints =>
{
	endpoints.MapMetrics();
});

app.Run();
