namespace VehiclePartsBackend.Dtos.Admin;

public class AdminFinancialReportResponse
{
    public string Period { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalSalesRevenue { get; set; }
    public decimal TotalPurchaseCost { get; set; }
    public decimal GrossProfit { get; set; }
    public int SalesInvoiceCount { get; set; }
    public int PurchaseInvoiceCount { get; set; }
    public List<AdminFinancialReportBucket> Buckets { get; set; } = new List<AdminFinancialReportBucket>();
}

public class AdminFinancialReportBucket
{
    public string Label { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public decimal SalesRevenue { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal GrossProfit { get; set; }
    public int SalesCount { get; set; }
    public int PurchaseCount { get; set; }
}