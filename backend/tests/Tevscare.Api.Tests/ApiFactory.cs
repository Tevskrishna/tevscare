using Microsoft.AspNetCore.Mvc.Testing;

namespace Tevscare.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"tevscare-tests-{Guid.NewGuid():N}.db");

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_databasePath}");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "testing-signing-key-at-least-32-characters");
        Environment.SetEnvironmentVariable("Tevscare__DatabaseProvider", "Sqlite");
        Environment.SetEnvironmentVariable("Tevscare__SeedDemoData", "false");
        Environment.SetEnvironmentVariable("Tevscare__ExposeResetTokens", "true");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        TryDelete(_databasePath);
        TryDelete(_databasePath + "-wal");
        TryDelete(_databasePath + "-shm");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // The test host may still be releasing the file.
        }
    }
}
