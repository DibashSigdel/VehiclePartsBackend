using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Admin;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = Roles.Admin)]
public class AdminReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("financial")]
    public async Task<ActionResult<AdminFinancialReportResponse>> GetFinancialReport(
        [FromQuery] string period = "monthly",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var normalizedPeriod = period.Trim().ToLowerInvariant();
        if (normalizedPeriod is not ("daily" or "monthly" or "yearly"))
        {
            return BadRequest("Period must be daily, monthly, or yearly.");
        }

        var rangeEnd = to.HasValue ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc) : DateTime.UtcNow;
        var rangeStart = from.HasValue
            ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc)
            : normalizedPeriod switch
            {
                "daily" => rangeEnd.AddDays(-30),
                "yearly" => rangeEnd.AddYears(-5),
                _ => rangeEnd.AddMonths(-12)
            };

        if (rangeStart > rangeEnd)
        {
            return BadRequest("'from' must be before 'to'.");
        }

        var sales = await _context.SalesInvoices
            .AsNoTracking()
            .Where(x => x.InvoiceDate >= rangeStart && x.InvoiceDate <= rangeEnd)
            .Select(x => new MoneyRow(x.InvoiceDate, x.TotalAmount))
            .ToListAsync();

        var purchases = await _context.PurchaseInvoices
            .AsNoTracking()
            .Where(x => x.PurchaseDate >= rangeStart && x.PurchaseDate <= rangeEnd)
            .Select(x => new MoneyRow(x.PurchaseDate, x.TotalCost))
            .ToListAsync();

        var buckets = BuildBuckets(normalizedPeriod, rangeStart, rangeEnd, sales, purchases);
        var totalSales = sales.Sum(x => x.Amount);
        var totalPurchases = purchases.Sum(x => x.Amount);

        return Ok(new AdminFinancialReportResponse
        {
            Period = normalizedPeriod,
            From = rangeStart,
            To = rangeEnd,
            TotalSalesRevenue = totalSales,
            TotalPurchaseCost = totalPurchases,
            GrossProfit = totalSales - totalPurchases,
            SalesInvoiceCount = sales.Count,
            PurchaseInvoiceCount = purchases.Count,
            Buckets = buckets
        });
    }

    private static List<AdminFinancialReportBucket> BuildBuckets(
        string period,
        DateTime rangeStart,
        DateTime rangeEnd,
        List<MoneyRow> sales,
        List<MoneyRow> purchases)
    {
        var bucketStarts = new List<DateTime>();
        var cursor = AlignPeriodStart(period, rangeStart);
        var endAligned = AlignPeriodStart(period, rangeEnd);

        while (cursor <= endAligned)
        {
            bucketStarts.Add(cursor);
            cursor = period switch
            {
                "daily" => cursor.AddDays(1),
                "yearly" => cursor.AddYears(1),
                _ => cursor.AddMonths(1)
            };
        }

        if (bucketStarts.Count == 0)
        {
            bucketStarts.Add(AlignPeriodStart(period, rangeStart));
        }

        var buckets = new List<AdminFinancialReportBucket>();
        foreach (var start in bucketStarts)
        {
            var next = period switch
            {
                "daily" => start.AddDays(1),
                "yearly" => start.AddYears(1),
                _ => start.AddMonths(1)
            };

            var periodSales = sales.Where(x => x.Date >= start && x.Date < next).ToList();
            var periodPurchases = purchases.Where(x => x.Date >= start && x.Date < next).ToList();
            var salesTotal = periodSales.Sum(x => x.Amount);
            var purchaseTotal = periodPurchases.Sum(x => x.Amount);

            buckets.Add(new AdminFinancialReportBucket
            {
                Label = FormatLabel(period, start),
                PeriodStart = start,
                SalesRevenue = salesTotal,
                PurchaseCost = purchaseTotal,
                GrossProfit = salesTotal - purchaseTotal,
                SalesCount = periodSales.Count,
                PurchaseCount = periodPurchases.Count
            });
        }

        return buckets;
    }

    private static DateTime AlignPeriodStart(string period, DateTime date) =>
        period switch
        {
            "daily" => date.Date,
            "yearly" => new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    private static string FormatLabel(string period, DateTime start) =>
        period switch
        {
            "daily" => start.ToString("yyyy-MM-dd"),
            "yearly" => start.ToString("yyyy"),
            _ => start.ToString("yyyy-MM")
        };

    private sealed record MoneyRow(DateTime Date, decimal Amount);
}