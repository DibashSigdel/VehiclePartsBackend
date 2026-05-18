namespace VehiclePartsBackend.Dtos.Staff;

public class StaffCreateSalesInvoiceRequest
{
    public int CustomerId { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string PaymentType { get; set; } = "Cash";
    public string PaymentStatus { get; set; } = "Paid";
    public DateTime? CreditDueDate { get; set; }
    public decimal DiscountAmount { get; set; }
    public List<StaffSalesInvoiceLineRequest> Items { get; set; } = [];
}

public class StaffSalesInvoiceLineRequest
{
    public int PartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class StaffSalesInvoiceResponse
{
    public int SalesInvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public bool LoyaltyDiscountApplied { get; set; }
    public decimal LoyaltyDiscountAmount { get; set; }
}

public class StaffSalesInvoiceListItemResponse
{
    public int SalesInvoiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
}

public class StaffCustomerOptionResponse
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class StaffPartSaleOptionResponse
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public int QuantityOnHand { get; set; }
}