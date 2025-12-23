using Microsoft.EntityFrameworkCore;
using OrderService.Application.Saga;
using OrderService.Infrastructure.Consumers;
using OrderService.Infrastructure.Persistence;
using Prometheus;
using RabbitMQ.Client;
using Serilog;
using Shared.OutBox;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
	lc.ReadFrom.Configuration(ctx.Configuration)
	  .WriteTo.Console();
});

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

// -------------------- Фабрика подключений к RabbitMQ --------------------
builder.Services.AddSingleton<IConnectionFactory>(_ =>
	new ConnectionFactory
	{
		HostName = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq",
		AutomaticRecoveryEnabled = true,
		NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
	});

// -------------------- Outbox Publisher --------------------
builder.Services.AddHostedService<OutboxPublisher<OrderDbContext>>();

// -------------------- Consumers --------------------
builder.Services.AddHostedService<StockReservedConsumer>();
builder.Services.AddHostedService<PaymentResultConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
	db.Database.Migrate();
}

app.UseRouting();

// endpoint /metrics
app.UseEndpoints(endpoints =>
{
	endpoints.MapMetrics();
});

app.MapControllers();
app.Run();