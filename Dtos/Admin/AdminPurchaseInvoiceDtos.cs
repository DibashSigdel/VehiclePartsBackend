namespace VehiclePartsBackend.Dtos.Admin;

public class AdminCreatePurchaseInvoiceRequest
{
    public int VendorId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public List<AdminPurchaseInvoiceLineRequest> Items { get; set; } = [];
}

public class AdminPurchaseInvoiceLineRequest
{
    public int PartId { get; set; }
    public decimal CostPrice { get; set; }
    public int QuantityBought { get; set; }
}

public class AdminPurchaseInvoiceResponse
{
    public int PurchaseInvoiceId { get; set; }
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal TotalCost { get; set; }
}

public class AdminPurchaseInvoiceListItemResponse
{
    public int PurchaseInvoiceId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal TotalCost { get; set; }
    public int LineCount { get; set; }
}
