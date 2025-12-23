using InventoryService.Application.Services;
using InventoryService.Infrastructure.Consumers;
using InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Prometheus;
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
builder.Services.AddDbContext<InventoryDbContext>(options =>
{
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("Postgres"));
});

// -------------------- Application --------------------
builder.Services.AddScoped<InventoryServiceType>();

// -------------------- Фабрика подключений к RabbitMQ --------------------
builder.Services.AddSingleton<IConnectionFactory>(_ =>
	new ConnectionFactory
	{
		HostName = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq",
		AutomaticRecoveryEnabled = true,
		NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
	});

// -------------------- Messaging --------------------
builder.Services.AddHostedService<OutboxPublisher<InventoryDbContext>>();

// -------------------- Consumer --------------------
builder.Services.AddHostedService<OrderCreatedConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
	db.Database.Migrate();
}

app.UseRouting();

// endpoint /metrics
app.UseEndpoints(endpoints =>
{
	endpoints.MapMetrics();
});

app.Run();