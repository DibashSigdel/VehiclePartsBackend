namespace VehiclePartsBackend.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    public static readonly string[] Allowed = [Admin, Staff, Customer];
}
