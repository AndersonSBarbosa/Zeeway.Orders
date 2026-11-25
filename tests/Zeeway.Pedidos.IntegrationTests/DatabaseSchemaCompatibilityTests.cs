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
        Assert.Equal("CustomerEmail", pedido.FindProperty(nameof(Pedido.EmailCliente))!.GetColumnName(pedidoTable));
        Assert.Equal("TotalAmount", pedido.FindProperty(nameof(Pedido.ValorTotal))!.GetColumnName(pedidoTable));
        Assert.Equal("CreatedAt", pedido.FindProperty(nameof(Pedido.DataCreated))!.GetColumnName(pedidoTable));
        Assert.Equal("UpdatedAt", pedido.FindProperty(nameof(Pedido.DataUpdated))!.GetColumnName(pedidoTable));
        var statusConverter = pedido.FindProperty(nameof(Pedido.Status))!.GetValueConverter()!;
        Assert.Equal("Pending", statusConverter.ConvertToProvider(PedidoStatus.Pendente));
        Assert.Equal(PedidoStatus.Pago, statusConverter.ConvertFromProvider("Paid"));

        var idempotencyTable = StoreObjectIdentifier.Table("idempotency_records", null);
        var idempotency = model.FindEntityType(typeof(IdempotencyRecord))!;
        Assert.Equal("OrderId", idempotency.FindProperty(nameof(IdempotencyRecord.PedidoId))!.GetColumnName(idempotencyTable));
        Assert.Equal("CreatedAt", idempotency.FindProperty(nameof(IdempotencyRecord.DataCreated))!.GetColumnName(idempotencyTable));
        Assert.Contains(idempotency.GetIndexes(), index => index.GetDatabaseName() == "IX_idempotency_records_OrderId");

        var itemTable = StoreObjectIdentifier.Table("order_items", null);
        var item = model.FindEntityType(typeof(PedidoItem))!;
        Assert.Equal("OrderId", item.FindProperty("PedidoId")!.GetColumnName(itemTable));
        Assert.Equal("Name", item.FindProperty(nameof(PedidoItem.Nome))!.GetColumnName(itemTable));
        Assert.Equal("Quantity", item.FindProperty(nameof(PedidoItem.Quantidade))!.GetColumnName(itemTable));
        Assert.Equal("UnitPrice", item.FindProperty(nameof(PedidoItem.PrecoUnitario))!.GetColumnName(itemTable));
        Assert.Contains(item.GetIndexes(), index => index.GetDatabaseName() == "IX_order_items_OrderId");
        Assert.Equal("FK_order_items_orders_OrderId", item.GetForeignKeys().Single().GetConstraintName());

        var eventoPagamentoTable = StoreObjectIdentifier.Table("payment_events", null);
        var eventoPagamento = model.FindEntityType(typeof(PagamentoEventRecord))!;
        Assert.Equal("OrderId", eventoPagamento.FindProperty(nameof(PagamentoEventRecord.PedidoId))!.GetColumnName(eventoPagamentoTable));
        Assert.Equal("EventId", eventoPagamento.FindProperty(nameof(PagamentoEventRecord.EventoId))!.GetColumnName(eventoPagamentoTable));
        Assert.Equal("Type", eventoPagamento.FindProperty(nameof(PagamentoEventRecord.Tipo))!.GetColumnName(eventoPagamentoTable));
        Assert.Equal("ReceivedAt", eventoPagamento.FindProperty(nameof(PagamentoEventRecord.DataRecebimento))!.GetColumnName(eventoPagamentoTable));
    }
}
