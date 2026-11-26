-- PAW-101 setup: give Milo last year's C3 shot (now overdue) plus this year's booster.
-- The Patients list should show next year's date but will show last month's date.
USE PawAndOrder;

DECLARE @patientId int = (SELECT TOP 1 Id FROM Patients WHERE Name = 'Milo');
DECLARE @typeId int = (SELECT TOP 1 Id FROM VaccinationTypes WHERE Name = 'Canine C3');
DECLARE @today date = CAST(SYSDATETIME() AS date);

INSERT INTO PatientVaccinationRecords
    (PatientId, VaccinationTypeId, DateAdministered, DueDate, Notes, CreatedAt, UpdatedAt)
VALUES
    (@patientId, @typeId, DATEADD(month, -13, @today), DATEADD(month, -1, @today), 'Annual C3', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
    (@patientId, @typeId, DATEADD(day, -14, @today), DATEADD(day, -14, DATEADD(year, 1, @today)), 'Annual C3 booster', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

SELECT p.Name, vt.Name AS Vaccine, r.DateAdministered, r.DueDate
FROM PatientVaccinationRecords r
JOIN Patients p ON p.Id = r.PatientId
JOIN VaccinationTypes vt ON vt.Id = r.VaccinationTypeId
WHERE r.PatientId = @patientId
ORDER BY r.DueDate;
