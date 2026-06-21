using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Repositories;

public interface IOutboxRepository
{
    Task<IEnumerable<OutboxMessage>> GetUnprocessedAsync();
    Task AddAsync(OutboxMessage message);
    Task MarkAsProcessedAsync(Guid id);
    Task SaveChangesAsync();
}
