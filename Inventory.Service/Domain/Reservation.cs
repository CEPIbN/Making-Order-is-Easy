namespace InventoryService.Domain;

/// <summary>
/// Объект зарезервированного заказа
/// </summary>
public class Reservation
{
	public Guid OrderId { get; private set; }
	public Guid ProductId { get; private set; }
	public int Quantity { get; private set; }
	public DateTime ReservedAt { get; private set; }

	private Reservation() { }

	public Reservation(Guid orderId, Guid productId, int quantity)
	{
		OrderId = orderId;
		ProductId = productId;
		Quantity = quantity;
		ReservedAt = DateTime.UtcNow;
	}
}
