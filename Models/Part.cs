namespace VehiclePartsBackend.Models;

public class Part
{
    public int PartId { get; set; }
    public int CategoryId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public bool IsActive { get; set; } = true;

    public PartCategory? Category { get; set; }
    public Stock? Stock { get; set; }
}
