using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PawAndOrder.Infrastructure;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PawAndOrderDbContext>
{
    public PawAndOrderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PawAndOrderDbContext>();
        optionsBuilder.UseSqlServer("Server=tcp:127.0.0.1,1433;Database=PawAndOrder;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False");
        return new PawAndOrderDbContext(optionsBuilder.Options);
    }
}
