using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Customer;
using VehiclePartsBackend.Helpers;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/customer/portal")]
[Authorize(Roles = Roles.Customer)]
public class CustomerPortalController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomerPortalController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("vehicles")]
    public async Task<ActionResult<List<CustomerVehicleOption>>> GetMyVehicles()
    {
        var customerId = User.GetUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var vehicles = await _context.Vehicles
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId.Value)
            .OrderBy(x => x.VehicleNumber)
            .Select(x => new CustomerVehicleOption
            {
                VehicleId = x.VehicleId,
                VehicleNumber = x.VehicleNumber,
                Brand = x.Brand,
                Model = x.Model,
                Year = x.Year
            })
            .ToListAsync();

        return Ok(vehicles);
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<List<CustomerAppointmentResponse>>> GetAppointments()
    {
        var customerId = User.GetUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var rows = await (
            from a in _context.Appointments.AsNoTracking()
            join v in _context.Vehicles.AsNoTracking() on a.VehicleId equals v.VehicleId
            where a.CustomerId == customerId.Value
            orderby a.AppointmentDate descending
            select new
            {
                a.AppointmentId,
                a.VehicleId,
                v.Brand,
                v.Model,
                v.VehicleNumber,
                a.AppointmentDate,
                a.Status,
                a.ServiceNote
            })
            .ToListAsync();

        var appointments = rows.Select(x => new CustomerAppointmentResponse
        {
            AppointmentId = x.AppointmentId,
            VehicleId = x.VehicleId,
            VehicleLabel = FormatVehicleLabel(x.Brand, x.Model, x.VehicleNumber),
            AppointmentDate = x.AppointmentDate,
            Status = x.Status,
            ServiceNote = x.ServiceNote
        }).ToList();

        return Ok(appointments);
    }

    [HttpPost("appointments")]
    public async Task<ActionResult<CustomerAppointmentResponse>> BookAppointment(CustomerBookAppointmentRequest request)
    {
        var customerId = User.GetUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        if (request.VehicleId <= 0)
        {
            return BadRequest("Select a vehicle.");
        }

        var appointmentDate = DateTime.SpecifyKind(request.AppointmentDate, DateTimeKind.Utc);
        if (appointmentDate <= DateTime.UtcNow)
        {
            return BadRequest("Appointment date must be in the future.");
        }

        var vehicle = await _context.Vehicles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.VehicleId == request.VehicleId && x.CustomerId == customerId.Value);
        if (vehicle is null)
        {
            return BadRequest("Vehicle not found for your account.");
        }

        var appointment = new Appointment
        {
            CustomerId = customerId.Value,
            VehicleId = request.VehicleId,
            AppointmentDate = appointmentDate,
            Status = "Booked",
            ServiceNote = request.ServiceNote?.Trim() ?? string.Empty
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return Ok(new CustomerAppointmentResponse
        {
            AppointmentId = appointment.AppointmentId,
            VehicleId = vehicle.VehicleId,
            VehicleLabel = FormatVehicleLabel(vehicle.Brand, vehicle.Model, vehicle.VehicleNumber),
            AppointmentDate = appointment.AppointmentDate,
            Status = appointment.Status,
            ServiceNote = appointment.ServiceNote
        });
    }

    [HttpGet("part-requests")]
    public async Task<ActionResult<List<CustomerPartRequestResponse>>> GetPartRequests()
    {
        var customerId = User.GetUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var requests = await _context.PartRequests
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId.Value)
            .OrderByDescending(x => x.RequestDate)
            .Select(x => new CustomerPartRequestResponse
            {
                PartRequestId = x.PartRequestId,
                RequestedPartName = x.RequestedPartName,
                Details = x.Details,
                RequestDate = x.RequestDate,
                RequestStatus = x.RequestStatus
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPost("part-requests")]
    public async Task<ActionResult<CustomerPartRequestResponse>> CreatePartRequest(CustomerCreatePartRequest request)
    {
        var customerId = User.GetUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.RequestedPartName))
        {
            return BadRequest("Part name is required.");
        }

        var partRequest = new PartRequest
        {
            CustomerId = customerId.Value,
            RequestedPartName = request.RequestedPartName.Trim(),
            Details = request.Details?.Trim() ?? string.Empty,
            RequestDate = DateTime.UtcNow,
            RequestStatus = "Pending"
        };

        _context.PartRequests.Add(partRequest);
        await _context.SaveChangesAsync();

        return Ok(new CustomerPartRequestResponse
        {
            PartRequestId = partRequest.PartRequestId,
            RequestedPartName = partRequest.RequestedPartName,
            Details = partRequest.Details,
            RequestDate = partRequest.RequestDate,
            RequestStatus = partRequest.RequestStatus
        });
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<List<CustomerReviewResponse>>> GetReviews()
    {
        var customerId = User.GetUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var rows = await (
            from r in _context.Reviews.AsNoTracking()
            join a in _context.Appointments.AsNoTracking() on r.AppointmentId equals a.AppointmentId
            join v in _context.Vehicles.AsNoTracking() on a.VehicleId equals v.VehicleId
            where r.CustomerId == customerId.Value
            orderby r.ReviewDate descending
            select new
            {
                r.ReviewId,
                r.AppointmentId,
                v.Brand,
                v.Model,
                v.VehicleNumber,
                r.Rating,
                r.Comment,
                r.ReviewDate
            })
            .ToListAsync();

        var reviews = rows.Select(x => new CustomerReviewResponse
        {
            ReviewId = x.ReviewId,
            AppointmentId = x.AppointmentId,
            VehicleLabel = FormatVehicleLabel(x.Brand, x.Model, x.VehicleNumber),
            Rating = x.Rating,
            Comment = x.Comment,
            ReviewDate = x.ReviewDate
        }).ToList();

        return Ok(reviews);
    }

    [HttpGet("appointments/reviewable")]
    public async Task<ActionResult<List<CustomerReviewableAppointment>>> GetReviewableAppointments()
    {
        var customerId = User.GetUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var reviewedIds = await _context.Reviews
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId.Value)
            .Select(x => x.AppointmentId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var rows = await (
            from a in _context.Appointments.AsNoTracking()
            join v in _context.Vehicles.AsNoTracking() on a.VehicleId equals v.VehicleId
            where a.CustomerId == customerId.Value
                  && !reviewedIds.Contains(a.AppointmentId)
                  && (a.Status == "Completed" || a.AppointmentDate <= now)
            orderby a.AppointmentDate descending
            select new
            {
                a.AppointmentId,
                v.Brand,
                v.Model,
                v.VehicleNumber,
                a.AppointmentDate,
                a.Status
            })
            .ToListAsync();

        var reviewable = rows.Select(x => new CustomerReviewableAppointment
        {
            AppointmentId = x.AppointmentId,
            VehicleLabel = FormatVehicleLabel(x.Brand, x.Model, x.VehicleNumber),
            AppointmentDate = x.AppointmentDate,
            Status = x.Status
        }).ToList();

        return Ok(reviewable);
    }

    [HttpPost("reviews")]
    public async Task<ActionResult<CustomerReviewResponse>> SubmitReview(CustomerSubmitReviewRequest request)
    {
        var customerId = User.GetUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        if (request.Rating is < 1 or > 5)
        {
            return BadRequest("Rating must be between 1 and 5.");
        }

        var appointment = await _context.Appointments
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.AppointmentId == request.AppointmentId && x.CustomerId == customerId.Value);
        if (appointment is null)
        {
            return BadRequest("Appointment not found.");
        }

        if (await _context.Reviews.AnyAsync(x => x.AppointmentId == request.AppointmentId))
        {
            return BadRequest("This appointment already has a review.");
        }

        var now = DateTime.UtcNow;
        if (appointment.Status != "Completed" && appointment.AppointmentDate > now)
        {
            return BadRequest("You can only review completed or past appointments.");
        }

        var review = new Review
        {
            CustomerId = customerId.Value,
            AppointmentId = request.AppointmentId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim() ?? string.Empty,
            ReviewDate = now
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        var vehicle = await _context.Vehicles.AsNoTracking()
            .SingleAsync(x => x.VehicleId == appointment.VehicleId);

        return Ok(new CustomerReviewResponse
        {
            ReviewId = review.ReviewId,
            AppointmentId = review.AppointmentId,
            VehicleLabel = FormatVehicleLabel(vehicle.Brand, vehicle.Model, vehicle.VehicleNumber),
            Rating = review.Rating,
            Comment = review.Comment,
            ReviewDate = review.ReviewDate
        });
    }

    private static string FormatVehicleLabel(string brand, string model, string vehicleNumber) =>
        $"{brand} {model} ({vehicleNumber})";
}