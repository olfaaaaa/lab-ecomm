namespace ECommerce.Ordering.Api.Models;

public class Order
{
    public int Id { get; set; }
    public required string Customer { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public List<OrderItem> Items { get; set; } = [];

    /// <summary>Promo code applied to this order, if any (normalized).</summary>
    public string? PromoCode { get; set; }

    /// <summary>Percentage snapshotted from the promo code when the order was placed.</summary>
    public int DiscountPercent { get; set; }

    public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);

    public decimal Discount => Math.Round(Subtotal * DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);

    public decimal Total => Subtotal - Discount;
}

public class OrderItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Cancelled
}
