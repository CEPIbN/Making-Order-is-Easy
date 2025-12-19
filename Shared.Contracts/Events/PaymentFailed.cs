using Shared.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events;

public record PaymentFailed(
	Guid OrderId,
	decimal Amount,
	string Reason
) : IIntegrationEvent
{
	public Guid EventId { get; } = Guid.NewGuid();
	public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
