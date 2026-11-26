using FluentValidation;
using PawAndOrder.Domain;

namespace PawAndOrder.Application;

public sealed record LoginRequest(string Username, string Password);

public sealed record ClientUpsertRequest(
    string FirstName,
    string LastName,
    string Phone,
    string? Email,
    string? AddressLine1,
    string? AddressLine2,
    string? Suburb,
    string? State,
    string? Postcode,
    string? Notes);

public sealed record PatientUpsertRequest(
    int ClientId,
    string Name,
    string Species,
    string? Breed,
    string? Sex,
    DateOnly? DateOfBirth,
    int? EstimatedAgeYears,
    string? Color,
    string? MicrochipNumber,
    decimal WeightKg,
    bool IsDesexed,
    string? AlertsSummary,
    string? AllergiesSummary,
    string? Notes);

public sealed record AppointmentUpsertRequest(
    int PatientId,
    int ClientId,
    int ClinicianUserId,
    int AppointmentTypeId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    AppointmentStatus Status,
    string Reason,
    string? Notes);

public sealed record ConsultUpsertRequest(
    int AppointmentId,
    int PatientId,
    int ClinicianUserId,
    string VisitReason,
    string? PresentingComplaint,
    string? SubjectiveNotes,
    string? ObjectiveNotes,
    string? AssessmentNotes,
    string? PlanNotes,
    string? FollowUpNotes,
    decimal? WeightKg,
    decimal? TemperatureC,
    int? HeartRateBpm,
    int? RespiratoryRateBpm);

public sealed record CatalogEntryRequest(string Name, string? Code, int? DefaultProductId);

public sealed record ConsultDiagnosisRequest(int? DiagnosisCatalogItemId, string DiagnosisText, bool SaveToCatalog);
public sealed record ConsultProcedureRequest(int? ProcedureCatalogItemId, string ProcedureText, decimal Quantity, string? Notes, bool SaveToCatalog, int? DefaultProductId);
public sealed record ConsultMedicationRequest(int ProductId, int? InventoryLotId, decimal Quantity, string? DoseInstructions, string? Notes);
public sealed record ConsultVaccinationRequest(int VaccinationTypeId, int? ProductId, int? InventoryLotId, DateOnly DateAdministered, DateOnly DueDate, string? Notes);

public sealed record DirectInvoiceCreateRequest(int ClientId, int PatientId);
public sealed record InvoiceLineRequest(int? ProductId, string Description, InvoiceLineType LineType, decimal Quantity, decimal UnitPriceExGst, bool IsTaxable, int? InventoryLotId);
public sealed record PaymentRequest(PaymentMethod PaymentMethod, decimal Amount, string? Reference, string? Notes);

public sealed record ProductUpsertRequest(
    string Sku,
    string Name,
    ProductType Type,
    string? Description,
    string UnitOfMeasure,
    bool TrackInventory,
    bool IsTaxable,
    decimal DefaultSalePriceExGst,
    decimal DefaultCostPriceExGst,
    decimal ReorderThreshold);

public sealed record VendorUpsertRequest(string Name, string? Phone, string? Email, string? ContactName, string? Notes);
public sealed record PurchaseOrderLineRequest(int ProductId, decimal QuantityOrdered, decimal UnitCostExGst, string? BatchNumber, DateOnly? ExpiryDate);
public sealed record PurchaseOrderUpsertRequest(int VendorId, DateTimeOffset? ExpectedAt, string? Notes, List<PurchaseOrderLineRequest> Lines);
public sealed record ReceivePurchaseOrderLineRequest(int PurchaseOrderLineId, decimal QuantityReceived, string BatchNumber, DateOnly? ExpiryDate);
public sealed record ReceivePurchaseOrderRequest(List<ReceivePurchaseOrderLineRequest> Lines);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class ClientUpsertRequestValidator : AbstractValidator<ClientUpsertRequest>
{
    public ClientUpsertRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30)
            .Matches(@"^(?:\+61|0)[2-478](?:\s?\d){8}$").WithMessage("Phone must be a valid Australian number, e.g. 0412 345 678.");
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Suburb).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().Must(s => AustralianStates.Contains(s))
            .WithMessage("State must be one of NSW, VIC, QLD, SA, WA, TAS, NT, ACT.");
        RuleFor(x => x.Postcode).NotEmpty().Matches(@"^\d{4}$").WithMessage("Postcode must be 4 digits.");
    }

    private static readonly string[] AustralianStates = ["NSW", "VIC", "QLD", "SA", "WA", "TAS", "NT", "ACT"];
}

public sealed class PatientUpsertRequestValidator : AbstractValidator<PatientUpsertRequest>
{
    public PatientUpsertRequestValidator()
    {
        RuleFor(x => x.ClientId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Species).NotEmpty().MaximumLength(50);
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0);
    }
}

public sealed class AppointmentUpsertRequestValidator : AbstractValidator<AppointmentUpsertRequest>
{
    public AppointmentUpsertRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.ClientId).GreaterThan(0);
        RuleFor(x => x.ClinicianUserId).GreaterThan(0);
        RuleFor(x => x.AppointmentTypeId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt);
    }
}

public sealed class ConsultUpsertRequestValidator : AbstractValidator<ConsultUpsertRequest>
{
    public ConsultUpsertRequestValidator()
    {
        RuleFor(x => x.AppointmentId).GreaterThan(0);
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.ClinicianUserId).GreaterThan(0);
        RuleFor(x => x.VisitReason).NotEmpty();
    }
}

public sealed class ConsultDiagnosisRequestValidator : AbstractValidator<ConsultDiagnosisRequest>
{
    public ConsultDiagnosisRequestValidator()
    {
        RuleFor(x => x.DiagnosisText).NotEmpty();
    }
}

public sealed class ConsultProcedureRequestValidator : AbstractValidator<ConsultProcedureRequest>
{
    public ConsultProcedureRequestValidator()
    {
        RuleFor(x => x.ProcedureText).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class ConsultMedicationRequestValidator : AbstractValidator<ConsultMedicationRequest>
{
    public ConsultMedicationRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class ConsultVaccinationRequestValidator : AbstractValidator<ConsultVaccinationRequest>
{
    public ConsultVaccinationRequestValidator()
    {
        RuleFor(x => x.VaccinationTypeId).GreaterThan(0);
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.DateAdministered);
    }
}

public sealed class ProductUpsertRequestValidator : AbstractValidator<ProductUpsertRequest>
{
    public ProductUpsertRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.UnitOfMeasure).NotEmpty();
        RuleFor(x => x.DefaultSalePriceExGst).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DefaultCostPriceExGst).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderThreshold).GreaterThanOrEqualTo(0);
    }
}

public sealed class DirectInvoiceCreateRequestValidator : AbstractValidator<DirectInvoiceCreateRequest>
{
    public DirectInvoiceCreateRequestValidator()
    {
        RuleFor(x => x.ClientId).GreaterThan(0);
        RuleFor(x => x.PatientId).GreaterThan(0);
    }
}

public sealed class InvoiceLineRequestValidator : AbstractValidator<InvoiceLineRequest>
{
    public InvoiceLineRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPriceExGst).GreaterThanOrEqualTo(0);
    }
}

public sealed class VendorUpsertRequestValidator : AbstractValidator<VendorUpsertRequest>
{
    public VendorUpsertRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public sealed class PaymentRequestValidator : AbstractValidator<PaymentRequest>
{
    public PaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class PurchaseOrderUpsertRequestValidator : AbstractValidator<PurchaseOrderUpsertRequest>
{
    public PurchaseOrderUpsertRequestValidator()
    {
        RuleFor(x => x.VendorId).GreaterThan(0);
        RuleForEach(x => x.Lines).SetValidator(new PurchaseOrderLineRequestValidator());
    }
}

public sealed class PurchaseOrderLineRequestValidator : AbstractValidator<PurchaseOrderLineRequest>
{
    public PurchaseOrderLineRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.QuantityOrdered).GreaterThan(0);
        RuleFor(x => x.UnitCostExGst).GreaterThanOrEqualTo(0);
    }
}

public sealed class ReceivePurchaseOrderRequestValidator : AbstractValidator<ReceivePurchaseOrderRequest>
{
    public ReceivePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new ReceivePurchaseOrderLineRequestValidator());
    }
}

public sealed class ReceivePurchaseOrderLineRequestValidator : AbstractValidator<ReceivePurchaseOrderLineRequest>
{
    public ReceivePurchaseOrderLineRequestValidator()
    {
        RuleFor(x => x.PurchaseOrderLineId).GreaterThan(0);
        RuleFor(x => x.QuantityReceived).GreaterThan(0);
        RuleFor(x => x.BatchNumber).NotEmpty();
    }
}

public static class PricingMath
{
    public const decimal GstRate = 0.10m;

    public static (decimal subtotalExGst, decimal gstAmount, decimal totalIncGst) Calculate(decimal quantity, decimal unitPriceExGst, bool isTaxable)
    {
        var subtotal = decimal.Round(quantity * unitPriceExGst, 2, MidpointRounding.AwayFromZero);
        var gstAmount = isTaxable
            ? decimal.Round(subtotal * GstRate, 2, MidpointRounding.AwayFromZero)
            : 0m;
        return (subtotal, gstAmount, subtotal + gstAmount);
    }
}

public static class PasswordHasher
{
    public static string Hash(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool Verify(string value, string hash) => Hash(value) == hash;
}
