using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;
using Shared.OutBox;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext, IOutboxDbContext
{
	public DbSet<PaymentItem> Payments => Set<PaymentItem>();
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

	public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
		: base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<PaymentItem>()
			.HasKey(x => x.OrderId);
	}
}