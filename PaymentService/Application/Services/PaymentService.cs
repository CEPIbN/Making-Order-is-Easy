using PaymentService.Domain;
using PaymentService.Infrastructure.Persistence;
using Shared.Contracts.Events;
using Shared.OutBox;
using System.Text.Json;

namespace PaymentService.Application.Services;

public class PaymentServiceType
{
	private readonly PaymentDbContext _db;
	private readonly Random _random = new();

	public PaymentServiceType(PaymentDbContext db)
	{
		_db = db;
	}

	public async Task HandleStockReserved(
		Guid orderId,
		decimal amount)
	{
		// Идемпотентность
		if (await _db.Payments.FindAsync(orderId) != null)
			return;

		// Эмуляция списания (80% успеха)
		var success = _random.Next(1, 100) <= 80;

		var payment = new Payment(orderId, amount, success);
		_db.Payments.Add(payment);

		if (success)
		{
			await AddToOutbox(new PaymentSucceeded(orderId, amount));
		}
		else
		{
			await AddToOutbox(new PaymentFailed(orderId, amount, "Insufficient funds"));
		}

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
