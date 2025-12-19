using InventoryService.Domain;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
	public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
	public DbSet<Reservation> Reservations => Set<Reservation>();
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

	public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
		: base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<InventoryItem>()
			.HasKey(x => x.ProductId);

		modelBuilder.Entity<Reservation>()
			.HasKey(x => x.OrderId);
	}
}
