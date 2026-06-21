using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence;

public class OrderManagementDbContext : DbContext
{
    public OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLineItem> OrderLineItems => Set<OrderLineItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer Configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(2);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Order Configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
            entity.Property(e => e.TotalAmount).HasPrecision(19, 2);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
        });

        // OrderLineItem Configuration
        modelBuilder.Entity<OrderLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductSku).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasPrecision(19, 2);
            entity.HasOne(e => e.Order)
                .WithMany(o => o.LineItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OrderId);
        });

        // OutboxMessage Configuration
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).IsRequired();
            entity.HasIndex(e => e.Processed);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
