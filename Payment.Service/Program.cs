using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Services;
using PaymentService.Infrastructure.Consumers;
using PaymentService.Infrastructure.Persistence;
using Prometheus;
using RabbitMQ.Client;
using Serilog;
using Shared.OutBox;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Serilog --------------------
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

// -------------------- Фабрика подключений к RabbitMQ --------------------
builder.Services.AddSingleton<IConnectionFactory>(_ =>
	new ConnectionFactory
	{
		HostName = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq",
		AutomaticRecoveryEnabled = true,
		NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
	});

// -------------------- Messaging --------------------
builder.Services.AddHostedService<OutboxPublisher<PaymentDbContext>>();

// -------------------- Consumers --------------------
builder.Services.AddHostedService<StockReservedConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
	db.Database.Migrate();
}

app.UseRouting();

// endpoint /metrics
app.UseEndpoints(endpoints =>
{
	endpoints.MapMetrics();
});

app.Run();
