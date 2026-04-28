namespace VehiclePartsBackend.Dtos.Admin;

public class AdminCreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
    public string Address { get; set; } = string.Empty;
}
