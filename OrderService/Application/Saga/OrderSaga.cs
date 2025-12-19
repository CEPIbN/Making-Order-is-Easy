using OrderService.Infrastructure.Persistence;

namespace OrderService.Application.Saga;

public class OrderSaga
{
	private readonly OrderDbContext _db;

	public OrderSaga(OrderDbContext db)
	{
		_db = db;
	}

	public async Task HandleStockReserved(Guid orderId)
	{
		var order = await _db.Orders.FindAsync(orderId);
		order!.MarkReserved();
		await _db.SaveChangesAsync();
	}

	public async Task HandlePaymentSucceeded(Guid orderId)
	{
		var order = await _db.Orders.FindAsync(orderId);
		order!.MarkPaid();
		order.Complete();
		await _db.SaveChangesAsync();
	}

	public async Task HandleFailure(Guid orderId)
	{
		var order = await _db.Orders.FindAsync(orderId);
		order!.Cancel();
		await _db.SaveChangesAsync();
	}
}
