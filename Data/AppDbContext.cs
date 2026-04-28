using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PartCategory> PartCategories => Set<PartCategory>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PartRequest> PartRequests => Set<PartRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CreditReminder> CreditReminders => Set<CreditReminder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PartCategory>().HasKey(x => x.CategoryId);
        modelBuilder.Entity<Stock>().HasKey(x => x.PartId);
        modelBuilder.Entity<PurchaseInvoiceItem>().HasKey(x => x.PurchaseItemId);
        modelBuilder.Entity<SalesInvoiceItem>().HasKey(x => x.SalesItemId);
        modelBuilder.Entity<CreditReminder>().HasKey(x => x.ReminderId);

        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(x => x.VehicleNumber).IsUnique();
        modelBuilder.Entity<PartCategory>().HasIndex(x => x.CategoryName).IsUnique();

        modelBuilder.Entity<User>().Property(x => x.Role).HasMaxLength(20);

        modelBuilder.Entity<SalesInvoice>()
            .Property(x => x.PaymentType)
            .HasMaxLength(20);

        modelBuilder.Entity<SalesInvoice>()
            .Property(x => x.PaymentStatus)
            .HasMaxLength(20);

        modelBuilder.Entity<Appointment>()
            .Property(x => x.Status)
            .HasMaxLength(20);

        modelBuilder.Entity<PartRequest>()
            .Property(x => x.RequestStatus)
            .HasMaxLength(20);

        modelBuilder.Entity<Stock>()
            .HasOne(x => x.Part)
            .WithOne(x => x.Stock)
            .HasForeignKey<Stock>(x => x.PartId);

        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(x => x.Vendor)
            .WithMany(x => x.PurchaseInvoices)
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(x => x.CreatedByAdmin)
            .WithMany(x => x.CreatedPurchaseInvoices)
            .HasForeignKey(x => x.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalesInvoice>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.CustomerSalesInvoices)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalesInvoice>()
            .HasOne(x => x.CreatedByStaff)
            .WithMany(x => x.StaffCreatedSalesInvoices)
            .HasForeignKey(x => x.CreatedByStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Vehicle)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Appointment)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PartRequest>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.PartRequests)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(x => x.User)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CreditReminder>()
            .HasOne(x => x.SalesInvoice)
            .WithMany(x => x.CreditReminders)
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CreditReminder>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.CreditReminders)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .ToTable(x => x.HasCheckConstraint("CK_Review_Rating", "\"Rating\" >= 1 AND \"Rating\" <= 5"));
    }
}
