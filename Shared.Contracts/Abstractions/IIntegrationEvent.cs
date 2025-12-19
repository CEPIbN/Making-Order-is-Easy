using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Abstractions
{
	public interface IIntegrationEvent
	{
		Guid EventId { get; }
		DateTime OccurredAt { get; }
	}
}
