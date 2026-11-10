namespace ECommerce.Ordering.Api.Models;

public class PromoCode
{
    /// <summary>Normalized (trimmed, upper-case) code; also the primary key.</summary>
    public required string Code { get; set; }

    /// <summary>Discount applied to the order subtotal, from 1 to 100.</summary>
    public int PercentOff { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsUsableAt(DateTimeOffset now) => IsActive && (ExpiresAt is null || ExpiresAt > now);

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();
}
