using Microsoft.EntityFrameworkCore;
using Zeeway.Orders.Domain;

namespace Zeeway.Orders.Infrastructure;

public sealed class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<PaymentEventRecord> PaymentEvents => Set<PaymentEventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();
        order.ToTable("orders");
        order.HasKey(x => x.Id);
        order.Property(x => x.CustomerEmail).HasMaxLength(320).IsRequired();
        order.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        order.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        order.Property(x => x.PaymentId).HasMaxLength(128);
        order.HasMany(x => x.Items).WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
        order.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        var item = modelBuilder.Entity<OrderItem>();
        item.ToTable("order_items");
        item.HasKey(x => x.Id);
        item.Property(x => x.Name).HasMaxLength(200).IsRequired();
        item.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
        item.Property<Guid>("OrderId").ValueGeneratedNever();

        var idem = modelBuilder.Entity<IdempotencyRecord>();
        idem.ToTable("idempotency_records");
        idem.HasKey(x => x.Key);
        idem.Property(x => x.Key).HasMaxLength(200);
        idem.HasIndex(x => x.OrderId).IsUnique();

        var evt = modelBuilder.Entity<PaymentEventRecord>();
        evt.ToTable("payment_events");
        evt.HasKey(x => x.EventId);
        evt.Property(x => x.EventId).HasMaxLength(200);
        evt.Property(x => x.Type).HasMaxLength(64).IsRequired();
        evt.HasIndex(x => x.EventId).IsUnique();
    }
}
