using OrderService.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.OutBox;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext, IOutboxDbContext
{
	public DbSet<Order> Orders => Set<Order>();
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

	public OrderDbContext(DbContextOptions<OrderDbContext> options)
		: base(options) { }
}

