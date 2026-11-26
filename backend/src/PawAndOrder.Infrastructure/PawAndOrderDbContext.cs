using Microsoft.EntityFrameworkCore;
using PawAndOrder.Domain;

namespace PawAndOrder.Infrastructure;

public sealed class PawAndOrderDbContext(DbContextOptions<PawAndOrderDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientAlert> PatientAlerts => Set<PatientAlert>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<DiagnosisCatalogItem> DiagnosisCatalogItems => Set<DiagnosisCatalogItem>();
    public DbSet<ProcedureCatalogItem> ProcedureCatalogItems => Set<ProcedureCatalogItem>();
    public DbSet<VaccinationType> VaccinationTypes => Set<VaccinationType>();
    public DbSet<Consult> Consults => Set<Consult>();
    public DbSet<ConsultDiagnosis> ConsultDiagnoses => Set<ConsultDiagnosis>();
    public DbSet<ConsultProcedure> ConsultProcedures => Set<ConsultProcedure>();
    public DbSet<ConsultMedication> ConsultMedications => Set<ConsultMedication>();
    public DbSet<PatientVaccinationRecord> PatientVaccinationRecords => Set<PatientVaccinationRecord>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEnums(modelBuilder);
        ConfigureDecimalPrecision(modelBuilder);

        modelBuilder.Entity<Client>().HasIndex(x => new { x.LastName, x.FirstName });
        modelBuilder.Entity<Client>().HasIndex(x => x.Phone);
        modelBuilder.Entity<Patient>().HasIndex(x => x.Name);
        modelBuilder.Entity<Patient>().HasIndex(x => x.ClientId);
        modelBuilder.Entity<Appointment>().HasIndex(x => new { x.StartAt, x.ClinicianUserId });
        modelBuilder.Entity<Invoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Name);
        modelBuilder.Entity<InventoryLot>().HasIndex(x => x.BatchNumber);
        modelBuilder.Entity<PatientVaccinationRecord>().HasIndex(x => x.DueDate);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Consult)
            .WithOne(x => x.Appointment)
            .HasForeignKey<Consult>(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Client)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.Clinician)
            .WithMany()
            .HasForeignKey(x => x.ClinicianUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Consult>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Consults)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Consult>()
            .HasOne(x => x.Clinician)
            .WithMany()
            .HasForeignKey(x => x.ClinicianUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(x => x.Consult)
            .WithOne(x => x.Invoice)
            .HasForeignKey<Invoice>(x => x.ConsultId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>()
            .HasOne(x => x.Client)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProcedureCatalogItem>()
            .HasOne(x => x.DefaultProduct)
            .WithMany()
            .HasForeignKey(x => x.DefaultProductId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Patient>()
            .HasOne(x => x.Client)
            .WithMany(x => x.Patients)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientAlert>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Alerts)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientVaccinationRecord>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.VaccinationRecords)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientVaccinationRecord>()
            .HasOne(x => x.Consult)
            .WithMany(x => x.Vaccinations)
            .HasForeignKey(x => x.ConsultId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PatientVaccinationRecord>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientVaccinationRecord>()
            .HasOne(x => x.InventoryLot)
            .WithMany()
            .HasForeignKey(x => x.InventoryLotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientVaccinationRecord>()
            .HasOne(x => x.VaccinationType)
            .WithMany()
            .HasForeignKey(x => x.VaccinationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConsultDiagnosis>()
            .HasOne(x => x.Consult)
            .WithMany(x => x.Diagnoses)
            .HasForeignKey(x => x.ConsultId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConsultProcedure>()
            .HasOne(x => x.Consult)
            .WithMany(x => x.Procedures)
            .HasForeignKey(x => x.ConsultId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConsultMedication>()
            .HasOne(x => x.Consult)
            .WithMany(x => x.Medications)
            .HasForeignKey(x => x.ConsultId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConsultMedication>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConsultMedication>()
            .HasOne(x => x.InventoryLot)
            .WithMany()
            .HasForeignKey(x => x.InventoryLotId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ConsultDiagnosis>()
            .HasOne(x => x.DiagnosisCatalogItem)
            .WithMany()
            .HasForeignKey(x => x.DiagnosisCatalogItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ConsultProcedure>()
            .HasOne(x => x.ProcedureCatalogItem)
            .WithMany()
            .HasForeignKey(x => x.ProcedureCatalogItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(x => x.Vendor)
            .WithMany(x => x.PurchaseOrders)
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InvoiceLine>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceLine>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InvoiceLine>()
            .HasOne(x => x.InventoryLot)
            .WithMany()
            .HasForeignKey(x => x.InventoryLotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureEnums(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(x => x.Role).HasConversion<string>();
        modelBuilder.Entity<Appointment>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Consult>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Invoice>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<Payment>().Property(x => x.PaymentMethod).HasConversion<string>();
        modelBuilder.Entity<Product>().Property(x => x.Type).HasConversion<string>();
        modelBuilder.Entity<InvoiceLine>().Property(x => x.LineType).HasConversion<string>();
        modelBuilder.Entity<InventoryTransaction>().Property(x => x.Type).HasConversion<string>();
        modelBuilder.Entity<PurchaseOrder>().Property(x => x.Status).HasConversion<string>();
    }

    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }
}
