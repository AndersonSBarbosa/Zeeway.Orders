namespace Zeeway.Pedidos.Domain
{
    public class Base
    {
        public DateTimeOffset DataCreated { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset DataUpdated { get; set; } = DateTimeOffset.UtcNow;
    }
}
