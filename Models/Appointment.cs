namespace VehiclePartsBackend.Models;

public class Appointment
{
    public int AppointmentId { get; set; }
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = "Booked";
    public string ServiceNote { get; set; } = string.Empty;

    public User? Customer { get; set; }
    public Vehicle? Vehicle { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
