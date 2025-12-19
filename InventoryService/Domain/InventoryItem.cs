namespace InventoryService.Domain;

public class InventoryItem
{
	public Guid ProductId { get; private set; }
	public int AvailableQuantity { get; private set; }

	private InventoryItem() { }

	public InventoryItem(Guid productId, int quantity)
	{
		ProductId = productId;
		AvailableQuantity = quantity;
	}

	public bool CanReserve(int quantity) =>
		AvailableQuantity >= quantity;

	public void Reserve(int quantity)
	{
		if (!CanReserve(quantity))
			throw new InvalidOperationException("Not enough stock");

		AvailableQuantity -= quantity;
	}

	public void Release(int quantity)
	{
		AvailableQuantity += quantity;
	}
}
