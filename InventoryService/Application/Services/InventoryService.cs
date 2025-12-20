using InventoryService.Domain;
using InventoryService.Infrastructure.Persistence;
using Shared.Contracts.Events;
using System.Text.Json;

namespace InventoryService.Application.Services;

public class InventoryServiceType
{
	private readonly InventoryDbContext _db;

	public InventoryServiceType(InventoryDbContext db)
	{
		_db = db;
	}

	public async Task HandleOrderCreated(
		Guid orderId,
		Guid productId,
		int quantity)
	{
		var item = await _db.Inventory.FindAsync(productId);

		if (item == null || !item.CanReserve(quantity))
		{
			await PublishFailure(orderId, productId, quantity, "Insufficient stock");
			return;
		}

		item.Reserve(quantity);

		var reservation = new Reservation(orderId, productId, quantity);
		_db.Reservations.Add(reservation);

		var evt = new StockReserved(orderId, productId, quantity);

		await AddToOutbox(evt);
		await _db.SaveChangesAsync();
	}

	private async Task PublishFailure(
		Guid orderId,
		Guid productId,
		int quantity,
		string reason)
	{
		var evt = new StockFailed(orderId, productId, quantity, reason);
		await AddToOutbox(evt);
		await _db.SaveChangesAsync();
	}

	private Task AddToOutbox<T>(T evt)
	{
		_db.OutboxMessages.Add(new OutboxMessage
		{
			Id = Guid.NewGuid(),
			Type = typeof(T).Name,
			Payload = JsonSerializer.Serialize(evt),
			OccurredAt = DateTime.UtcNow
		});

		return Task.CompletedTask;
	}
}
