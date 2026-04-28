using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Admin;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/admin/vendors")]
[Authorize(Roles = Roles.Admin)]
public class AdminVendorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminVendorsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminVendorResponse>>> GetVendors()
    {
        var vendors = await _context.Vendors
            .OrderBy(x => x.VendorName)
            .Select(x => new AdminVendorResponse
            {
                VendorId = x.VendorId,
                VendorName = x.VendorName,
                VendorPhone = x.VendorPhone,
                VendorEmail = x.VendorEmail,
                VendorAddress = x.VendorAddress,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(vendors);
    }

    [HttpPost]
    public async Task<IActionResult> CreateVendor(AdminCreateVendorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VendorName))
        {
            return BadRequest("Vendor name is required.");
        }

        var vendor = new Vendor
        {
            VendorName = request.VendorName,
            VendorPhone = request.VendorPhone,
            VendorEmail = request.VendorEmail,
            VendorAddress = request.VendorAddress,
            IsActive = request.IsActive
        };

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        return Ok(new AdminVendorResponse
        {
            VendorId = vendor.VendorId,
            VendorName = vendor.VendorName,
            VendorPhone = vendor.VendorPhone,
            VendorEmail = vendor.VendorEmail,
            VendorAddress = vendor.VendorAddress,
            IsActive = vendor.IsActive
        });
    }

    [HttpPut("{vendorId:int}")]
    public async Task<IActionResult> UpdateVendor(int vendorId, AdminUpdateVendorRequest request)
    {
        var vendor = await _context.Vendors.SingleOrDefaultAsync(x => x.VendorId == vendorId);
        if (vendor is null)
        {
            return NotFound("Vendor not found.");
        }

        vendor.VendorName = request.VendorName;
        vendor.VendorPhone = request.VendorPhone;
        vendor.VendorEmail = request.VendorEmail;
        vendor.VendorAddress = request.VendorAddress;
        vendor.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok("Vendor updated.");
    }

    [HttpDelete("{vendorId:int}")]
    public async Task<IActionResult> DeleteVendor(int vendorId)
    {
        var vendor = await _context.Vendors.SingleOrDefaultAsync(x => x.VendorId == vendorId);
        if (vendor is null)
        {
            return NotFound("Vendor not found.");
        }

        _context.Vendors.Remove(vendor);
        await _context.SaveChangesAsync();
        return Ok("Vendor deleted.");
    }
}
