using OrderService.Domain;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
	public DbSet<Order> Orders => Set<Order>();
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

	public OrderDbContext(DbContextOptions<OrderDbContext> options)
		: base(options) { }
}

