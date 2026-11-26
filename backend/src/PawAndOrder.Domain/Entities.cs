namespace PawAndOrder.Domain;

public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum UserRole
{
    Admin,
    Veterinarian,
    Nurse,
    Receptionist,
    InventoryManager
}

public enum AppointmentStatus
{
    Booked,
    Arrived,
    InConsult,
    Completed,
    Cancelled,
    NoShow
}

public enum ConsultStatus
{
    Draft,
    Finalized
}

public enum InvoiceStatus
{
    Draft,
    Issued,
    Paid,
    PartiallyPaid,
    Void
}

public enum ProductType
{
    Service,
    Medication,
    RetailItem,
    Consumable
}

public enum InvoiceLineType
{
    Service,
    Product
}

public enum InventoryTransactionType
{
    Receive,
    Dispense,
    Adjustment,
    Correction
}

public enum PurchaseOrderStatus
{
    Draft,
    Submitted,
    PartiallyReceived,
    Received,
    Cancelled
}

public enum PaymentMethod
{
    Cash,
    Card
}

public sealed class User : EntityBase
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Client : EntityBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
    public string? Notes { get; set; }

    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public sealed class Patient : EntityBase
{
    public int ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public string? Sex { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public int? EstimatedAgeYears { get; set; }
    public string? Color { get; set; }
    public string? MicrochipNumber { get; set; }
    public decimal WeightKg { get; set; }
    public bool IsDesexed { get; set; }
    public bool IsDeceased { get; set; }
    public string? AlertsSummary { get; set; }
    public string? AllergiesSummary { get; set; }
    public string? Notes { get; set; }

    public Client? Client { get; set; }
    public ICollection<PatientAlert> Alerts { get; set; } = new List<PatientAlert>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Consult> Consults { get; set; } = new List<Consult>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<PatientVaccinationRecord> VaccinationRecords { get; set; } = new List<PatientVaccinationRecord>();
}

public sealed class PatientAlert : EntityBase
{
    public int PatientId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;

    public Patient? Patient { get; set; }
}

public sealed class AppointmentType : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string ColorHex { get; set; } = "#1f6feb";
    public bool IsActive { get; set; } = true;
}

public sealed class Appointment : EntityBase
{
    public int PatientId { get; set; }
    public int ClientId { get; set; }
    public int ClinicianUserId { get; set; }
    public int AppointmentTypeId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public AppointmentStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Patient? Patient { get; set; }
    public Client? Client { get; set; }
    public User? Clinician { get; set; }
    public AppointmentType? AppointmentType { get; set; }
    public Consult? Consult { get; set; }
}

public sealed class DiagnosisCatalogItem : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }
}

public sealed class ProcedureCatalogItem : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int? DefaultProductId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedByUserId { get; set; }

    public Product? DefaultProduct { get; set; }
    public User? CreatedByUser { get; set; }
}

public sealed class VaccinationType : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public int DefaultIntervalMonths { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Consult : EntityBase
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int ClinicianUserId { get; set; }
    public string VisitReason { get; set; } = string.Empty;
    public string? PresentingComplaint { get; set; }
    public string? SubjectiveNotes { get; set; }
    public string? ObjectiveNotes { get; set; }
    public string? AssessmentNotes { get; set; }
    public string? PlanNotes { get; set; }
    public string? FollowUpNotes { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? HeartRateBpm { get; set; }
    public int? RespiratoryRateBpm { get; set; }
    public ConsultStatus Status { get; set; } = ConsultStatus.Draft;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public Appointment? Appointment { get; set; }
    public Patient? Patient { get; set; }
    public User? Clinician { get; set; }
    public ICollection<ConsultDiagnosis> Diagnoses { get; set; } = new List<ConsultDiagnosis>();
    public ICollection<ConsultProcedure> Procedures { get; set; } = new List<ConsultProcedure>();
    public ICollection<ConsultMedication> Medications { get; set; } = new List<ConsultMedication>();
    public ICollection<PatientVaccinationRecord> Vaccinations { get; set; } = new List<PatientVaccinationRecord>();
    public Invoice? Invoice { get; set; }
}

public sealed class ConsultDiagnosis : EntityBase
{
    public int ConsultId { get; set; }
    public int? DiagnosisCatalogItemId { get; set; }
    public string DiagnosisText { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Consult? Consult { get; set; }
    public DiagnosisCatalogItem? DiagnosisCatalogItem { get; set; }
}

public sealed class ConsultProcedure : EntityBase
{
    public int ConsultId { get; set; }
    public int? ProcedureCatalogItemId { get; set; }
    public string ProcedureText { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public string? Notes { get; set; }
    public int SortOrder { get; set; }

    public Consult? Consult { get; set; }
    public ProcedureCatalogItem? ProcedureCatalogItem { get; set; }
}

public sealed class ConsultMedication : EntityBase
{
    public int ConsultId { get; set; }
    public int ProductId { get; set; }
    public int? InventoryLotId { get; set; }
    public decimal Quantity { get; set; }
    public string? DoseInstructions { get; set; }
    public string? Notes { get; set; }

    public Consult? Consult { get; set; }
    public Product? Product { get; set; }
    public InventoryLot? InventoryLot { get; set; }
}

public sealed class PatientVaccinationRecord : EntityBase
{
    public int PatientId { get; set; }
    public int? ConsultId { get; set; }
    public int VaccinationTypeId { get; set; }
    public int? ProductId { get; set; }
    public int? InventoryLotId { get; set; }
    public DateOnly DateAdministered { get; set; }
    public DateOnly DueDate { get; set; }
    public string? Notes { get; set; }

    public Patient? Patient { get; set; }
    public Consult? Consult { get; set; }
    public VaccinationType? VaccinationType { get; set; }
    public Product? Product { get; set; }
    public InventoryLot? InventoryLot { get; set; }
}

public sealed class Invoice : EntityBase
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public int PatientId { get; set; }
    public int? ConsultId { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTimeOffset? IssuedAt { get; set; }
    public decimal SubtotalExGst { get; set; }
    public decimal TotalGst { get; set; }
    public decimal TotalIncGst { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }

    public Client? Client { get; set; }
    public Patient? Patient { get; set; }
    public Consult? Consult { get; set; }
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public sealed class InvoiceLine : EntityBase
{
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public InvoiceLineType LineType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPriceExGst { get; set; }
    public decimal LineSubtotalExGst { get; set; }
    public decimal GstRate { get; set; }
    public decimal GstAmount { get; set; }
    public decimal LineTotalIncGst { get; set; }
    public bool IsTaxable { get; set; }
    public int? InventoryLotId { get; set; }
    // True only when adding this line took stock out; removing it then puts stock back.
    public bool StockDispensed { get; set; }

    public Invoice? Invoice { get; set; }
    public Product? Product { get; set; }
    public InventoryLot? InventoryLot { get; set; }
}

public sealed class Payment : EntityBase
{
    public int InvoiceId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Invoice? Invoice { get; set; }
}

public sealed class Product : EntityBase
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProductType Type { get; set; }
    public string? Description { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public bool TrackInventory { get; set; }
    public bool IsTaxable { get; set; } = true;
    public decimal DefaultGstRate { get; set; } = 0.10m;
    public decimal DefaultSalePriceExGst { get; set; }
    public decimal DefaultCostPriceExGst { get; set; }
    public decimal ReorderThreshold { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InventoryLot> Lots { get; set; } = new List<InventoryLot>();
}

public sealed class InventoryLot : EntityBase
{
    public int ProductId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public decimal UnitCostExGst { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityOnHand { get; set; }

    public Product? Product { get; set; }
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}

public sealed class InventoryTransaction : EntityBase
{
    public int ProductId { get; set; }
    public int? InventoryLotId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public decimal QuantityDelta { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int? CreatedByUserId { get; set; }

    public Product? Product { get; set; }
    public InventoryLot? InventoryLot { get; set; }
    public User? CreatedByUser { get; set; }
}

public sealed class Vendor : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactName { get; set; }
    public string? Notes { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

public sealed class PurchaseOrder : EntityBase
{
    public string OrderNumber { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTimeOffset OrderedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpectedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public string? Notes { get; set; }

    public Vendor? Vendor { get; set; }
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}

public sealed class PurchaseOrderLine : EntityBase
{
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCostExGst { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
    public Product? Product { get; set; }
}
