using Microsoft.EntityFrameworkCore;
using Zeeway.Pedidos.Domain;

namespace Zeeway.Pedidos.Infrastructure;

public sealed class PedidosDbContext : DbContext
{
    public PedidosDbContext(DbContextOptions<PedidosDbContext> options) : base(options) { }

    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> ItensPedido => Set<PedidoItem>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<PagamentoEventRecord> EventosPagamento => Set<PagamentoEventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var pedido = modelBuilder.Entity<Pedido>();
        pedido.ToTable("orders");
        pedido.HasKey(x => x.Id);
        pedido.Property(x => x.CustomerEmail).HasMaxLength(320).IsRequired();
        pedido.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        pedido.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        pedido.Property(x => x.PagamentoId).HasColumnName("PaymentId").HasMaxLength(128);
        pedido.HasMany(x => x.Items).WithOne()
            .HasForeignKey("PedidoId")
            .HasConstraintName("FK_order_items_orders_OrderId")
            .OnDelete(DeleteBehavior.Cascade);
        pedido.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        var item = modelBuilder.Entity<PedidoItem>();
        item.ToTable("order_items");
        item.HasKey(x => x.Id);
        item.Property(x => x.Name).HasMaxLength(200).IsRequired();
        item.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
        item.Property<Guid>("PedidoId").HasColumnName("OrderId").ValueGeneratedNever();
        item.HasIndex("PedidoId").HasDatabaseName("IX_order_items_OrderId");

        var idem = modelBuilder.Entity<IdempotencyRecord>();
        idem.ToTable("idempotency_records");
        idem.HasKey(x => x.Key);
        idem.Property(x => x.Key).HasMaxLength(200);
        idem.Property(x => x.PedidoId).HasColumnName("OrderId");
        idem.HasIndex(x => x.PedidoId)
            .HasDatabaseName("IX_idempotency_records_OrderId")
            .IsUnique();

        var evt = modelBuilder.Entity<PagamentoEventRecord>();
        evt.ToTable("payment_events");
        evt.HasKey(x => x.EventId);
        evt.Property(x => x.PedidoId).HasColumnName("OrderId");
        evt.Property(x => x.EventId).HasMaxLength(200);
        evt.Property(x => x.Type).HasMaxLength(64).IsRequired();
        evt.HasIndex(x => x.EventId).IsUnique();
    }
}
