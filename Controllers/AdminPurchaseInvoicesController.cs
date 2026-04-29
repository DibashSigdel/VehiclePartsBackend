using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Admin;
using VehiclePartsBackend.Helpers;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/admin/purchase-invoices")]
[Authorize(Roles = Roles.Admin)]
public class AdminPurchaseInvoicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminPurchaseInvoicesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminPurchaseInvoiceListItemResponse>>> GetPurchaseInvoices()
    {
        var list = await _context.PurchaseInvoices
            .AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.Items)
            .OrderByDescending(x => x.PurchaseDate)
            .Take(100)
            .Select(x => new AdminPurchaseInvoiceListItemResponse
            {
                PurchaseInvoiceId = x.PurchaseInvoiceId,
                VendorName = x.Vendor != null ? x.Vendor.VendorName : "",
                PurchaseDate = x.PurchaseDate,
                TotalCost = x.TotalCost,
                LineCount = x.Items.Count
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminPurchaseInvoiceResponse>> CreatePurchaseInvoice(AdminCreatePurchaseInvoiceRequest request)
    {
        var adminId = User.GetUserId();
        if (adminId is null)
        {
            return Unauthorized();
        }

        if (request.Items.Count == 0)
        {
            return BadRequest("Add at least one line item.");
        }

        var vendor = await _context.Vendors.SingleOrDefaultAsync(x => x.VendorId == request.VendorId);
        if (vendor is null || !vendor.IsActive)
        {
            return BadRequest("Vendor not found or inactive.");
        }

        foreach (var line in request.Items)
        {
            if (line.QuantityBought <= 0)
            {
                return BadRequest("Quantity must be greater than zero on every line.");
            }

            if (line.CostPrice < 0)
            {
                return BadRequest("Cost price cannot be negative.");
            }
        }

        var partIds = request.Items.Select(x => x.PartId).Distinct().ToList();
        var parts = await _context.Parts
            .Include(x => x.Stock)
            .Where(x => partIds.Contains(x.PartId))
            .ToDictionaryAsync(x => x.PartId);

        foreach (var pid in partIds)
        {
            if (!parts.TryGetValue(pid, out var part) || !part.IsActive)
            {
                return BadRequest($"Part {pid} not found or inactive.");
            }
        }

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            var purchaseDate = request.PurchaseDate ?? DateTime.UtcNow;
            var invoice = new PurchaseInvoice
            {
                VendorId = request.VendorId,
                CreatedByAdminId = adminId.Value,
                PurchaseDate = purchaseDate,
                TotalCost = 0
            };

            decimal totalCost = 0;
            foreach (var line in request.Items)
            {
                var lineTotal = line.CostPrice * line.QuantityBought;
                totalCost += lineTotal;
                invoice.Items.Add(new PurchaseInvoiceItem
                {
                    PartId = line.PartId,
                    CostPrice = line.CostPrice,
                    QuantityBought = line.QuantityBought,
                    LineTotal = lineTotal
                });
            }

            invoice.TotalCost = totalCost;
            _context.PurchaseInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var line in request.Items)
            {
                var part = parts[line.PartId];
                if (part.Stock is null)
                {
                    part.Stock = new Stock
                    {
                        PartId = part.PartId,
                        QuantityOnHand = line.QuantityBought,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.Stocks.Add(part.Stock);
                }
                else
                {
                    part.Stock.QuantityOnHand += line.QuantityBought;
                    part.Stock.LastUpdated = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new AdminPurchaseInvoiceResponse
            {
                PurchaseInvoiceId = invoice.PurchaseInvoiceId,
                VendorId = vendor.VendorId,
                VendorName = vendor.VendorName,
                PurchaseDate = invoice.PurchaseDate,
                TotalCost = invoice.TotalCost
            });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
