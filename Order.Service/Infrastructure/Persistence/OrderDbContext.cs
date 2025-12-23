using OrderService.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.OutBox;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext, IOutboxDbContext
{
	public DbSet<OrderItem> Orders => Set<OrderItem>();
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

	public OrderDbContext(DbContextOptions<OrderDbContext> options)
		: base(options) { }
}

