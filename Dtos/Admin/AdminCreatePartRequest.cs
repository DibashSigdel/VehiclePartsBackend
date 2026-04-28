namespace VehiclePartsBackend.Dtos.Admin;

public class AdminCreatePartRequest
{
    public int CategoryId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public int QuantityOnHand { get; set; }
}
