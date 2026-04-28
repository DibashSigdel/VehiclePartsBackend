namespace VehiclePartsBackend.Dtos.Admin;

public class AdminVendorResponse
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string VendorPhone { get; set; } = string.Empty;
    public string VendorEmail { get; set; } = string.Empty;
    public string VendorAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
