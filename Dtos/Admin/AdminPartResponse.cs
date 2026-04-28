namespace VehiclePartsBackend.Dtos.Admin;

public class AdminPartResponse
{
    public int PartId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; }
    public int QuantityOnHand { get; set; }
}
