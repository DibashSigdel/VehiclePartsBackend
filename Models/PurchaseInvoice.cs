namespace VehiclePartsBackend.Models;

public class PurchaseInvoice
{
    public int PurchaseInvoiceId { get; set; }
    public int VendorId { get; set; }
    public int CreatedByAdminId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal TotalCost { get; set; }

    public Vendor? Vendor { get; set; }
    public User? CreatedByAdmin { get; set; }
    public ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
}
