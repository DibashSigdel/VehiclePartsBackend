namespace VehiclePartsBackend.Models;

public class PartCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ICollection<Part> Parts { get; set; } = new List<Part>();
}
