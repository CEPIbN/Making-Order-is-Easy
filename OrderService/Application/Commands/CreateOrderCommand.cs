using OrderService.Domain;
using OrderService.Infrastructure.Persistence;
using Shared.Contracts.Events;
using Shared.OutBox;
using System.Text.Json;

namespace OrderService.Application.Commands;

public class CreateOrderCommand
{
	private readonly OrderDbContext _db;

	public CreateOrderCommand(OrderDbContext db)
	{
		_db = db;
	}

	/// <summary>
	///Метод создания объектов заказа и сообщения события с записью в БД
	/// </summary>
	public async Task<Tuple<Guid, OrderStatus>> ExecuteAsync(Guid productId, int quantity, decimal price)
	{
		var order = new Order(productId, quantity, price);

		var orderCreatedEvent = new OrderCreated(
			order.Id,
			productId,
			quantity,
			price);

		var outbox = new OutboxMessage
		{
			Id = Guid.NewGuid(),
			Type = nameof(OrderCreated),
			Payload = JsonSerializer.Serialize(orderCreatedEvent),
			OccurredAt = DateTime.UtcNow
		};

		_db.Orders.Add(order);
		_db.OutboxMessages.Add(outbox);

		// Записываем order и событие создания заказа в одной транзакции
		await _db.SaveChangesAsync();

		return new Tuple<Guid, OrderStatus>(order.Id, order.Status);
	}
}

