namespace VehiclePartsBackend.Models;

public class Stock
{
    public int PartId { get; set; }
    public int QuantityOnHand { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public Part? Part { get; set; }
}
