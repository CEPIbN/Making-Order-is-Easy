using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.OutBox;

public interface IOutboxDbContext
{
	DbSet<OutboxMessage> OutboxMessages { get; }
	Task<int> SaveChangesAsync(CancellationToken ct);
}

