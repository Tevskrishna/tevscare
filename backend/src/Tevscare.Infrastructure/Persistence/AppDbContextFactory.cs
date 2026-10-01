using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tevscare.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=tevscare;Username=tevscare;Password=tevscare_dev_password";
        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UseNpgsql(connection);
        return new AppDbContext(options.Options);
    }
}
