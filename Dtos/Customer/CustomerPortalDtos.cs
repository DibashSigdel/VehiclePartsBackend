namespace VehiclePartsBackend.Dtos.Customer;

public class CustomerVehicleOption
{
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class CustomerAppointmentResponse
{
    public int AppointmentId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ServiceNote { get; set; } = string.Empty;
}

public class CustomerBookAppointmentRequest
{
    public int VehicleId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string ServiceNote { get; set; } = string.Empty;
}

public class CustomerPartRequestResponse
{
    public int PartRequestId { get; set; }
    public string RequestedPartName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string RequestStatus { get; set; } = string.Empty;
}

public class CustomerCreatePartRequest
{
    public string RequestedPartName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class CustomerReviewableAppointment
{
    public int AppointmentId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CustomerReviewResponse
{
    public int ReviewId { get; set; }
    public int AppointmentId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
}

public class CustomerSubmitReviewRequest
{
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}