using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Commands;
using OrderService.Infrastructure.Persistence;

namespace OrderService.API.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
	private readonly OrderDbContext _db;

	public OrdersController(OrderDbContext db)
	{
		_db = db;
	}

	[HttpPost]
	public async Task<IActionResult> CreateOrder(
		[FromBody] CreateOrderRequest request)
	{
		var command = new CreateOrderCommand(_db);

		var order = await command.ExecuteAsync(
			request.ProductId,
			request.Quantity,
			request.Price);

		return Accepted(new { OrderId = order.Item1, Status = order.Item2 });
	}

	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetOrder(Guid id)
	{
		var order = await _db.Orders
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id);

		if (order == null)
			return NotFound();

		return Ok(new
		{
			order.Id,
			order.Status,
			order.ProductId,
			order.Quantity,
			order.Price
		});
	}
}

// DTO
public record CreateOrderRequest(
	Guid ProductId,
	int Quantity,
	decimal Price);

