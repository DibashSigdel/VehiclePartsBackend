namespace VehiclePartsBackend.Dtos.Admin;

public class AdminCreateVendorRequest
{
    public string VendorName { get; set; } = string.Empty;
    public string VendorPhone { get; set; } = string.Empty;
    public string VendorEmail { get; set; } = string.Empty;
    public string VendorAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
