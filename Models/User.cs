namespace VehiclePartsBackend.Models;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<SalesInvoice> CustomerSalesInvoices { get; set; } = new List<SalesInvoice>();
    public ICollection<SalesInvoice> StaffCreatedSalesInvoices { get; set; } = new List<SalesInvoice>();
    public ICollection<PurchaseInvoice> CreatedPurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<CreditReminder> CreditReminders { get; set; } = new List<CreditReminder>();
}
