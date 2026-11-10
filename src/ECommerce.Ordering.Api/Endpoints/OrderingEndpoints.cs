using ECommerce.Ordering.Api.Data;
using ECommerce.Ordering.Api.Models;
using ECommerce.Ordering.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Api.Endpoints;

public static class OrderingEndpoints
{
    public static RouteGroupBuilder MapOrderingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/orders").WithTags("Ordering");

        group.MapGet("/", async (OrderingDbContext db) =>
            await db.Orders.AsNoTracking().Include(o => o.Items).ToListAsync())
            .WithName("GetOrders");

        group.MapGet("/{id:int}", async (int id, OrderingDbContext db) =>
            await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id) is { } order
                ? Results.Ok(order)
                : Results.NotFound())
            .WithName("GetOrderById");

        group.MapPost("/", async (
            CreateOrderRequest request,
            OrderingDbContext db,
            CatalogServiceClient catalog,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Customer))
            {
                return Results.BadRequest("Customer name is required.");
            }

            if (request.Items is null || request.Items.Count == 0)
            {
                return Results.BadRequest("An order must contain at least one item.");
            }

            var invalidLine = request.Items.FirstOrDefault(i => i.Quantity <= 0);
            if (invalidLine is not null)
            {
                return Results.BadRequest($"Quantity for product {invalidLine.ProductId} must be greater than zero.");
            }

            var order = new Order
            {
                Customer = request.Customer,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = OrderStatus.Pending
            };

            if (!string.IsNullOrWhiteSpace(request.PromoCode))
            {
                var code = PromoCode.Normalize(request.PromoCode);
                var promo = await db.PromoCodes.AsNoTracking().FirstOrDefaultAsync(p => p.Code == code, ct);
                if (promo is null || !promo.IsUsableAt(order.CreatedAt))
                {
                    return Results.BadRequest($"Promo code '{code}' is invalid or expired.");
                }

                // Snapshot the percentage so later changes to the code don't alter past orders.
                order.PromoCode = promo.Code;
                order.DiscountPercent = promo.PercentOff;
            }

            // Validate each product against the Catalog service (sync HTTP call).
            foreach (var item in request.Items)
            {
                var product = await catalog.GetProductAsync(item.ProductId, ct);
                if (product is null)
                {
                    return Results.BadRequest($"Product {item.ProductId} does not exist.");
                }

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity
                });
            }

            order.Status = OrderStatus.Confirmed;
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);

            return Results.CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
        })
        .WithName("CreateOrder");

        return group;
    }
}

public record CreateOrderRequest(string Customer, List<OrderLine> Items, string? PromoCode = null);
public record OrderLine(int ProductId, int Quantity);
