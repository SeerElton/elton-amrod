using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly OrderManagementDbContext _context;

    public OutboxRepository(OrderManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OutboxMessage>> GetUnprocessedAsync()
    {
        return await _context.OutboxMessages
            .Where(m => !m.Processed)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(OutboxMessage message)
    {
        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsProcessedAsync(Guid id)
    {
        var message = await _context.OutboxMessages.FindAsync(id);
        if (message != null)
        {
            message.Processed = true;
            message.ProcessedAt = DateTime.UtcNow;
            _context.OutboxMessages.Update(message);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
