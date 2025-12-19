namespace OrderService.Domain;

public class Order
{
	public Guid Id { get; private set; }
	public Guid ProductId { get; private set; }
	public int Quantity { get; private set; }
	public decimal Price { get; private set; }
	public OrderStatus Status { get; private set; }

	private Order() { }

	public Order(Guid productId, int quantity, decimal price)
	{
		Id = Guid.NewGuid();
		ProductId = productId;
		Quantity = quantity;
		Price = price;
		Status = OrderStatus.Pending;
	}

	public void MarkReserved() => Status = OrderStatus.Reserved;
	public void MarkPaid() => Status = OrderStatus.Paid;
	public void Complete() => Status = OrderStatus.Completed;
	public void Cancel() => Status = OrderStatus.Cancelled;
}

