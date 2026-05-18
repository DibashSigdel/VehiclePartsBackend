namespace VehiclePartsBackend.Constants;

public static class LoyaltyRules
{
    public const decimal MinimumSubTotal = 5000m;
    public const decimal DiscountRate = 0.10m;

    public static decimal CalculateLoyaltyDiscount(decimal subTotal)
    {
        if (subTotal > MinimumSubTotal)
        {
            return Math.Round(subTotal * DiscountRate, 2, MidpointRounding.AwayFromZero);
        }

        return 0m;
    }
}