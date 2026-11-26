using FluentAssertions;
using PawAndOrder.Application;

namespace PawAndOrder.Application.Tests;

public class PricingMathTests
{
    [Fact]
    public void Calculate_ShouldApplyAustralianGst_WhenItemIsTaxable()
    {
        var (subtotal, gstAmount, total) = PricingMath.Calculate(2m, 82m, true);

        subtotal.Should().Be(164m);
        gstAmount.Should().Be(16.40m);
        total.Should().Be(180.40m);
    }

    [Fact]
    public void Calculate_ShouldNotApplyGst_WhenItemIsNotTaxable()
    {
        var (subtotal, gstAmount, total) = PricingMath.Calculate(3m, 10m, false);

        subtotal.Should().Be(30m);
        gstAmount.Should().Be(0m);
        total.Should().Be(30m);
    }

    [Fact]
    public void PasswordHasher_ShouldValidateKnownPassword()
    {
        var hash = PasswordHasher.Hash("PawAndOrder!");

        PasswordHasher.Verify("PawAndOrder!", hash).Should().BeTrue();
        PasswordHasher.Verify("wrong-password", hash).Should().BeFalse();
    }
}
