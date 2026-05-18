namespace VehiclePartsBackend.Services;

public class EmailSettings
{
	public string FromAddress { get; set; } = "noreply@vehicleparts.local";
	public string FromName { get; set; } = "Vehicle Parts System";
	public string SmtpHost { get; set; } = string.Empty;
	public int SmtpPort { get; set; } = 587;
	public string SmtpUser { get; set; } = string.Empty;
	public string SmtpPassword { get; set; } = string.Empty;
	public bool UseDevelopmentMode { get; set; } = true;
}