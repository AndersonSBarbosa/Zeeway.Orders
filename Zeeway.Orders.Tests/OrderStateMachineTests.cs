using Zeeway.Orders.Domain;

namespace Zeeway.Orders.Tests;

public sealed class OrderStateMachineTests
{
    [Fact]
    public void Pending_can_transition_to_paid()
    {
        var order = CreateOrder();
        Assert.True(order.ApplyPaymentStatus(OrderStatus.Paid));
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void Pending_can_transition_to_failed()
    {
        var order = CreateOrder();
        Assert.True(order.ApplyPaymentStatus(OrderStatus.Failed));
        Assert.Equal(OrderStatus.Failed, order.Status);
    }

    [Fact]
    public void Final_status_cannot_change()
    {
        var order = CreateOrder();
        order.ApplyPaymentStatus(OrderStatus.Paid);
        Assert.Throws<InvalidOperationException>(() => order.ApplyPaymentStatus(OrderStatus.Failed));
    }

    [Fact]
    public void Duplicate_final_event_is_ignored()
    {
        var order = CreateOrder();
        order.ApplyPaymentStatus(OrderStatus.Paid);
        Assert.False(order.ApplyPaymentStatus(OrderStatus.Paid));
    }

    [Fact]
    public void Idempotency_record_binds_key_to_one_order()
    {
        var orderId = Guid.NewGuid();
        var record = new IdempotencyRecord("key-123", orderId);
        Assert.Equal("key-123", record.Key);
        Assert.Equal(orderId, record.OrderId);
    }

    private static Order CreateOrder() =>
        new("buyer@example.com", new[] { new OrderItem("Coffee", 2, 12.50m) });
}
