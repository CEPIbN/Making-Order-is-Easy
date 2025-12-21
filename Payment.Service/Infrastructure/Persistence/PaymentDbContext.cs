using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;
using Shared.OutBox;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext, IOutboxDbContext
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