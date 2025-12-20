using InventoryService.Domain;
using InventoryService.Infrastructure.Persistence;
using Shared.Contracts.Events;
using Shared.OutBox;
using System.Text.Json;

namespace InventoryService.Application.Services;

public class InventoryServiceType
{
	private readonly InventoryDbContext _db;

	public InventoryServiceType(InventoryDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Добавление сообщения события резервирования в OutBox, Обновление доступного кол-ва товара в Inventory и создание записи о резервировании товара.
	/// </summary>
	/// <param name="orderId"></param>
	/// <param name="productId"></param>
	/// <param name="quantity"></param>
	/// <returns></returns>
	public async Task HandleOrderCreated(
		Guid orderId,
		Guid productId,
		int quantity,
		decimal price)
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

		var evt = new StockReserved(orderId, productId, quantity, price);

		await AddToOutbox(evt);
		await _db.SaveChangesAsync();
	}

	/// <summary>
	/// Сохраняет запись о неудачном резервировании в БД
	/// </summary>
	/// <param name="orderId"></param>
	/// <param name="productId"></param>
	/// <param name="quantity"></param>
	/// <param name="reason"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Добавление записи "StockFailed" в OutBox
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="evt"></param>
	/// <returns></returns>
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
