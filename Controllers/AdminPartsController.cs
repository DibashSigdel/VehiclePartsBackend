using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Admin;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/admin/parts")]
[Authorize(Roles = Roles.Admin)]
public class AdminPartsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminPartsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminPartResponse>>> GetParts()
    {
        var parts = await _context.Parts
            .Include(x => x.Category)
            .Include(x => x.Stock)
            .OrderBy(x => x.PartName)
            .Select(x => new AdminPartResponse
            {
                PartId = x.PartId,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.CategoryName : string.Empty,
                PartName = x.PartName,
                Description = x.Description,
                SellingPrice = x.SellingPrice,
                ReorderLevel = x.ReorderLevel,
                IsActive = x.IsActive,
                QuantityOnHand = x.Stock != null ? x.Stock.QuantityOnHand : 0
            })
            .ToListAsync();

        return Ok(parts);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<AdminPartCategoryResponse>>> GetCategories()
    {
        var categories = await _context.PartCategories
            .OrderBy(x => x.CategoryName)
            .Select(x => new AdminPartCategoryResponse
            {
                CategoryId = x.CategoryId,
                CategoryName = x.CategoryName
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(AdminCreatePartCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryName))
        {
            return BadRequest("Category name is required.");
        }

        var exists = await _context.PartCategories.AnyAsync(x => x.CategoryName == request.CategoryName);
        if (exists)
        {
            return BadRequest("Category already exists.");
        }

        var category = new PartCategory
        {
            CategoryName = request.CategoryName
        };

        _context.PartCategories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new AdminPartCategoryResponse
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName
        });
    }

    [HttpPut("categories/{categoryId:int}")]
    public async Task<IActionResult> UpdateCategory(int categoryId, AdminUpdatePartCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryName))
        {
            return BadRequest("Category name is required.");
        }

        var category = await _context.PartCategories.SingleOrDefaultAsync(x => x.CategoryId == categoryId);
        if (category is null)
        {
            return NotFound("Category not found.");
        }

        category.CategoryName = request.CategoryName;
        await _context.SaveChangesAsync();
        return Ok("Category updated.");
    }

    [HttpDelete("categories/{categoryId:int}")]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var category = await _context.PartCategories.Include(x => x.Parts).SingleOrDefaultAsync(x => x.CategoryId == categoryId);
        if (category is null)
        {
            return NotFound("Category not found.");
        }

        if (category.Parts.Any())
        {
            return BadRequest("Cannot delete category that is used by parts.");
        }

        _context.PartCategories.Remove(category);
        await _context.SaveChangesAsync();
        return Ok("Category deleted.");
    }

    [HttpPost]
    public async Task<IActionResult> CreatePart(AdminCreatePartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PartName))
        {
            return BadRequest("Part name is required.");
        }

        var categoryExists = await _context.PartCategories.AnyAsync(x => x.CategoryId == request.CategoryId);
        if (!categoryExists)
        {
            return BadRequest("Invalid category.");
        }

        var part = new Part
        {
            CategoryId = request.CategoryId,
            PartName = request.PartName,
            Description = request.Description,
            SellingPrice = request.SellingPrice,
            ReorderLevel = request.ReorderLevel,
            IsActive = request.IsActive
        };

        _context.Parts.Add(part);
        await _context.SaveChangesAsync();

        var stock = new Stock
        {
            PartId = part.PartId,
            QuantityOnHand = request.QuantityOnHand,
            LastUpdated = DateTime.UtcNow
        };

        _context.Stocks.Add(stock);
        await _context.SaveChangesAsync();

        var category = await _context.PartCategories.SingleAsync(x => x.CategoryId == part.CategoryId);
        return Ok(new AdminPartResponse
        {
            PartId = part.PartId,
            CategoryId = part.CategoryId,
            CategoryName = category.CategoryName,
            PartName = part.PartName,
            Description = part.Description,
            SellingPrice = part.SellingPrice,
            ReorderLevel = part.ReorderLevel,
            IsActive = part.IsActive,
            QuantityOnHand = stock.QuantityOnHand
        });
    }

    [HttpPut("{partId:int}")]
    public async Task<IActionResult> UpdatePart(int partId, AdminUpdatePartRequest request)
    {
        var part = await _context.Parts.Include(x => x.Stock).SingleOrDefaultAsync(x => x.PartId == partId);
        if (part is null)
        {
            return NotFound("Part not found.");
        }

        var categoryExists = await _context.PartCategories.AnyAsync(x => x.CategoryId == request.CategoryId);
        if (!categoryExists)
        {
            return BadRequest("Invalid category.");
        }

        part.CategoryId = request.CategoryId;
        part.PartName = request.PartName;
        part.Description = request.Description;
        part.SellingPrice = request.SellingPrice;
        part.ReorderLevel = request.ReorderLevel;
        part.IsActive = request.IsActive;

        if (part.Stock is null)
        {
            part.Stock = new Stock
            {
                PartId = part.PartId,
                QuantityOnHand = request.QuantityOnHand,
                LastUpdated = DateTime.UtcNow
            };
        }
        else
        {
            part.Stock.QuantityOnHand = request.QuantityOnHand;
            part.Stock.LastUpdated = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok("Part updated.");
    }

    [HttpDelete("{partId:int}")]
    public async Task<IActionResult> DeletePart(int partId)
    {
        var part = await _context.Parts.Include(x => x.Stock).SingleOrDefaultAsync(x => x.PartId == partId);
        if (part is null)
        {
            return NotFound("Part not found.");
        }

        if (part.Stock is not null)
        {
            _context.Stocks.Remove(part.Stock);
        }

        _context.Parts.Remove(part);
        await _context.SaveChangesAsync();
        return Ok("Part deleted.");
    }
}
