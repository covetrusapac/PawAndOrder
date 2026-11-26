namespace PawAndOrder.Api.Tests;

public class ProgramTests
{
    [Fact]
    public void ProgramType_ShouldBeAccessibleForApiTests()
    {
        Assert.NotNull(typeof(Program));
    }
}
