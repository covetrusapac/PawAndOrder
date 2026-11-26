using FluentAssertions;
using PawAndOrder.Application;

namespace PawAndOrder.Application.Tests;

public class ClientUpsertRequestValidatorTests
{
    private readonly ClientUpsertRequestValidator _validator = new();

    private static ClientUpsertRequest Valid() => new(
        FirstName: "Emma",
        LastName: "Wallace",
        Phone: "0401 001 001",
        Email: "emma.wallace@example.com",
        AddressLine1: "14 Grevillea St",
        AddressLine2: null,
        Suburb: "Richmond",
        State: "VIC",
        Postcode: "3121",
        Notes: null);

    [Theory]
    [InlineData("0401001001")]
    [InlineData("0401 001 001")]
    [InlineData("+61401001001")]
    [InlineData("0398765432")]
    public void ValidClient_Passes(string phone)
    {
        _validator.Validate(Valid() with { Phone = phone }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void OptionalFields_CanBeEmpty()
    {
        _validator.Validate(Valid() with { AddressLine2 = null, Notes = null }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("AddressLine1")]
    [InlineData("Suburb")]
    [InlineData("State")]
    [InlineData("Postcode")]
    public void RequiredField_Missing_Fails(string field)
    {
        var request = field switch
        {
            "Email" => Valid() with { Email = "" },
            "AddressLine1" => Valid() with { AddressLine1 = "" },
            "Suburb" => Valid() with { Suburb = "" },
            "State" => Valid() with { State = "" },
            "Postcode" => Valid() with { Postcode = "" },
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain(e => e.PropertyName == field);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("0401-abc-001")]
    [InlineData("040100100")]      // too short
    [InlineData("04010010011")]    // too long
    [InlineData("0101001001")]     // invalid area prefix
    public void Phone_WrongFormat_Fails(string phone)
    {
        var result = _validator.Validate(Valid() with { Phone = phone });

        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    [InlineData("@example.com")]
    public void Email_WrongFormat_Fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });

        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("312")]
    [InlineData("31211")]
    [InlineData("ABCD")]
    public void Postcode_WrongFormat_Fails(string postcode)
    {
        var result = _validator.Validate(Valid() with { Postcode = postcode });

        result.Errors.Should().Contain(e => e.PropertyName == "Postcode");
    }

    [Theory]
    [InlineData("Victoria")]
    [InlineData("XYZ")]
    public void State_NotAustralianState_Fails(string state)
    {
        var result = _validator.Validate(Valid() with { State = state });

        result.Errors.Should().Contain(e => e.PropertyName == "State");
    }
}
