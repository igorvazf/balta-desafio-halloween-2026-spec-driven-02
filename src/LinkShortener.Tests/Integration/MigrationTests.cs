using LinkShortener.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Tests.Integration;

public sealed class MigrationTests : IDisposable
{
    private readonly TemporaryDatabase _database = new();

    [Fact]
    public void Migration_creates_links_table_with_plan_data_model()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_database.ConnectionString).Options;
        using (var context = new AppDbContext(options))
        {
            context.Database.Migrate();
        }

        using var connection = new SqliteConnection(_database.ConnectionString);
        connection.Open();

        var columns = new Dictionary<string, (string Type, bool NotNull)>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info('Links');";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                columns[reader.GetString(1)] = (reader.GetString(2), reader.GetInt32(3) == 1);
            }
        }

        Assert.Equal(
            new[] { "AccessCount", "Code", "CreatedAtUtc", "ExpiresAtUtc", "Id", "TargetUrl" },
            columns.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.True(columns["Code"].NotNull);
        Assert.True(columns["TargetUrl"].NotNull);
        Assert.True(columns["CreatedAtUtc"].NotNull);
        Assert.False(columns["ExpiresAtUtc"].NotNull);
        Assert.True(columns["AccessCount"].NotNull);

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT \"unique\" FROM pragma_index_list('Links') WHERE name = 'IX_Links_Code';";
            Assert.Equal(1L, command.ExecuteScalar());
        }
    }

    public void Dispose() => _database.Dispose();
}
