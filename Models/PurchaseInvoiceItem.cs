namespace VehiclePartsBackend.Models;

public class PurchaseInvoiceItem
{
    public int PurchaseItemId { get; set; }
    public int PurchaseInvoiceId { get; set; }
    public int PartId { get; set; }
    public decimal CostPrice { get; set; }
    public int QuantityBought { get; set; }
    public decimal LineTotal { get; set; }

    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public Part? Part { get; set; }
}
