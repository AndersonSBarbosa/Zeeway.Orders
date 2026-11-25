using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Zeeway.Pedidos.Domain;
using Zeeway.Pedidos.Infrastructure;

namespace Zeeway.Pedidos.IntegrationTests;

public sealed class DatabaseSchemaCompatibilityTests
{
    [Fact]
    public void Renamed_entities_keep_existing_table_and_column_names()
    {
        var options = new DbContextOptionsBuilder<PedidosDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
        using var context = new PedidosDbContext(options);
        var model = context.Model;

        var pedidoTable = StoreObjectIdentifier.Table("orders", null);
        var pedido = model.FindEntityType(typeof(Pedido))!;
        Assert.Equal("orders", pedido.GetTableName());
        Assert.Equal("PaymentId", pedido.FindProperty(nameof(Pedido.PagamentoId))!.GetColumnName(pedidoTable));

        var idempotencyTable = StoreObjectIdentifier.Table("idempotency_records", null);
        var idempotency = model.FindEntityType(typeof(IdempotencyRecord))!;
        Assert.Equal("OrderId", idempotency.FindProperty(nameof(IdempotencyRecord.PedidoId))!.GetColumnName(idempotencyTable));
        Assert.Contains(idempotency.GetIndexes(), index => index.GetDatabaseName() == "IX_idempotency_records_OrderId");

        var itemTable = StoreObjectIdentifier.Table("order_items", null);
        var item = model.FindEntityType(typeof(PedidoItem))!;
        Assert.Equal("OrderId", item.FindProperty("PedidoId")!.GetColumnName(itemTable));
        Assert.Contains(item.GetIndexes(), index => index.GetDatabaseName() == "IX_order_items_OrderId");
        Assert.Equal("FK_order_items_orders_OrderId", item.GetForeignKeys().Single().GetConstraintName());

        var eventoPagamentoTable = StoreObjectIdentifier.Table("payment_events", null);
        var eventoPagamento = model.FindEntityType(typeof(PagamentoEventRecord))!;
        Assert.Equal("OrderId", eventoPagamento.FindProperty(nameof(PagamentoEventRecord.PedidoId))!.GetColumnName(eventoPagamentoTable));
    }
}
