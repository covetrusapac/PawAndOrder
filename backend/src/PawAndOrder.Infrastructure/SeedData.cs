using Microsoft.EntityFrameworkCore;
using PawAndOrder.Application;
using PawAndOrder.Domain;

namespace PawAndOrder.Infrastructure;

public static class SeedData
{
    public static async Task InitializeAsync(PawAndOrderDbContext dbContext)
    {
        await dbContext.Database.EnsureCreatedAsync();

        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var users = new[]
        {
            new User { Username = "admin", DisplayName = "Alex Mercer", PasswordHash = PasswordHasher.Hash("PawAndOrder!"), Role = UserRole.Admin },
            new User { Username = "drharper", DisplayName = "Dr. Claire Harper", PasswordHash = PasswordHasher.Hash("PawAndOrder!"), Role = UserRole.Veterinarian },
            new User { Username = "drnguyen", DisplayName = "Dr. Minh Nguyen", PasswordHash = PasswordHasher.Hash("PawAndOrder!"), Role = UserRole.Veterinarian },
            new User { Username = "nurseamy", DisplayName = "Amy Hart", PasswordHash = PasswordHasher.Hash("PawAndOrder!"), Role = UserRole.Nurse },
            new User { Username = "nurseraj", DisplayName = "Raj Patel", PasswordHash = PasswordHasher.Hash("PawAndOrder!"), Role = UserRole.Nurse },
            new User { Username = "frontdesk", DisplayName = "Sam Collins", PasswordHash = PasswordHasher.Hash("PawAndOrder!"), Role = UserRole.Receptionist },
        };

        var appointmentTypes = new[]
        {
            new AppointmentType { Name = "Consultation", DurationMinutes = 30, ColorHex = "#1976d2" },
            new AppointmentType { Name = "Vaccination", DurationMinutes = 20, ColorHex = "#2e7d32" },
            new AppointmentType { Name = "Recheck", DurationMinutes = 15, ColorHex = "#ed6c02" },
            new AppointmentType { Name = "Procedure Admit", DurationMinutes = 45, ColorHex = "#9c27b0" },
        };

        var products = new List<Product>
        {
            new() { Sku = "SRV-CONSULT", Name = "General Consultation", Type = ProductType.Service, UnitOfMeasure = "each", TrackInventory = false, DefaultSalePriceExGst = 82m, DefaultCostPriceExGst = 0m, ReorderThreshold = 0m },
            new() { Sku = "SRV-RECHECK", Name = "Recheck Consultation", Type = ProductType.Service, UnitOfMeasure = "each", TrackInventory = false, DefaultSalePriceExGst = 54m, DefaultCostPriceExGst = 0m, ReorderThreshold = 0m },
            new() { Sku = "SRV-VAX-C3", Name = "Canine C3 Vaccination", Type = ProductType.Service, UnitOfMeasure = "dose", TrackInventory = false, DefaultSalePriceExGst = 79m, DefaultCostPriceExGst = 0m, ReorderThreshold = 0m },
            new() { Sku = "SRV-BLOOD", Name = "In-house Blood Test", Type = ProductType.Service, UnitOfMeasure = "each", TrackInventory = false, DefaultSalePriceExGst = 120m, DefaultCostPriceExGst = 0m, ReorderThreshold = 0m },
            new() { Sku = "SRV-XRAY", Name = "Radiograph Series", Type = ProductType.Service, UnitOfMeasure = "each", TrackInventory = false, DefaultSalePriceExGst = 165m, DefaultCostPriceExGst = 0m, ReorderThreshold = 0m },
            new() { Sku = "MED-AMOX", Name = "Amoxicillin 50mg", Type = ProductType.Medication, UnitOfMeasure = "tablet", TrackInventory = true, DefaultSalePriceExGst = 1.65m, DefaultCostPriceExGst = 0.80m, ReorderThreshold = 20m },
            new() { Sku = "MED-MELOX", Name = "Meloxicam Oral", Type = ProductType.Medication, UnitOfMeasure = "ml", TrackInventory = true, DefaultSalePriceExGst = 2.45m, DefaultCostPriceExGst = 1.15m, ReorderThreshold = 15m },
            new() { Sku = "MED-FLEA", Name = "Flea and Tick Preventative", Type = ProductType.RetailItem, UnitOfMeasure = "dose", TrackInventory = true, DefaultSalePriceExGst = 26m, DefaultCostPriceExGst = 14m, ReorderThreshold = 10m },
            new() { Sku = "VAC-C3", Name = "Canine C3 Vaccine", Type = ProductType.Medication, UnitOfMeasure = "dose", TrackInventory = true, DefaultSalePriceExGst = 0m, DefaultCostPriceExGst = 18m, ReorderThreshold = 8m },
            new() { Sku = "VAC-C5", Name = "Canine C5 Vaccine", Type = ProductType.Medication, UnitOfMeasure = "dose", TrackInventory = true, DefaultSalePriceExGst = 0m, DefaultCostPriceExGst = 24m, ReorderThreshold = 8m },
            new() { Sku = "VAC-F3", Name = "Feline F3 Vaccine", Type = ProductType.Medication, UnitOfMeasure = "dose", TrackInventory = true, DefaultSalePriceExGst = 0m, DefaultCostPriceExGst = 16m, ReorderThreshold = 8m },
            new() { Sku = "CONS-GLOVES", Name = "Examination Gloves", Type = ProductType.Consumable, UnitOfMeasure = "box", TrackInventory = true, DefaultSalePriceExGst = 0m, DefaultCostPriceExGst = 8m, ReorderThreshold = 4m },
        };

        var diagnosisCatalog = new[]
        {
            new DiagnosisCatalogItem { Name = "Otitis externa", Code = "DX-001" },
            new DiagnosisCatalogItem { Name = "Gastroenteritis", Code = "DX-002" },
            new DiagnosisCatalogItem { Name = "Dermatitis", Code = "DX-003" },
            new DiagnosisCatalogItem { Name = "Dental disease", Code = "DX-004" },
            new DiagnosisCatalogItem { Name = "Osteoarthritis", Code = "DX-005" },
            new DiagnosisCatalogItem { Name = "Conjunctivitis", Code = "DX-006" },
            new DiagnosisCatalogItem { Name = "Lameness", Code = "DX-007" },
            new DiagnosisCatalogItem { Name = "Obesity", Code = "DX-008" },
        };

        var procedureCatalog = new[]
        {
            new ProcedureCatalogItem { Name = "Consultation", Code = "PROC-001", DefaultProduct = products[0] },
            new ProcedureCatalogItem { Name = "Vaccination", Code = "PROC-002", DefaultProduct = products[2] },
            new ProcedureCatalogItem { Name = "Nail clip", Code = "PROC-003" },
            new ProcedureCatalogItem { Name = "Blood test", Code = "PROC-004", DefaultProduct = products[3] },
            new ProcedureCatalogItem { Name = "Radiograph", Code = "PROC-005", DefaultProduct = products[4] },
            new ProcedureCatalogItem { Name = "Ear cleaning", Code = "PROC-006" },
            new ProcedureCatalogItem { Name = "Dental scale and polish", Code = "PROC-007" },
        };

        var vaccinationTypes = new[]
        {
            new VaccinationType { Name = "Canine C3", Species = "Canine", DefaultIntervalMonths = 12 },
            new VaccinationType { Name = "Canine C5", Species = "Canine", DefaultIntervalMonths = 12 },
            new VaccinationType { Name = "Feline F3", Species = "Feline", DefaultIntervalMonths = 12 },
            new VaccinationType { Name = "Feline FIV", Species = "Feline", DefaultIntervalMonths = 12 },
            new VaccinationType { Name = "Rabbit Calicivirus", Species = "Rabbit", DefaultIntervalMonths = 12 },
        };

        var vendors = new[]
        {
            new Vendor { Name = "VetSupplies Australia", Phone = "1300 100 100", Email = "orders@vetsupplies.example", ContactName = "Ella Brown" },
            new Vendor { Name = "Companion Animal Pharma", Phone = "1300 200 200", Email = "sales@companionpharma.example", ContactName = "Luca Green" },
            new Vendor { Name = "Clinic Essentials AU", Phone = "1300 300 300", Email = "hello@clinicessentials.example", ContactName = "Priya Shah" },
        };

        var clients = new[]
        {
            new Client { FirstName = "Emma", LastName = "Wallace", Phone = "0401001001", Email = "emma.wallace@example.com", AddressLine1 = "14 Grevillea St", Suburb = "Richmond", State = "VIC", Postcode = "3121" },
            new Client { FirstName = "Noah", LastName = "Davies", Phone = "0401001002", Email = "noah.davies@example.com", AddressLine1 = "22 Johnston Rd", Suburb = "Hawthorn", State = "VIC", Postcode = "3122" },
            new Client { FirstName = "Olivia", LastName = "Turner", Phone = "0401001003", Email = "olivia.turner@example.com", AddressLine1 = "6 Park Ave", Suburb = "Kew", State = "VIC", Postcode = "3101" },
            new Client { FirstName = "Liam", LastName = "Scott", Phone = "0401001004", Email = "liam.scott@example.com", AddressLine1 = "7 McArthur St", Suburb = "Brunswick", State = "VIC", Postcode = "3056" },
            new Client { FirstName = "Mia", LastName = "Foster", Phone = "0401001005", Email = "mia.foster@example.com", AddressLine1 = "31 Albert St", Suburb = "Coburg", State = "VIC", Postcode = "3058" },
            new Client { FirstName = "Henry", LastName = "Price", Phone = "0401001006", Email = "henry.price@example.com", AddressLine1 = "55 Barkly St", Suburb = "St Kilda", State = "VIC", Postcode = "3182" },
            new Client { FirstName = "Isla", LastName = "Cook", Phone = "0401001007", Email = "isla.cook@example.com", AddressLine1 = "91 Bay St", Suburb = "Port Melbourne", State = "VIC", Postcode = "3207" },
            new Client { FirstName = "Jack", LastName = "Bennett", Phone = "0401001008", Email = "jack.bennett@example.com", AddressLine1 = "12 Darling St", Suburb = "South Yarra", State = "VIC", Postcode = "3141" },
            new Client { FirstName = "Sophie", LastName = "Ward", Phone = "0401001009", Email = "sophie.ward@example.com", AddressLine1 = "88 Whitehorse Rd", Suburb = "Balwyn", State = "VIC", Postcode = "3103" },
            new Client { FirstName = "Lucas", LastName = "King", Phone = "0401001010", Email = "lucas.king@example.com", AddressLine1 = "16 Durham St", Suburb = "Fitzroy", State = "VIC", Postcode = "3065" },
            new Client { FirstName = "Grace", LastName = "Reid", Phone = "0401001011", Email = "grace.reid@example.com", AddressLine1 = "9 Pakington St", Suburb = "Geelong West", State = "VIC", Postcode = "3218" },
            new Client { FirstName = "William", LastName = "Ross", Phone = "0401001012", Email = "william.ross@example.com", AddressLine1 = "48 Miller St", Suburb = "North Sydney", State = "NSW", Postcode = "2060" },
        };

        var patients = new[]
        {
            new Patient { Client = clients[0], Name = "Milo", Species = "Dog", Breed = "Cavoodle", Sex = "Male", DateOfBirth = new DateOnly(2020, 4, 12), WeightKg = 8.4m, IsDesexed = true, MicrochipNumber = "956000001", AlertsSummary = "Nervous in consult room" },
            new Patient { Client = clients[0], Name = "Poppy", Species = "Cat", Breed = "Domestic Short Hair", Sex = "Female", DateOfBirth = new DateOnly(2019, 10, 2), WeightKg = 4.3m, IsDesexed = true },
            new Patient { Client = clients[1], Name = "Archie", Species = "Dog", Breed = "Kelpie", Sex = "Male", DateOfBirth = new DateOnly(2018, 3, 21), WeightKg = 19.7m, IsDesexed = true },
            new Patient { Client = clients[1], Name = "Luna", Species = "Dog", Breed = "Border Collie", Sex = "Female", DateOfBirth = new DateOnly(2021, 1, 18), WeightKg = 14.9m, IsDesexed = true },
            new Patient { Client = clients[2], Name = "Coco", Species = "Dog", Breed = "French Bulldog", Sex = "Female", DateOfBirth = new DateOnly(2017, 7, 9), WeightKg = 11.2m, IsDesexed = true, AllergiesSummary = "Chicken sensitivity" },
            new Patient { Client = clients[2], Name = "Leo", Species = "Cat", Breed = "Ragdoll", Sex = "Male", DateOfBirth = new DateOnly(2022, 6, 1), WeightKg = 5.8m, IsDesexed = true },
            new Patient { Client = clients[3], Name = "Ruby", Species = "Dog", Breed = "Golden Retriever", Sex = "Female", DateOfBirth = new DateOnly(2016, 11, 30), WeightKg = 27.4m, IsDesexed = true },
            new Patient { Client = clients[3], Name = "Gus", Species = "Rabbit", Breed = "Mini Lop", Sex = "Male", DateOfBirth = new DateOnly(2023, 2, 14), WeightKg = 1.9m, IsDesexed = false },
            new Patient { Client = clients[4], Name = "Nala", Species = "Cat", Breed = "Burmese", Sex = "Female", DateOfBirth = new DateOnly(2015, 9, 22), WeightKg = 4.1m, IsDesexed = true },
            new Patient { Client = clients[5], Name = "Buddy", Species = "Dog", Breed = "Labrador", Sex = "Male", DateOfBirth = new DateOnly(2020, 12, 7), WeightKg = 29.2m, IsDesexed = true },
            new Patient { Client = clients[6], Name = "Pepper", Species = "Cat", Breed = "Domestic Long Hair", Sex = "Female", DateOfBirth = new DateOnly(2021, 8, 16), WeightKg = 3.7m, IsDesexed = true },
            new Patient { Client = clients[7], Name = "Frankie", Species = "Dog", Breed = "Jack Russell", Sex = "Male", DateOfBirth = new DateOnly(2019, 1, 4), WeightKg = 7.3m, IsDesexed = true },
            new Patient { Client = clients[8], Name = "Willow", Species = "Dog", Breed = "Whippet", Sex = "Female", DateOfBirth = new DateOnly(2022, 4, 27), WeightKg = 10.6m, IsDesexed = true },
            new Patient { Client = clients[9], Name = "Charlie", Species = "Cat", Breed = "Maine Coon", Sex = "Male", DateOfBirth = new DateOnly(2018, 5, 13), WeightKg = 6.4m, IsDesexed = true },
            new Patient { Client = clients[10], Name = "Maple", Species = "Dog", Breed = "Beagle", Sex = "Female", DateOfBirth = new DateOnly(2020, 9, 10), WeightKg = 12.5m, IsDesexed = true },
            new Patient { Client = clients[11], Name = "Theo", Species = "Dog", Breed = "German Shepherd", Sex = "Male", DateOfBirth = new DateOnly(2017, 4, 6), WeightKg = 32.8m, IsDesexed = true },
        };

        dbContext.AddRange(users);
        dbContext.AddRange(appointmentTypes);
        dbContext.AddRange(products);
        dbContext.AddRange(diagnosisCatalog);
        dbContext.AddRange(procedureCatalog);
        dbContext.AddRange(vaccinationTypes);
        dbContext.AddRange(vendors);
        dbContext.AddRange(clients);
        dbContext.AddRange(patients);
        await dbContext.SaveChangesAsync();

        var lots = new[]
        {
            new InventoryLot { ProductId = products[5].Id, BatchNumber = "AMX-2401", ExpiryDate = DateOnly.FromDateTime(now.AddMonths(8).DateTime), ReceivedDate = DateOnly.FromDateTime(now.AddDays(-45).DateTime), UnitCostExGst = 0.80m, QuantityReceived = 100, QuantityOnHand = 62 },
            new InventoryLot { ProductId = products[6].Id, BatchNumber = "MEL-2402", ExpiryDate = DateOnly.FromDateTime(now.AddMonths(6).DateTime), ReceivedDate = DateOnly.FromDateTime(now.AddDays(-30).DateTime), UnitCostExGst = 1.15m, QuantityReceived = 50, QuantityOnHand = 18 },
            new InventoryLot { ProductId = products[7].Id, BatchNumber = "FLT-2403", ExpiryDate = DateOnly.FromDateTime(now.AddMonths(10).DateTime), ReceivedDate = DateOnly.FromDateTime(now.AddDays(-20).DateTime), UnitCostExGst = 14m, QuantityReceived = 30, QuantityOnHand = 9 },
            new InventoryLot { ProductId = products[8].Id, BatchNumber = "C3-2404", ExpiryDate = DateOnly.FromDateTime(now.AddMonths(5).DateTime), ReceivedDate = DateOnly.FromDateTime(now.AddDays(-25).DateTime), UnitCostExGst = 18m, QuantityReceived = 20, QuantityOnHand = 6 },
            new InventoryLot { ProductId = products[9].Id, BatchNumber = "C5-2405", ExpiryDate = DateOnly.FromDateTime(now.AddMonths(7).DateTime), ReceivedDate = DateOnly.FromDateTime(now.AddDays(-18).DateTime), UnitCostExGst = 24m, QuantityReceived = 18, QuantityOnHand = 11 },
            new InventoryLot { ProductId = products[10].Id, BatchNumber = "F3-2406", ExpiryDate = DateOnly.FromDateTime(now.AddMonths(9).DateTime), ReceivedDate = DateOnly.FromDateTime(now.AddDays(-10).DateTime), UnitCostExGst = 16m, QuantityReceived = 16, QuantityOnHand = 13 },
            new InventoryLot { ProductId = products[11].Id, BatchNumber = "GLV-2407", ExpiryDate = DateOnly.FromDateTime(now.AddMonths(14).DateTime), ReceivedDate = DateOnly.FromDateTime(now.AddDays(-60).DateTime), UnitCostExGst = 8m, QuantityReceived = 12, QuantityOnHand = 3 },
        };
        dbContext.InventoryLots.AddRange(lots);
        await dbContext.SaveChangesAsync();

        var purchaseOrders = new[]
        {
            new PurchaseOrder { OrderNumber = "PO-1001", VendorId = vendors[0].Id, Status = PurchaseOrderStatus.Received, OrderedAt = now.AddDays(-20), ReceivedAt = now.AddDays(-18), Notes = "Routine vaccine replenishment" },
            new PurchaseOrder { OrderNumber = "PO-1002", VendorId = vendors[1].Id, Status = PurchaseOrderStatus.PartiallyReceived, OrderedAt = now.AddDays(-12), ExpectedAt = now.AddDays(2), Notes = "Analgesics and antibiotics" },
            new PurchaseOrder { OrderNumber = "PO-1003", VendorId = vendors[2].Id, Status = PurchaseOrderStatus.Submitted, OrderedAt = now.AddDays(-5), ExpectedAt = now.AddDays(5), Notes = "Consumables restock" },
        };
        dbContext.PurchaseOrders.AddRange(purchaseOrders);
        await dbContext.SaveChangesAsync();

        var poLines = new[]
        {
            new PurchaseOrderLine { PurchaseOrderId = purchaseOrders[0].Id, ProductId = products[8].Id, QuantityOrdered = 20, QuantityReceived = 20, UnitCostExGst = 18m, BatchNumber = "C3-2404", ExpiryDate = lots[3].ExpiryDate },
            new PurchaseOrderLine { PurchaseOrderId = purchaseOrders[1].Id, ProductId = products[5].Id, QuantityOrdered = 100, QuantityReceived = 40, UnitCostExGst = 0.80m, BatchNumber = "AMX-2401", ExpiryDate = lots[0].ExpiryDate },
            new PurchaseOrderLine { PurchaseOrderId = purchaseOrders[1].Id, ProductId = products[6].Id, QuantityOrdered = 50, QuantityReceived = 20, UnitCostExGst = 1.15m, BatchNumber = "MEL-2402", ExpiryDate = lots[1].ExpiryDate },
            new PurchaseOrderLine { PurchaseOrderId = purchaseOrders[2].Id, ProductId = products[11].Id, QuantityOrdered = 12, QuantityReceived = 0, UnitCostExGst = 8m },
        };
        dbContext.PurchaseOrderLines.AddRange(poLines);

        foreach (var lot in lots)
        {
            dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = lot.ProductId,
                InventoryLotId = lot.Id,
                Type = InventoryTransactionType.Receive,
                QuantityDelta = lot.QuantityReceived,
                ReferenceType = "Seed",
                ReferenceId = $"LOT-{lot.BatchNumber}",
                Notes = "Initial seeded stock",
                CreatedByUserId = users[0].Id,
            });
        }

        await dbContext.SaveChangesAsync();

        var clinicians = users.Where(x => x.Role == UserRole.Veterinarian).ToArray();
        var random = new Random(42);
        var appointments = new List<Appointment>();
        for (var dayOffset = -1; dayOffset <= 5; dayOffset++)
        {
            for (var slot = 0; slot < 4; slot++)
            {
                var patient = patients[(dayOffset + slot + patients.Length) % patients.Length];
                var clinician = clinicians[(slot + Math.Max(dayOffset, 0)) % clinicians.Length];
                var start = now.Date.AddDays(dayOffset).AddHours(9 + slot * 2);
                appointments.Add(new Appointment
                {
                    PatientId = patient.Id,
                    ClientId = patient.ClientId,
                    ClinicianUserId = clinician.Id,
                    AppointmentTypeId = appointmentTypes[slot % appointmentTypes.Length].Id,
                    StartAt = new DateTimeOffset(start, now.Offset),
                    EndAt = new DateTimeOffset(start.AddMinutes(appointmentTypes[slot % appointmentTypes.Length].DurationMinutes), now.Offset),
                    Status = dayOffset < 0 ? AppointmentStatus.Completed : (slot == 0 && dayOffset == 0 ? AppointmentStatus.Arrived : AppointmentStatus.Booked),
                    Reason = slot % 2 == 0 ? "Wellness exam" : "Limping and pain review",
                    Notes = slot == 3 ? "Owner requested updated flea prevention advice" : null,
                });
            }
        }

        dbContext.Appointments.AddRange(appointments);
        await dbContext.SaveChangesAsync();

        var completedAppointments = appointments.Where(x => x.Status == AppointmentStatus.Completed).Take(10).ToArray();
        var consults = new List<Consult>();
        var invoices = new List<Invoice>();

        for (var i = 0; i < completedAppointments.Length; i++)
        {
            var appointment = completedAppointments[i];
            var consult = new Consult
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                ClinicianUserId = appointment.ClinicianUserId,
                VisitReason = appointment.Reason,
                PresentingComplaint = i % 3 == 0 ? "Intermittent scratching and head shaking" : "Annual health assessment",
                SubjectiveNotes = "Owner reports normal appetite and hydration. Mild change in behaviour this week.",
                ObjectiveNotes = "Bright, alert and responsive. Ears mildly erythematous on the left in affected cases.",
                AssessmentNotes = i % 3 == 0 ? "Likely early otitis externa" : "Routine preventive care visit",
                PlanNotes = i % 3 == 0 ? "Clean ears, commence antibiotic course, review in 7 days." : "Vaccination update and preventive discussion.",
                FollowUpNotes = i % 3 == 0 ? "Recheck in one week if not resolved." : "Annual review due in 12 months.",
                WeightKg = patients.First(x => x.Id == appointment.PatientId).WeightKg,
                TemperatureC = 38.4m,
                HeartRateBpm = 98,
                RespiratoryRateBpm = 24,
                Status = ConsultStatus.Finalized,
                StartedAt = appointment.StartAt,
                CompletedAt = appointment.EndAt,
            };
            consults.Add(consult);
        }

        dbContext.Consults.AddRange(consults);
        await dbContext.SaveChangesAsync();

        for (var i = 0; i < consults.Count; i++)
        {
            var consult = consults[i];
            var appointment = completedAppointments[i];
            dbContext.ConsultDiagnoses.Add(new ConsultDiagnosis
            {
                ConsultId = consult.Id,
                DiagnosisCatalogItemId = diagnosisCatalog[i % diagnosisCatalog.Length].Id,
                DiagnosisText = diagnosisCatalog[i % diagnosisCatalog.Length].Name,
                SortOrder = 1,
            });

            dbContext.ConsultProcedures.Add(new ConsultProcedure
            {
                ConsultId = consult.Id,
                ProcedureCatalogItemId = procedureCatalog[0].Id,
                ProcedureText = procedureCatalog[0].Name,
                Quantity = 1m,
                SortOrder = 1,
            });

            if (i % 2 == 0)
            {
                dbContext.ConsultMedications.Add(new ConsultMedication
                {
                    ConsultId = consult.Id,
                    ProductId = products[5].Id,
                    InventoryLotId = lots[0].Id,
                    Quantity = 14,
                    DoseInstructions = "1 tablet twice daily for 7 days",
                });

                lots[0].QuantityOnHand -= 14;
                dbContext.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = products[5].Id,
                    InventoryLotId = lots[0].Id,
                    Type = InventoryTransactionType.Dispense,
                    QuantityDelta = -14,
                    ReferenceType = "Consult",
                    ReferenceId = consult.Id.ToString(),
                    Notes = "Dispensed during consult",
                    CreatedByUserId = consult.ClinicianUserId,
                });
            }

            if (i % 3 == 0)
            {
                var vaccineType = vaccinationTypes[i % 3];
                var vaccineProduct = vaccineType.Name.Contains("Feline") ? products[10] : products[8];
                var vaccineLot = vaccineProduct.Id == products[10].Id ? lots[5] : lots[3];

                dbContext.PatientVaccinationRecords.Add(new PatientVaccinationRecord
                {
                    PatientId = consult.PatientId,
                    ConsultId = consult.Id,
                    VaccinationTypeId = vaccineType.Id,
                    ProductId = vaccineProduct.Id,
                    InventoryLotId = vaccineLot.Id,
                    DateAdministered = DateOnly.FromDateTime(appointment.StartAt.Date),
                    DueDate = DateOnly.FromDateTime(appointment.StartAt.AddMonths(12).Date),
                    Notes = "No adverse event observed.",
                });

                vaccineLot.QuantityOnHand -= 1;
                dbContext.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = vaccineProduct.Id,
                    InventoryLotId = vaccineLot.Id,
                    Type = InventoryTransactionType.Dispense,
                    QuantityDelta = -1,
                    ReferenceType = "Vaccination",
                    ReferenceId = consult.Id.ToString(),
                    Notes = "Vaccination stock used during consult",
                    CreatedByUserId = consult.ClinicianUserId,
                });
            }

            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{1000 + i}",
                ClientId = appointment.ClientId,
                PatientId = appointment.PatientId,
                ConsultId = consult.Id,
                Status = i % 2 == 0 ? InvoiceStatus.Paid : InvoiceStatus.Issued,
                IssuedAt = appointment.EndAt,
            };

            invoice.Lines.Add(BuildInvoiceLine(products[0], 1m));
            if (i % 2 == 0)
            {
                invoice.Lines.Add(BuildInvoiceLine(products[5], 14m, lots[0].Id));
            }

            if (i % 3 == 0)
            {
                invoice.Lines.Add(BuildInvoiceLine(products[2], 1m));
            }

            RecalculateInvoice(invoice);

            if (invoice.Status == InvoiceStatus.Paid)
            {
                invoice.AmountPaid = invoice.TotalIncGst;
                invoice.BalanceDue = 0m;
                invoice.Payments.Add(new Payment
                {
                    Amount = invoice.TotalIncGst,
                    PaymentMethod = i % 4 == 0 ? PaymentMethod.Card : PaymentMethod.Cash,
                    PaidAt = invoice.IssuedAt ?? now,
                    Reference = i % 4 == 0 ? "Terminal 1" : null,
                });
            }
            else
            {
                invoice.BalanceDue = invoice.TotalIncGst;
            }

            invoices.Add(invoice);
        }

        dbContext.Invoices.AddRange(invoices);
        await dbContext.SaveChangesAsync();
    }

    private static InvoiceLine BuildInvoiceLine(Product product, decimal quantity, int? lotId = null)
    {
        var (subtotal, gstAmount, total) = PricingMath.Calculate(quantity, product.DefaultSalePriceExGst, product.IsTaxable);

        return new InvoiceLine
        {
            ProductId = product.Id,
            Description = product.Name,
            LineType = product.TrackInventory ? InvoiceLineType.Product : InvoiceLineType.Service,
            Quantity = quantity,
            UnitPriceExGst = product.DefaultSalePriceExGst,
            LineSubtotalExGst = subtotal,
            GstRate = product.IsTaxable ? PricingMath.GstRate : 0m,
            GstAmount = gstAmount,
            LineTotalIncGst = total,
            IsTaxable = product.IsTaxable,
            InventoryLotId = lotId,
        };
    }

    public static void RecalculateInvoice(Invoice invoice)
    {
        invoice.SubtotalExGst = invoice.Lines.Sum(x => x.LineSubtotalExGst);
        invoice.TotalGst = invoice.Lines.Sum(x => x.GstAmount);
        invoice.TotalIncGst = invoice.Lines.Sum(x => x.LineTotalIncGst);
        invoice.AmountPaid = invoice.Payments.Sum(x => x.Amount);
        invoice.BalanceDue = invoice.TotalIncGst - invoice.AmountPaid;

        // An invoice with no lines has nothing to pay, so it isn't "Paid" yet.
        invoice.Status = invoice.Lines.Count > 0 && invoice.BalanceDue <= 0
            ? InvoiceStatus.Paid
            : invoice.AmountPaid > 0
                ? InvoiceStatus.PartiallyPaid
                : invoice.Status == InvoiceStatus.Draft ? InvoiceStatus.Draft : InvoiceStatus.Issued;
    }
}
