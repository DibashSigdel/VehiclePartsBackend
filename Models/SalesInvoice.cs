namespace VehiclePartsBackend.Models;

public class SalesInvoice
{
    public int SalesInvoiceId { get; set; }
    public int CustomerId { get; set; }
    public int CreatedByStaffId { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public string PaymentType { get; set; } = "Cash";
    public string PaymentStatus { get; set; } = "Paid";
    public DateTime? CreditDueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public User? Customer { get; set; }
    public User? CreatedByStaff { get; set; }
    public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
    public ICollection<CreditReminder> CreditReminders { get; set; } = new List<CreditReminder>();
}
