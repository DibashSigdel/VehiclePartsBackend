using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Staff;

namespace VehiclePartsBackend.Services;

public class SalesInvoiceEmailService
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;

    public SalesInvoiceEmailService(AppDbContext context, IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }

    public async Task<StaffSendInvoiceEmailResponse?> SendInvoiceEmailAsync(int salesInvoiceId)
    {
        var header = await (
            from inv in _context.SalesInvoices.AsNoTracking()
            join customer in _context.Users.AsNoTracking() on inv.CustomerId equals customer.UserId
            where inv.SalesInvoiceId == salesInvoiceId
            select new
            {
                inv.SalesInvoiceId,
                inv.InvoiceDate,
                inv.SubTotal,
                inv.DiscountAmount,
                inv.TotalAmount,
                inv.PaymentType,
                inv.PaymentStatus,
                inv.CreditDueDate,
                customer.Name,
                customer.Email
            })
            .SingleOrDefaultAsync();

        if (header is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(header.Email))
        {
            return new StaffSendInvoiceEmailResponse
            {
                SalesInvoiceId = salesInvoiceId,
                Sent = false,
                Message = "Customer has no email address on file."
            };
        }

        var lines = await (
            from item in _context.SalesInvoiceItems.AsNoTracking()
            join part in _context.Parts.AsNoTracking() on item.PartId equals part.PartId
            where item.SalesInvoiceId == salesInvoiceId
            orderby part.PartName
            select new StaffSalesInvoiceLineDetailResponse
            {
                PartName = part.PartName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            })
            .ToListAsync();

        var subject = $"Sales invoice #{header.SalesInvoiceId} — Vehicle Parts System";
        var htmlBody = BuildInvoiceHtml(header.SalesInvoiceId, header.InvoiceDate, header.Name,
            header.SubTotal, header.DiscountAmount, header.TotalAmount, header.PaymentType,
            header.PaymentStatus, header.CreditDueDate, lines);

        await _emailSender.SendAsync(header.Email, subject, htmlBody);

        return new StaffSendInvoiceEmailResponse
        {
            SalesInvoiceId = salesInvoiceId,
            Sent = true,
            Message = $"Invoice emailed to {header.Email}."
        };
    }

    private static string BuildInvoiceHtml(
        int invoiceId,
        DateTime invoiceDate,
        string customerName,
        decimal subTotal,
        decimal discount,
        decimal total,
        string paymentType,
        string paymentStatus,
        DateTime? creditDueDate,
        List<StaffSalesInvoiceLineDetailResponse> lines)
    {
        var rows = string.Join(string.Empty, lines.Select(line =>
            $"<tr><td>{WebEncode(line.PartName)}</td><td style=\"text-align:right\">{line.Quantity}</td>" +
            $"<td style=\"text-align:right\">{line.UnitPrice:N2}</td><td style=\"text-align:right\">{line.LineTotal:N2}</td></tr>"));

        var creditLine = creditDueDate.HasValue
            ? $"<p>Credit due date: {creditDueDate.Value:yyyy-MM-dd}</p>"
            : string.Empty;

        return $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif">
            <h2>Vehicle Parts — Sales Invoice #{invoiceId}</h2>
            <p>Dear {WebEncode(customerName)},</p>
            <p>Thank you for your purchase. Invoice date: <strong>{invoiceDate:yyyy-MM-dd}</strong></p>
            <table border="1" cellpadding="8" cellspacing="0" style="border-collapse:collapse;width:100%;max-width:640px">
            <thead><tr><th>Part</th><th>Qty</th><th>Unit price</th><th>Line total</th></tr></thead>
            <tbody>{rows}</tbody>
            </table>
            <p>Subtotal: {subTotal:N2}<br/>Discount: {discount:N2}<br/><strong>Total: {total:N2}</strong></p>
            <p>Payment: {WebEncode(paymentType)} / {WebEncode(paymentStatus)}</p>
            {creditLine}
            <p>Regards,<br/>Vehicle Service Center</p>
            </body></html>
            """;
    }

    private static string WebEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}