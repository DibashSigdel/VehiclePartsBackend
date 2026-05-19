namespace VehiclePartsBackend.Dtos.Staff;

public class StaffCustomerDetailResponse
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<StaffCustomerVehicleDetail> Vehicles { get; set; } = [];
    public List<StaffCustomerPurchaseHistoryItem> PurchaseHistory { get; set; } = [];
    public List<StaffCustomerServiceHistoryItem> ServiceHistory { get; set; } = [];
}

public class StaffCustomerVehicleDetail
{
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class StaffCustomerPurchaseHistoryItem
{
    public int SalesInvoiceId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class StaffCustomerServiceHistoryItem
{
    public int AppointmentId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ServiceNote { get; set; } = string.Empty;
}
