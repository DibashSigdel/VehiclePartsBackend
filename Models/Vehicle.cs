namespace VehiclePartsBackend.Models;

public class Vehicle
{
    public int VehicleId { get; set; }
    public int CustomerId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }

    public User? Customer { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
