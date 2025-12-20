using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
	public DbSet<Payment> Payments => Set<Payment>();
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

	public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
		: base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Payment>()
			.HasKey(x => x.OrderId);
	}
}