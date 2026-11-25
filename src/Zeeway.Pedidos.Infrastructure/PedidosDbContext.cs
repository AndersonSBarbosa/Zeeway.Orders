using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
        var statusConverter = new ValueConverter<PedidoStatus, string>(
            status => StatusToStore(status),
            status => StatusFromStore(status));

        var pedido = modelBuilder.Entity<Pedido>();
        pedido.ToTable("orders");
        pedido.HasKey(x => x.Id);
        pedido.Property(x => x.EmailCliente).HasColumnName("CustomerEmail").HasMaxLength(320).IsRequired();
        pedido.Property(x => x.ValorTotal).HasColumnName("TotalAmount").HasPrecision(18, 2).IsRequired();
        pedido.Property(x => x.Status).HasConversion(statusConverter).HasMaxLength(16).IsRequired();
        pedido.Property(x => x.PagamentoId).HasColumnName("PaymentId").HasMaxLength(128);
        pedido.Property(x => x.DataCreated).HasColumnName("CreatedAt");
        pedido.Property(x => x.DataUpdated).HasColumnName("UpdatedAt");
        pedido.HasMany(x => x.Items).WithOne()
            .HasForeignKey("PedidoId")
            .HasConstraintName("FK_order_items_orders_OrderId")
            .OnDelete(DeleteBehavior.Cascade);
        pedido.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        var item = modelBuilder.Entity<PedidoItem>();
        item.ToTable("order_items");
        item.HasKey(x => x.Id);
        item.Property(x => x.Nome).HasColumnName("Name").HasMaxLength(200).IsRequired();
        item.Property(x => x.Quantidade).HasColumnName("Quantity");
        item.Property(x => x.PrecoUnitario).HasColumnName("UnitPrice").HasPrecision(18, 2).IsRequired();
        item.Property<Guid>("PedidoId").HasColumnName("OrderId").ValueGeneratedNever();
        item.HasIndex("PedidoId").HasDatabaseName("IX_order_items_OrderId");

        var idem = modelBuilder.Entity<IdempotencyRecord>();
        idem.ToTable("idempotency_records");
        idem.HasKey(x => x.Key);
        idem.Property(x => x.Key).HasMaxLength(200);
        idem.Property(x => x.PedidoId).HasColumnName("OrderId");
        idem.Property(x => x.DataCreated).HasColumnName("CreatedAt");
        idem.Ignore(x => x.DataUpdated);
        idem.HasIndex(x => x.PedidoId)
            .HasDatabaseName("IX_idempotency_records_OrderId")
            .IsUnique();

        var evt = modelBuilder.Entity<PagamentoEventRecord>();
        evt.ToTable("payment_events");
        evt.HasKey(x => x.EventoId).HasName("PK_payment_events");
        evt.Property(x => x.EventoId).HasColumnName("EventId").HasMaxLength(200).IsRequired();
        evt.Property(x => x.PedidoId).HasColumnName("OrderId");
        evt.Property(x => x.Tipo).HasColumnName("Type").HasMaxLength(64).IsRequired();
        evt.Property(x => x.DataRecebimento).HasColumnName("ReceivedAt");
        evt.HasIndex(x => x.EventoId)
            .HasDatabaseName("IX_payment_events_EventId")
            .IsUnique();
    }

    private static string StatusToStore(PedidoStatus status) => status switch
    {
        PedidoStatus.Pendente => "Pending",
        PedidoStatus.Pago => "Paid",
        PedidoStatus.Falha => "Failed",
        _ => throw new InvalidOperationException($"Unknown pedido status: {status}.")
    };

    private static PedidoStatus StatusFromStore(string status) => status switch
    {
        "Pending" => PedidoStatus.Pendente,
        "Paid" => PedidoStatus.Pago,
        "Failed" => PedidoStatus.Falha,
        _ => throw new InvalidOperationException($"Unknown stored pedido status: {status}.")
    };
}
