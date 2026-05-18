namespace VehiclePartsBackend.Dtos.Staff;

public class StaffCustomerSearchResult
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<string> VehicleNumbers { get; set; } = [];
}
