namespace VehiclePartsBackend.Models;

public class Review
{
    public int ReviewId { get; set; }
    public int CustomerId { get; set; }
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

    public User? Customer { get; set; }
    public Appointment? Appointment { get; set; }
}
