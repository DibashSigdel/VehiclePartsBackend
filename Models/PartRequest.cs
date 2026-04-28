namespace VehiclePartsBackend.Models;

public class PartRequest
{
    public int PartRequestId { get; set; }
    public int CustomerId { get; set; }
    public string RequestedPartName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public string RequestStatus { get; set; } = "Pending";

    public User? Customer { get; set; }
}
