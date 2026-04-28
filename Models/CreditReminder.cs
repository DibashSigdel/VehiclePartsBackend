namespace VehiclePartsBackend.Models;

public class CreditReminder
{
    public int ReminderId { get; set; }
    public int SalesInvoiceId { get; set; }
    public int CustomerId { get; set; }
    public decimal AmountDue { get; set; }
    public DateTime ReminderSentAt { get; set; } = DateTime.UtcNow;

    public SalesInvoice? SalesInvoice { get; set; }
    public User? Customer { get; set; }
}
