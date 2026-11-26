using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Application;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api;

public static class ApiRoutes
{
    public static IEndpointRouteBuilder MapClinicApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        MapAuth(api);
        MapLookups(api);
        MapClients(api);
        MapPatients(api);
        MapAppointments(api);
        MapConsults(api);
        MapInvoices(api);
        MapProducts(api);
        MapInventory(api);
        MapVendors(api);
        MapPurchaseOrders(api);
        MapReports(api);

        return app;
    }

    private static void MapAuth(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/auth");

        group.MapPost("/login", async (LoginRequest request, IValidator<LoginRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == request.Username && x.IsActive, cancellationToken);
            if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                token = $"demo-{user.Username}",
                user = new { user.Id, user.Username, user.DisplayName, user.Role },
            });
        });

        group.MapGet("/me", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var user = await db.Users.AsNoTracking().OrderBy(x => x.Id).FirstAsync(cancellationToken);
            return Results.Ok(new { user.Id, user.Username, user.DisplayName, user.Role });
        });
    }

    private static void MapLookups(RouteGroupBuilder api)
    {
        api.MapGet("/lookups/bootstrap", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var clinicians = await db.Users.AsNoTracking()
                .Where(x => x.Role == UserRole.Veterinarian && x.IsActive)
                .OrderBy(x => x.DisplayName)
                .Select(x => new { x.Id, x.DisplayName })
                .ToListAsync(cancellationToken);

            var appointmentTypes = await db.AppointmentTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.DurationMinutes, x.ColorHex })
                .ToListAsync(cancellationToken);

            var diagnoses = await db.DiagnosisCatalogItems.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Code })
                .ToListAsync(cancellationToken);

            var procedures = await db.ProcedureCatalogItems.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Code, x.DefaultProductId })
                .ToListAsync(cancellationToken);

            var vaccinationTypes = await db.VaccinationTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Species, x.DefaultIntervalMonths })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { clinicians, appointmentTypes, diagnoses, procedures, vaccinationTypes });
        });
    }

    private static void MapClients(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/clients");

        group.MapGet("", async (string? search, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<Client> query = db.Clients.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    (x.FirstName + " " + x.LastName).Contains(search) ||
                    x.Phone.Contains(search) ||
                    (x.Email != null && x.Email.Contains(search)));
            }

            var clients = await query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    displayName = x.FirstName + " " + x.LastName,
                    x.Phone,
                    x.Email,
                    patientCount = x.Patients.Count,
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items = clients });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var client = await db.Clients.AsNoTracking()
                .Include(x => x.Patients)
                .Include(x => x.Invoices)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (client is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                client.Id,
                client.FirstName,
                client.LastName,
                displayName = client.FirstName + " " + client.LastName,
                client.Phone,
                client.Email,
                client.AddressLine1,
                client.AddressLine2,
                client.Suburb,
                client.State,
                client.Postcode,
                client.Notes,
                patients = client.Patients.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Species, x.Breed, x.WeightKg }),
                invoices = client.Invoices.OrderByDescending(x => x.IssuedAt).Take(8).Select(x => new { x.Id, x.InvoiceNumber, x.Status, x.TotalIncGst, x.BalanceDue, x.IssuedAt }),
            });
        });

        group.MapPost("", async (ClientUpsertRequest request, IValidator<ClientUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var client = new Client
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                Email = request.Email,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                Suburb = request.Suburb,
                State = request.State,
                Postcode = request.Postcode,
                Notes = request.Notes,
            };

            db.Clients.Add(client);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/clients/{client.Id}", new { client.Id });
        });

        group.MapPut("/{id:int}", async (int id, ClientUpsertRequest request, IValidator<ClientUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var client = await db.Clients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (client is null)
            {
                return Results.NotFound();
            }

            client.FirstName = request.FirstName;
            client.LastName = request.LastName;
            client.Phone = request.Phone;
            client.Email = request.Email;
            client.AddressLine1 = request.AddressLine1;
            client.AddressLine2 = request.AddressLine2;
            client.Suburb = request.Suburb;
            client.State = request.State;
            client.Postcode = request.Postcode;
            client.Notes = request.Notes;
            client.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { client.Id });
        });
    }

    private static void MapPatients(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/patients");

        group.MapGet("", async (string? search, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<Patient> query = db.Patients.AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.VaccinationRecords);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Name.Contains(search) ||
                    x.Species.Contains(search) ||
                    (x.Client != null && (x.Client.FirstName + " " + x.Client.LastName).Contains(search)));
            }

            var patients = await query
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.ClientId,
                    x.Name,
                    x.Species,
                    x.Breed,
                    x.WeightKg,
                    ownerName = x.Client != null ? x.Client.FirstName + " " + x.Client.LastName : string.Empty,
                    nextVaccinationDue = x.VaccinationRecords.OrderBy(v => v.DueDate).Select(v => (DateOnly?)v.DueDate).FirstOrDefault(),
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items = patients });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var patient = await db.Patients.AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.Appointments).ThenInclude(x => x.Clinician)
                .Include(x => x.Consults)
                .Include(x => x.Invoices)
                .Include(x => x.VaccinationRecords).ThenInclude(v => v.VaccinationType)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (patient is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                patient.Id,
                patient.Name,
                patient.Species,
                patient.Breed,
                patient.Sex,
                patient.DateOfBirth,
                patient.EstimatedAgeYears,
                patient.Color,
                patient.MicrochipNumber,
                patient.WeightKg,
                patient.IsDesexed,
                patient.AlertsSummary,
                patient.AllergiesSummary,
                patient.Notes,
                owner = patient.Client is null ? null : new { patient.Client.Id, displayName = patient.Client.FirstName + " " + patient.Client.LastName, patient.Client.Phone },
                appointments = patient.Appointments.OrderByDescending(x => x.StartAt).Take(8).Select(x => new { x.Id, x.StartAt, x.Status, x.Reason, clinician = x.Clinician!.DisplayName }),
                consults = patient.Consults.OrderByDescending(x => x.StartedAt).Take(8).Select(x => new { x.Id, x.VisitReason, x.Status, x.StartedAt, x.CompletedAt }),
                invoices = patient.Invoices.OrderByDescending(x => x.IssuedAt).Take(8).Select(x => new { x.Id, x.InvoiceNumber, x.Status, x.TotalIncGst, x.BalanceDue, x.IssuedAt }),
                vaccinations = patient.VaccinationRecords.OrderBy(x => x.DueDate).Select(x => new { x.Id, name = x.VaccinationType!.Name, x.DateAdministered, x.DueDate, x.Notes }),
            });
        });

        group.MapPost("", async (PatientUpsertRequest request, IValidator<PatientUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var patient = new Patient
            {
                ClientId = request.ClientId,
                Name = request.Name,
                Species = request.Species,
                Breed = request.Breed,
                Sex = request.Sex,
                DateOfBirth = request.DateOfBirth,
                EstimatedAgeYears = request.EstimatedAgeYears,
                Color = request.Color,
                MicrochipNumber = request.MicrochipNumber,
                WeightKg = request.WeightKg,
                IsDesexed = request.IsDesexed,
                AlertsSummary = request.AlertsSummary,
                AllergiesSummary = request.AllergiesSummary,
                Notes = request.Notes,
            };

            db.Patients.Add(patient);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/patients/{patient.Id}", new { patient.Id });
        });

        group.MapPut("/{id:int}", async (int id, PatientUpsertRequest request, IValidator<PatientUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var patient = await db.Patients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (patient is null)
            {
                return Results.NotFound();
            }

            patient.ClientId = request.ClientId;
            patient.Name = request.Name;
            patient.Species = request.Species;
            patient.Breed = request.Breed;
            patient.Sex = request.Sex;
            patient.DateOfBirth = request.DateOfBirth;
            patient.EstimatedAgeYears = request.EstimatedAgeYears;
            patient.Color = request.Color;
            patient.MicrochipNumber = request.MicrochipNumber;
            patient.WeightKg = request.WeightKg;
            patient.IsDesexed = request.IsDesexed;
            patient.AlertsSummary = request.AlertsSummary;
            patient.AllergiesSummary = request.AllergiesSummary;
            patient.Notes = request.Notes;
            patient.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { patient.Id });
        });
    }

    private static void MapAppointments(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/appointments");

        group.MapGet("", async (DateTimeOffset? from, DateTimeOffset? to, int? clinicianId, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var rangeStart = from ?? DateTimeOffset.UtcNow.Date;
            var rangeEnd = to ?? rangeStart.AddDays(7);

            IQueryable<Appointment> query = db.Appointments.AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Client)
                .Include(x => x.Clinician)
                .Include(x => x.AppointmentType)
                .Where(x => x.StartAt >= rangeStart && x.StartAt <= rangeEnd);

            if (clinicianId.HasValue)
            {
                query = query.Where(x => x.ClinicianUserId == clinicianId.Value);
            }

            var items = await query.OrderBy(x => x.StartAt)
                .Select(x => new
                {
                    x.Id,
                    x.StartAt,
                    x.EndAt,
                    x.Status,
                    x.Reason,
                    x.Notes,
                    patient = x.Patient!.Name,
                    client = x.Client!.FirstName + " " + x.Client!.LastName,
                    clinician = x.Clinician!.DisplayName,
                    appointmentType = x.AppointmentType!.Name,
                    appointmentTypeColor = x.AppointmentType!.ColorHex,
                    consultId = x.Consult != null ? x.Consult.Id : (int?)null,
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapPost("", async (AppointmentUpsertRequest request, IValidator<AppointmentUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var appointment = new Appointment
            {
                PatientId = request.PatientId,
                ClientId = request.ClientId,
                ClinicianUserId = request.ClinicianUserId,
                AppointmentTypeId = request.AppointmentTypeId,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                Status = request.Status,
                Reason = request.Reason,
                Notes = request.Notes,
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/appointments/{appointment.Id}", new { appointment.Id });
        });

        group.MapPut("/{id:int}", async (int id, AppointmentUpsertRequest request, IValidator<AppointmentUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            appointment.PatientId = request.PatientId;
            appointment.ClientId = request.ClientId;
            appointment.ClinicianUserId = request.ClinicianUserId;
            appointment.AppointmentTypeId = request.AppointmentTypeId;
            appointment.StartAt = request.StartAt;
            appointment.EndAt = request.EndAt;
            appointment.Status = request.Status;
            appointment.Reason = request.Reason;
            appointment.Notes = request.Notes;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { appointment.Id });
        });

        group.MapPost("/{id:int}/arrive", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            appointment.Status = AppointmentStatus.Arrived;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { appointment.Id, appointment.Status });
        });

        group.MapPost("/{id:int}/start-consult", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var appointment = await db.Appointments.Include(x => x.Consult).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            appointment.Status = AppointmentStatus.InConsult;
            if (appointment.Consult is null)
            {
                appointment.Consult = new Consult
                {
                    AppointmentId = appointment.Id,
                    PatientId = appointment.PatientId,
                    ClinicianUserId = appointment.ClinicianUserId,
                    VisitReason = appointment.Reason,
                    StartedAt = DateTimeOffset.UtcNow,
                };
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { consultId = appointment.Consult.Id });
        });

        group.MapPost("/{id:int}/cancel", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { appointment.Id, appointment.Status });
        });
    }

    private static void MapConsults(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/consults");

        group.MapPost("", async (ConsultUpsertRequest request, IValidator<ConsultUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = new Consult
            {
                AppointmentId = request.AppointmentId,
                PatientId = request.PatientId,
                ClinicianUserId = request.ClinicianUserId,
                VisitReason = request.VisitReason,
                PresentingComplaint = request.PresentingComplaint,
                SubjectiveNotes = request.SubjectiveNotes,
                ObjectiveNotes = request.ObjectiveNotes,
                AssessmentNotes = request.AssessmentNotes,
                PlanNotes = request.PlanNotes,
                FollowUpNotes = request.FollowUpNotes,
                WeightKg = request.WeightKg,
                TemperatureC = request.TemperatureC,
                HeartRateBpm = request.HeartRateBpm,
                RespiratoryRateBpm = request.RespiratoryRateBpm,
            };

            db.Consults.Add(consult);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/consults/{consult.Id}", new { consult.Id });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var consult = await db.Consults.AsNoTracking()
                .Include(x => x.Patient)!.ThenInclude(x => x!.Client)
                .Include(x => x.Clinician)
                .Include(x => x.Diagnoses)
                .Include(x => x.Procedures)
                .Include(x => x.Medications).ThenInclude(x => x.Product)
                .Include(x => x.Medications).ThenInclude(x => x.InventoryLot)
                .Include(x => x.Vaccinations).ThenInclude(x => x.VaccinationType)
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (consult is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                consult.Id,
                consult.AppointmentId,
                consult.PatientId,
                consult.ClinicianUserId,
                consult.VisitReason,
                consult.PresentingComplaint,
                consult.SubjectiveNotes,
                consult.ObjectiveNotes,
                consult.AssessmentNotes,
                consult.PlanNotes,
                consult.FollowUpNotes,
                consult.WeightKg,
                consult.TemperatureC,
                consult.HeartRateBpm,
                consult.RespiratoryRateBpm,
                consult.Status,
                consult.StartedAt,
                consult.CompletedAt,
                patient = consult.Patient is null ? null : new
                {
                    consult.Patient.Id,
                    consult.Patient.Name,
                    consult.Patient.Species,
                    consult.Patient.Breed,
                    consult.Patient.WeightKg,
                    owner = consult.Patient.Client is null ? null : consult.Patient.Client.FirstName + " " + consult.Patient.Client.LastName,
                },
                clinician = consult.Clinician?.DisplayName,
                diagnoses = consult.Diagnoses.OrderBy(x => x.SortOrder).Select(x => new { x.Id, x.DiagnosisCatalogItemId, x.DiagnosisText }),
                procedures = consult.Procedures.OrderBy(x => x.SortOrder).Select(x => new { x.Id, x.ProcedureCatalogItemId, x.ProcedureText, x.Quantity, x.Notes }),
                medications = consult.Medications.Select(x => new { x.Id, x.ProductId, productName = x.Product!.Name, x.InventoryLotId, lot = x.InventoryLot != null ? x.InventoryLot.BatchNumber : null, x.Quantity, x.DoseInstructions, x.Notes }),
                vaccinations = consult.Vaccinations.Select(x => new { x.Id, x.VaccinationTypeId, name = x.VaccinationType!.Name, x.DateAdministered, x.DueDate, x.InventoryLotId, x.Notes }),
                invoice = consult.Invoice is null ? null : new { consult.Invoice.Id, consult.Invoice.InvoiceNumber, consult.Invoice.Status, consult.Invoice.TotalIncGst },
            });
        });

        group.MapPut("/{id:int}", async (int id, ConsultUpsertRequest request, IValidator<ConsultUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            consult.VisitReason = request.VisitReason;
            consult.PresentingComplaint = request.PresentingComplaint;
            consult.SubjectiveNotes = request.SubjectiveNotes;
            consult.ObjectiveNotes = request.ObjectiveNotes;
            consult.AssessmentNotes = request.AssessmentNotes;
            consult.PlanNotes = request.PlanNotes;
            consult.FollowUpNotes = request.FollowUpNotes;
            consult.WeightKg = request.WeightKg;
            consult.TemperatureC = request.TemperatureC;
            consult.HeartRateBpm = request.HeartRateBpm;
            consult.RespiratoryRateBpm = request.RespiratoryRateBpm;
            consult.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { consult.Id });
        });

        group.MapPost("/{id:int}/diagnoses", async (int id, ConsultDiagnosisRequest request, IValidator<ConsultDiagnosisRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.Include(x => x.Diagnoses).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            var diagnosisCatalogItemId = request.DiagnosisCatalogItemId;
            if (request.SaveToCatalog && request.DiagnosisCatalogItemId is null)
            {
                var catalogItem = new DiagnosisCatalogItem { Name = request.DiagnosisText };
                db.DiagnosisCatalogItems.Add(catalogItem);
                await db.SaveChangesAsync(cancellationToken);
                diagnosisCatalogItemId = catalogItem.Id;
            }

            var item = new ConsultDiagnosis
            {
                ConsultId = id,
                DiagnosisCatalogItemId = diagnosisCatalogItemId,
                DiagnosisText = request.DiagnosisText,
                SortOrder = consult.Diagnoses.Count + 1,
            };
            db.ConsultDiagnoses.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { item.Id });
        });

        group.MapPost("/{id:int}/procedures", async (int id, ConsultProcedureRequest request, IValidator<ConsultProcedureRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.Include(x => x.Procedures).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            var procedureCatalogItemId = request.ProcedureCatalogItemId;
            if (request.SaveToCatalog && request.ProcedureCatalogItemId is null)
            {
                var catalogItem = new ProcedureCatalogItem { Name = request.ProcedureText, DefaultProductId = request.DefaultProductId };
                db.ProcedureCatalogItems.Add(catalogItem);
                await db.SaveChangesAsync(cancellationToken);
                procedureCatalogItemId = catalogItem.Id;
            }

            var item = new ConsultProcedure
            {
                ConsultId = id,
                ProcedureCatalogItemId = procedureCatalogItemId,
                ProcedureText = request.ProcedureText,
                Quantity = request.Quantity,
                Notes = request.Notes,
                SortOrder = consult.Procedures.Count + 1,
            };

            db.ConsultProcedures.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { item.Id });
        });

        group.MapPost("/{id:int}/medications", async (int id, ConsultMedicationRequest request, IValidator<ConsultMedicationRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            var product = await db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
            if (product is null)
            {
                return Results.BadRequest("Selected product could not be found.");
            }

            InventoryLot? lot = null;
            if (product.TrackInventory)
            {
                lot = await ResolveLotAsync(db, product.Id, request.InventoryLotId, request.Quantity, cancellationToken);
                if (lot is null)
                {
                    return Results.BadRequest("Unable to allocate stock for the selected medication.");
                }

                lot.QuantityOnHand -= request.Quantity;
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    InventoryLotId = lot.Id,
                    Type = InventoryTransactionType.Dispense,
                    QuantityDelta = -request.Quantity,
                    ReferenceType = "ConsultMedication",
                    ReferenceId = id.ToString(),
                    Notes = request.Notes,
                    CreatedByUserId = consult.ClinicianUserId,
                });
            }

            var item = new ConsultMedication
            {
                ConsultId = id,
                ProductId = product.Id,
                InventoryLotId = lot?.Id,
                Quantity = request.Quantity,
                DoseInstructions = request.DoseInstructions,
                Notes = request.Notes,
            };

            db.ConsultMedications.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { item.Id });
        });

        group.MapPost("/{id:int}/vaccinations", async (int id, ConsultVaccinationRequest request, IValidator<ConsultVaccinationRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            InventoryLot? lot = null;
            if (request.ProductId.HasValue)
            {
                var product = await db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId.Value, cancellationToken);
                if (product is null)
                {
                    return Results.BadRequest("Selected vaccination stock product could not be found.");
                }

                if (product.TrackInventory)
                {
                    lot = await ResolveLotAsync(db, product.Id, request.InventoryLotId, 1m, cancellationToken);
                    if (lot is null)
                    {
                        return Results.BadRequest("Unable to allocate stock for the selected vaccination.");
                    }

                    lot.QuantityOnHand -= 1m;
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        InventoryLotId = lot.Id,
                        Type = InventoryTransactionType.Dispense,
                        QuantityDelta = -1m,
                        ReferenceType = "Vaccination",
                        ReferenceId = id.ToString(),
                        Notes = request.Notes,
                        CreatedByUserId = consult.ClinicianUserId,
                    });
                }
            }

            var item = new PatientVaccinationRecord
            {
                PatientId = consult.PatientId,
                ConsultId = id,
                VaccinationTypeId = request.VaccinationTypeId,
                ProductId = request.ProductId,
                InventoryLotId = lot?.Id,
                DateAdministered = request.DateAdministered,
                DueDate = request.DueDate,
                Notes = request.Notes,
            };

            db.PatientVaccinationRecords.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { item.Id });
        });

        group.MapPost("/{id:int}/finalize", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var consult = await db.Consults.Include(x => x.Appointment).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            consult.Status = ConsultStatus.Finalized;
            consult.CompletedAt = DateTimeOffset.UtcNow;
            if (consult.Appointment is not null)
            {
                consult.Appointment.Status = AppointmentStatus.Completed;
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { consult.Id, consult.Status });
        });

        group.MapPost("/{id:int}/generate-invoice", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var consult = await db.Consults
                .Include(x => x.Patient)
                .Include(x => x.Appointment)
                .Include(x => x.Procedures).ThenInclude(x => x.ProcedureCatalogItem)
                .Include(x => x.Medications).ThenInclude(x => x.Product)
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (consult is null || consult.Patient is null || consult.Appointment is null)
            {
                return Results.NotFound();
            }

            if (consult.Invoice is not null)
            {
                return Results.Ok(new { consult.Invoice.Id, consult.Invoice.InvoiceNumber });
            }

            var invoiceCount = await db.Invoices.CountAsync(cancellationToken);
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{1000 + invoiceCount + 1}",
                ClientId = consult.Patient.ClientId,
                PatientId = consult.PatientId,
                ConsultId = consult.Id,
                Status = InvoiceStatus.Draft,
                IssuedAt = DateTimeOffset.UtcNow,
            };

            foreach (var procedure in consult.Procedures)
            {
                Product? product = null;
                if (procedure.ProcedureCatalogItem?.DefaultProductId is int defaultProductId)
                {
                    product = await db.Products.FirstOrDefaultAsync(x => x.Id == defaultProductId, cancellationToken);
                }

                product ??= await db.Products.FirstAsync(x => x.Sku == "SRV-CONSULT", cancellationToken);
                invoice.Lines.Add(BuildInvoiceLine(product, procedure.Quantity, null, procedure.ProcedureText));
            }

            if (!invoice.Lines.Any())
            {
                var consultProduct = await db.Products.FirstAsync(x => x.Sku == "SRV-CONSULT", cancellationToken);
                invoice.Lines.Add(BuildInvoiceLine(consultProduct, 1m, null));
            }

            foreach (var medication in consult.Medications)
            {
                if (medication.Product is not null)
                {
                    invoice.Lines.Add(BuildInvoiceLine(medication.Product, medication.Quantity, medication.InventoryLotId));
                }
            }

            SeedData.RecalculateInvoice(invoice);
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { invoice.Id, invoice.InvoiceNumber });
        });
    }

    private static void MapInvoices(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/invoices");

        group.MapPost("", async (DirectInvoiceCreateRequest request, IValidator<DirectInvoiceCreateRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PatientId && x.ClientId == request.ClientId, cancellationToken);
            if (patient is null)
            {
                return Results.BadRequest("The selected patient does not belong to the selected client.");
            }

            var invoiceCount = await db.Invoices.CountAsync(cancellationToken);
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{1000 + invoiceCount + 1}",
                ClientId = request.ClientId,
                PatientId = request.PatientId,
                Status = InvoiceStatus.Draft,
                IssuedAt = DateTimeOffset.UtcNow,
            };

            SeedData.RecalculateInvoice(invoice);
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/invoices/{invoice.Id}", new { invoice.Id, invoice.InvoiceNumber });
        });

        group.MapGet("", async (string? search, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<Invoice> query = db.Invoices.AsNoTracking().Include(x => x.Client).Include(x => x.Patient);
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.InvoiceNumber.Contains(search) || x.Patient!.Name.Contains(search) || (x.Client!.FirstName + " " + x.Client.LastName).Contains(search));
            }

            var items = await query.OrderByDescending(x => x.IssuedAt).Take(50)
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNumber,
                    x.Status,
                    x.TotalIncGst,
                    x.BalanceDue,
                    x.IssuedAt,
                    patient = x.Patient!.Name,
                    client = x.Client!.FirstName + " " + x.Client!.LastName,
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var invoice = await db.Invoices.AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.Patient)
                .Include(x => x.Lines).ThenInclude(x => x.InventoryLot)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                invoice.IssuedAt,
                invoice.SubtotalExGst,
                invoice.TotalGst,
                invoice.TotalIncGst,
                invoice.AmountPaid,
                invoice.BalanceDue,
                client = invoice.Client is null ? null : invoice.Client.FirstName + " " + invoice.Client.LastName,
                patient = invoice.Patient?.Name,
                lines = invoice.Lines.Select(x => new { x.Id, x.Description, x.LineType, x.Quantity, x.UnitPriceExGst, x.LineSubtotalExGst, x.GstAmount, x.LineTotalIncGst, lot = x.InventoryLot != null ? x.InventoryLot.BatchNumber : null }),
                payments = invoice.Payments.OrderByDescending(x => x.PaidAt).Select(x => new { x.Id, x.PaymentMethod, x.Amount, x.PaidAt, x.Reference, x.Notes }),
            });
        });

        group.MapPost("/{id:int}/lines", async (int id, InvoiceLineRequest request, IValidator<InvoiceLineRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var invoice = await db.Invoices.Include(x => x.Lines).Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            Product? product = null;
            InventoryLot? lot = null;
            if (request.ProductId.HasValue)
            {
                product = await db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId.Value, cancellationToken);
                if (product is null)
                {
                    return Results.BadRequest("The selected product was not found.");
                }

                if (product.TrackInventory)
                {
                    lot = await ResolveLotAsync(db, product.Id, request.InventoryLotId, request.Quantity, cancellationToken);
                    if (lot is null)
                    {
                        return Results.BadRequest("Unable to allocate stock for the selected line item.");
                    }

                    lot.QuantityOnHand -= request.Quantity;
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        InventoryLotId = lot.Id,
                        Type = InventoryTransactionType.Dispense,
                        QuantityDelta = -request.Quantity,
                        ReferenceType = "InvoiceLine",
                        ReferenceId = invoice.Id.ToString(),
                        Notes = $"POS line added to {invoice.InvoiceNumber}",
                    });
                }
            }

            var isTaxable = product?.IsTaxable ?? request.IsTaxable;
            var unitPriceExGst = product?.DefaultSalePriceExGst ?? request.UnitPriceExGst;
            var description = string.IsNullOrWhiteSpace(request.Description) ? product?.Name ?? string.Empty : request.Description;
            var lineType = product is not null ? (product.TrackInventory ? InvoiceLineType.Product : InvoiceLineType.Service) : request.LineType;

            var line = BuildInvoiceLineFromValues(product?.Id, description, lineType, request.Quantity, unitPriceExGst, isTaxable, lot?.Id);
            line.StockDispensed = lot is not null;
            invoice.Lines.Add(line);
            SeedData.RecalculateInvoice(invoice);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { lineId = line.Id, invoiceId = invoice.Id, invoice.TotalIncGst, invoice.BalanceDue });
        });

        group.MapDelete("/{id:int}/lines/{lineId:int}", async (int id, int lineId, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var invoice = await db.Invoices.Include(x => x.Lines).Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            var line = invoice.Lines.FirstOrDefault(x => x.Id == lineId);
            if (line is null)
            {
                return Results.NotFound();
            }

            if (line.StockDispensed && line.ProductId.HasValue && line.InventoryLotId.HasValue)
            {
                var lot = await db.InventoryLots.FirstOrDefaultAsync(x => x.Id == line.InventoryLotId.Value, cancellationToken);
                if (lot is not null)
                {
                    lot.QuantityOnHand += line.Quantity;
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = line.ProductId.Value,
                        InventoryLotId = lot.Id,
                        Type = InventoryTransactionType.Correction,
                        QuantityDelta = line.Quantity,
                        ReferenceType = "InvoiceLineRemoval",
                        ReferenceId = invoice.Id.ToString(),
                        Notes = $"Removed line from {invoice.InvoiceNumber}",
                    });
                }
            }

            db.InvoiceLines.Remove(line);
            SeedData.RecalculateInvoice(invoice);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { invoice.Id, invoice.TotalIncGst, invoice.BalanceDue });
        });

        group.MapPost("/{id:int}/payments", async (int id, PaymentRequest request, IValidator<PaymentRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var invoice = await db.Invoices.Include(x => x.Payments).Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            if (request.Amount > invoice.BalanceDue)
            {
                return Results.BadRequest("Payment amount exceeds the remaining balance.");
            }

            invoice.Payments.Add(new Payment
            {
                InvoiceId = invoice.Id,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                PaidAt = DateTimeOffset.UtcNow,
                Reference = request.Reference,
                Notes = request.Notes,
            });

            invoice.Status = InvoiceStatus.Issued;
            SeedData.RecalculateInvoice(invoice);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { invoice.Id, invoice.Status, invoice.BalanceDue });
        });

        group.MapPost("/{id:int}/issue", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            invoice.Status = InvoiceStatus.Issued;
            invoice.IssuedAt = invoice.IssuedAt ?? DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { invoice.Id, invoice.Status });
        });
    }

    private static void MapProducts(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/products");

        group.MapGet("", async (string? search, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<Product> query = db.Products.AsNoTracking().Include(x => x.Lots).Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Name.Contains(search) || x.Sku.Contains(search));
            }

            var items = await query.OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Sku,
                    x.Name,
                    x.Type,
                    x.TrackInventory,
                    x.DefaultSalePriceExGst,
                    x.ReorderThreshold,
                    quantityOnHand = x.Lots.Sum(l => l.QuantityOnHand),
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var product = await db.Products.AsNoTracking()
                .Include(x => x.Lots)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (product is null)
            {
                return Results.NotFound();
            }

            var transactions = await db.InventoryTransactions.AsNoTracking()
                .Where(x => x.ProductId == product.Id)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .Select(x => new { x.Id, x.Type, x.QuantityDelta, x.ReferenceType, x.ReferenceId, x.CreatedAt, x.InventoryLotId })
                .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                product.Id,
                product.Sku,
                product.Name,
                product.Type,
                product.Description,
                product.UnitOfMeasure,
                product.TrackInventory,
                product.IsTaxable,
                product.DefaultSalePriceExGst,
                product.DefaultCostPriceExGst,
                product.ReorderThreshold,
                quantityOnHand = product.Lots.Sum(x => x.QuantityOnHand),
                lots = product.Lots.OrderBy(x => x.ExpiryDate).ThenBy(x => x.ReceivedDate).Select(x => new { x.Id, x.BatchNumber, x.ExpiryDate, x.ReceivedDate, x.UnitCostExGst, x.QuantityReceived, x.QuantityOnHand }),
                transactions,
            });
        });

        group.MapPost("", async (ProductUpsertRequest request, IValidator<ProductUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var product = new Product
            {
                Sku = request.Sku,
                Name = request.Name,
                Type = request.Type,
                Description = request.Description,
                UnitOfMeasure = request.UnitOfMeasure,
                TrackInventory = request.TrackInventory,
                IsTaxable = request.IsTaxable,
                DefaultSalePriceExGst = request.DefaultSalePriceExGst,
                DefaultCostPriceExGst = request.DefaultCostPriceExGst,
                ReorderThreshold = request.ReorderThreshold,
            };

            db.Products.Add(product);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/products/{product.Id}", new { product.Id });
        });
    }

    private static void MapInventory(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/inventory");

        group.MapGet("/stock", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var items = await db.Products.AsNoTracking()
                .Include(x => x.Lots)
                .Where(x => x.TrackInventory)
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Sku,
                    x.Name,
                    x.Type,
                    x.ReorderThreshold,
                    quantityOnHand = x.Lots.Sum(l => l.QuantityOnHand),
                    nextExpiry = x.Lots.Where(l => l.QuantityOnHand > 0 && l.ExpiryDate != null).OrderBy(l => l.ExpiryDate).Select(l => l.ExpiryDate).FirstOrDefault(),
                    lowStock = x.Lots.Sum(l => l.QuantityOnHand) <= x.ReorderThreshold,
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/lots", async (int? productId, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<InventoryLot> query = db.InventoryLots.AsNoTracking().Include(x => x.Product);
            if (productId.HasValue)
            {
                query = query.Where(x => x.ProductId == productId.Value);
            }

            var items = await query.OrderBy(x => x.ExpiryDate).ThenBy(x => x.ReceivedDate)
                .Select(x => new { x.Id, x.ProductId, product = x.Product!.Name, x.BatchNumber, x.ExpiryDate, x.ReceivedDate, x.UnitCostExGst, x.QuantityReceived, x.QuantityOnHand })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapPost("/adjustments", async (int productId, int? inventoryLotId, decimal quantityDelta, string? notes, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var product = await db.Products.Include(x => x.Lots).FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
            if (product is null)
            {
                return Results.BadRequest("Product not found.");
            }

            InventoryLot? lot = null;
            if (product.TrackInventory)
            {
                lot = inventoryLotId.HasValue
                    ? await db.InventoryLots.FirstOrDefaultAsync(x => x.Id == inventoryLotId.Value, cancellationToken)
                    : product.Lots.OrderBy(x => x.ExpiryDate).ThenBy(x => x.ReceivedDate).FirstOrDefault();

                if (lot is null)
                {
                    return Results.BadRequest("No inventory lot found for adjustment.");
                }

                if (lot.QuantityOnHand + quantityDelta < 0)
                {
                    return Results.BadRequest("Adjustment would produce negative stock.");
                }

                lot.QuantityOnHand += quantityDelta;
            }

            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = productId,
                InventoryLotId = lot?.Id,
                Type = InventoryTransactionType.Adjustment,
                QuantityDelta = quantityDelta,
                ReferenceType = "ManualAdjustment",
                ReferenceId = Guid.NewGuid().ToString("N"),
                Notes = notes,
            });

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { productId, inventoryLotId = lot?.Id, quantityDelta });
        });
    }

    private static void MapVendors(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/vendors");

        group.MapGet("", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var items = await db.Vendors.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Phone, x.Email, x.ContactName, openOrders = x.PurchaseOrders.Count(po => po.Status != PurchaseOrderStatus.Received && po.Status != PurchaseOrderStatus.Cancelled) })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapPost("", async (VendorUpsertRequest request, IValidator<VendorUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var vendor = new Vendor
            {
                Name = request.Name,
                Phone = request.Phone,
                Email = request.Email,
                ContactName = request.ContactName,
                Notes = request.Notes,
            };

            db.Vendors.Add(vendor);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/vendors/{vendor.Id}", new { vendor.Id });
        });
    }

    private static void MapPurchaseOrders(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/purchase-orders");

        group.MapGet("", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var items = await db.PurchaseOrders.AsNoTracking()
                .Include(x => x.Vendor)
                .Include(x => x.Lines)
                .OrderByDescending(x => x.OrderedAt)
                .Select(x => new
                {
                    x.Id,
                    x.OrderNumber,
                    vendor = x.Vendor!.Name,
                    x.Status,
                    x.OrderedAt,
                    x.ExpectedAt,
                    totalLines = x.Lines.Count,
                    totalOrdered = x.Lines.Sum(l => l.QuantityOrdered),
                    totalReceived = x.Lines.Sum(l => l.QuantityReceived),
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var order = await db.PurchaseOrders.AsNoTracking()
                .Include(x => x.Vendor)
                .Include(x => x.Lines).ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (order is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                order.Id,
                order.OrderNumber,
                order.Status,
                order.OrderedAt,
                order.ExpectedAt,
                order.ReceivedAt,
                order.Notes,
                vendor = order.Vendor is null ? null : new { order.Vendor.Id, order.Vendor.Name },
                lines = order.Lines.Select(x => new { x.Id, x.ProductId, product = x.Product!.Name, x.QuantityOrdered, x.QuantityReceived, x.UnitCostExGst, x.BatchNumber, x.ExpiryDate }),
            });
        });

        group.MapPost("", async (PurchaseOrderUpsertRequest request, IValidator<PurchaseOrderUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var count = await db.PurchaseOrders.CountAsync(cancellationToken);
            var order = new PurchaseOrder
            {
                OrderNumber = $"PO-{1000 + count + 1}",
                VendorId = request.VendorId,
                ExpectedAt = request.ExpectedAt,
                Notes = request.Notes,
                Status = PurchaseOrderStatus.Draft,
            };

            foreach (var line in request.Lines)
            {
                order.Lines.Add(new PurchaseOrderLine
                {
                    ProductId = line.ProductId,
                    QuantityOrdered = line.QuantityOrdered,
                    UnitCostExGst = line.UnitCostExGst,
                    BatchNumber = line.BatchNumber,
                    ExpiryDate = line.ExpiryDate,
                });
            }

            db.PurchaseOrders.Add(order);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/purchase-orders/{order.Id}", new { order.Id });
        });

        group.MapPost("/{id:int}/submit", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var order = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (order is null)
            {
                return Results.NotFound();
            }

            order.Status = PurchaseOrderStatus.Submitted;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { order.Id, order.Status });
        });

        group.MapPost("/{id:int}/receive", async (int id, ReceivePurchaseOrderRequest request, IValidator<ReceivePurchaseOrderRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var order = await db.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (order is null)
            {
                return Results.NotFound();
            }

            foreach (var receivedLine in request.Lines)
            {
                var line = order.Lines.FirstOrDefault(x => x.Id == receivedLine.PurchaseOrderLineId);
                if (line is null)
                {
                    return Results.BadRequest($"Purchase order line {receivedLine.PurchaseOrderLineId} does not belong to this order.");
                }

                line.QuantityReceived += receivedLine.QuantityReceived;
                line.BatchNumber = receivedLine.BatchNumber;
                line.ExpiryDate = receivedLine.ExpiryDate;

                var lot = new InventoryLot
                {
                    ProductId = line.ProductId,
                    BatchNumber = receivedLine.BatchNumber,
                    ExpiryDate = receivedLine.ExpiryDate,
                    ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    UnitCostExGst = line.UnitCostExGst,
                    QuantityReceived = receivedLine.QuantityReceived,
                    QuantityOnHand = receivedLine.QuantityReceived,
                };
                db.InventoryLots.Add(lot);
                await db.SaveChangesAsync(cancellationToken);

                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = line.ProductId,
                    InventoryLotId = lot.Id,
                    Type = InventoryTransactionType.Receive,
                    QuantityDelta = receivedLine.QuantityReceived,
                    ReferenceType = "PurchaseOrder",
                    ReferenceId = order.OrderNumber,
                    Notes = "Received against purchase order",
                });
            }

            order.ReceivedAt = DateTimeOffset.UtcNow;
            order.Status = order.Lines.All(x => x.QuantityReceived >= x.QuantityOrdered)
                ? PurchaseOrderStatus.Received
                : PurchaseOrderStatus.PartiallyReceived;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { order.Id, order.Status });
        });
    }

    private static void MapReports(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/reports");

        group.MapGet("/dashboard", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var today = DateTimeOffset.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todaysAppointments = await db.Appointments.AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Client)
                .Include(x => x.Clinician)
                .Where(x => x.StartAt >= today && x.StartAt < tomorrow)
                .OrderBy(x => x.StartAt)
                .Select(x => new
                {
                    x.Id,
                    x.StartAt,
                    x.EndAt,
                    x.Status,
                    x.Reason,
                    x.Notes,
                    patient = x.Patient!.Name,
                    client = x.Client!.FirstName + " " + x.Client!.LastName,
                    clinician = x.Clinician!.DisplayName,
                    consultId = x.Consult != null ? x.Consult.Id : (int?)null,
                })
                .ToListAsync(cancellationToken);

            var openInvoices = await db.Invoices.AsNoTracking().CountAsync(x => x.BalanceDue > 0, cancellationToken);
            var inConsult = todaysAppointments.Count(x => x.Status == AppointmentStatus.InConsult);

            return Results.Ok(new
            {
                metrics = new
                {
                    todaysAppointments = todaysAppointments.Count,
                    arrivalsWaiting = todaysAppointments.Count(x => x.Status == AppointmentStatus.Arrived),
                    inConsult,
                    openInvoices,
                },
                todaysAppointments,
            });
        });

        group.MapGet("/daily-summary", async (DateOnly? date, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var target = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var start = new DateTimeOffset(target.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var end = start.AddDays(1);

            var appointments = await db.Appointments.AsNoTracking().CountAsync(x => x.StartAt >= start && x.StartAt < end, cancellationToken);
            var revenue = await db.Invoices.AsNoTracking().Where(x => x.IssuedAt >= start && x.IssuedAt < end).SumAsync(x => (decimal?)x.TotalIncGst, cancellationToken) ?? 0m;
            var paid = await db.Payments.AsNoTracking().Where(x => x.PaidAt >= start && x.PaidAt < end).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

            return Results.Ok(new { date = target, appointments, revenue, paid });
        });

        group.MapGet("/revenue", async (DateOnly? from, DateOnly? to, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var start = new DateTimeOffset((from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var end = new DateTimeOffset((to ?? DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

            var items = await db.Invoices.AsNoTracking()
                .Where(x => x.IssuedAt >= start && x.IssuedAt < end)
                .GroupBy(x => DateOnly.FromDateTime(x.IssuedAt!.Value.UtcDateTime))
                .Select(x => new { date = x.Key, total = x.Sum(i => i.TotalIncGst), invoiceCount = x.Count() })
                .OrderBy(x => x.date)
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/appointments", async (DateOnly? from, DateOnly? to, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var start = new DateTimeOffset((from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7))).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var end = new DateTimeOffset((to ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

            var items = await db.Appointments.AsNoTracking()
                .Where(x => x.StartAt >= start && x.StartAt < end)
                .GroupBy(x => x.Status)
                .Select(x => new { status = x.Key, count = x.Count() })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/low-stock", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var items = await db.Products.AsNoTracking()
                .Include(x => x.Lots)
                .Where(x => x.TrackInventory)
                .Select(x => new { x.Id, x.Name, onHand = x.Lots.Sum(l => l.QuantityOnHand), x.ReorderThreshold })
                .Where(x => x.onHand <= x.ReorderThreshold)
                .OrderBy(x => x.onHand)
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/product-usage", async (DateOnly? from, DateOnly? to, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var start = new DateTimeOffset((from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var end = new DateTimeOffset((to ?? DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

            var items = await db.InvoiceLines.AsNoTracking()
                .Where(x => x.Invoice!.IssuedAt >= start && x.Invoice.IssuedAt < end)
                .GroupBy(x => x.Description)
                .Select(x => new { name = x.Key, quantity = x.Sum(i => i.Quantity), revenue = x.Sum(i => i.LineTotalIncGst) })
                .OrderByDescending(x => x.revenue)
                .Take(10)
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });
    }

    private static InvoiceLine BuildInvoiceLine(Product product, decimal quantity, int? inventoryLotId, string? descriptionOverride = null)
    {
        var (subtotal, gstAmount, total) = PricingMath.Calculate(quantity, product.DefaultSalePriceExGst, product.IsTaxable);
        return BuildInvoiceLineFromValues(
            product.Id,
            descriptionOverride ?? product.Name,
            product.TrackInventory ? InvoiceLineType.Product : InvoiceLineType.Service,
            quantity,
            product.DefaultSalePriceExGst,
            product.IsTaxable,
            inventoryLotId);
    }

    private static InvoiceLine BuildInvoiceLineFromValues(int? productId, string description, InvoiceLineType lineType, decimal quantity, decimal unitPriceExGst, bool isTaxable, int? inventoryLotId)
    {
        var (subtotal, gstAmount, total) = PricingMath.Calculate(quantity, unitPriceExGst, isTaxable);
        return new InvoiceLine
        {
            ProductId = productId,
            Description = description,
            LineType = lineType,
            Quantity = quantity,
            UnitPriceExGst = unitPriceExGst,
            LineSubtotalExGst = subtotal,
            GstRate = isTaxable ? PricingMath.GstRate : 0m,
            GstAmount = gstAmount,
            LineTotalIncGst = total,
            IsTaxable = isTaxable,
            InventoryLotId = inventoryLotId,
        };
    }

    private static async Task<InventoryLot?> ResolveLotAsync(PawAndOrderDbContext db, int productId, int? requestedLotId, decimal quantity, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        IQueryable<InventoryLot> query = db.InventoryLots.Where(x =>
            x.ProductId == productId &&
            x.QuantityOnHand >= quantity &&
            (x.ExpiryDate == null || x.ExpiryDate >= today));

        if (requestedLotId.HasValue)
        {
            return await query.FirstOrDefaultAsync(x => x.Id == requestedLotId.Value, cancellationToken);
        }

        return await query.OrderBy(x => x.ExpiryDate ?? DateOnly.MaxValue).ThenBy(x => x.ReceivedDate).FirstOrDefaultAsync(cancellationToken);
    }

    private static async Task<IResult?> ValidateAsync<T>(T request, IValidator<T> validator)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
