using PawAndOrder.Api.Endpoints;

namespace PawAndOrder.Api;

public static class ApiRoutes
{
    public static IEndpointRouteBuilder MapClinicApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapAuthEndpoints();
        api.MapLookupEndpoints();
        api.MapClientEndpoints();
        api.MapPatientEndpoints();
        api.MapAppointmentEndpoints();
        api.MapConsultEndpoints();
        api.MapInvoiceEndpoints();
        api.MapProductEndpoints();
        api.MapInventoryEndpoints();
        api.MapVendorEndpoints();
        api.MapPurchaseOrderEndpoints();
        api.MapReportEndpoints();

        return app;
    }
}
