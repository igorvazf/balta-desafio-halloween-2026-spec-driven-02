using Microsoft.Data.Sqlite;

namespace LinkShortener.Tests.Integration;

/// <summary>
/// Arquivo SQLite descartável, um por teste.
/// </summary>
public sealed class TemporaryDatabase : IDisposable
{
    public string FilePath { get; } = Path.Combine(Path.GetTempPath(), $"linkshortener-tests-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={FilePath}";

    public void Dispose()
    {
        // O pool do Microsoft.Data.Sqlite segura o arquivo aberto no Windows.
        SqliteConnection.ClearAllPools();

        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            File.Delete(FilePath + suffix);
        }
    }
}
