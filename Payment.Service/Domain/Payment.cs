namespace PaymentService.Domain;

public class PaymentItem
{
	public Guid OrderId { get; private set; }
	public decimal Amount { get; private set; }
	public bool Succeeded { get; private set; }
	public DateTime ProcessedAt { get; private set; }

	private PaymentItem() { }

	public PaymentItem(Guid orderId, decimal amount, bool succeeded)
	{
		OrderId = orderId;
		Amount = amount;
		Succeeded = succeeded;
		ProcessedAt = DateTime.UtcNow;
	}
}