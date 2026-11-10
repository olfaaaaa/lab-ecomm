using ECommerce.Ordering.Api.Data;
using ECommerce.Ordering.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Api.Endpoints;

public static class PromoCodeEndpoints
{
    public static RouteGroupBuilder MapPromoCodeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/promo-codes").WithTags("Promo codes");

        // Lets a client check a code before ordering (e.g. to preview the discount).
        // Unknown, inactive and expired codes all answer 404 so codes cannot be enumerated by reason.
        group.MapGet("/{code}", async (string code, OrderingDbContext db, CancellationToken ct) =>
        {
            var normalized = PromoCode.Normalize(code);
            var promo = await db.PromoCodes.AsNoTracking().FirstOrDefaultAsync(p => p.Code == normalized, ct);

            return promo is not null && promo.IsUsableAt(DateTimeOffset.UtcNow)
                ? Results.Ok(new PromoCodeResponse(promo.Code, promo.PercentOff))
                : Results.NotFound();
        })
        .WithName("GetPromoCode");

        return group;
    }
}

public record PromoCodeResponse(string Code, int PercentOff);
