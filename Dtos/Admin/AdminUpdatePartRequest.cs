namespace VehiclePartsBackend.Dtos.Admin;

public class AdminUpdatePartRequest
{
    public int CategoryId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; }
    public int QuantityOnHand { get; set; }
}
