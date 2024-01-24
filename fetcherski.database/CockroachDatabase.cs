using System.Data;
using System.Data.Common;
using fetcherski.database.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace fetcherski.database;

public sealed class CockroachDatabase(IOptionsSnapshot<CockroachConfig> options) : IDatabase, IDisposable
{
    private readonly CockroachConfig _config = options.Value;
    private readonly CancellationTokenSource _cts = new();

    async Task<QueryResult<string>> IDatabase.StartQuery(CancellationToken cancellation)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        await using var db = await OpenDatabase(cts.Token);

        await using var command = db.CreateCommand();

        command.CommandType = CommandType.Text;
        command.CommandText = "select id, sequential_id, created, description from looseitems order by sequential_id asc limit 2";
        await using var reader = await command.ExecuteReaderAsync(cts.Token);

        var data = new List<string>();
        
        while (await reader.ReadAsync(cts.Token))
        {
            var id = reader.GetGuid(0);
            var sequentialId = reader.GetInt64(1);
            var timestamp = reader.GetDateTime(2);
            var description = reader.GetString(3);
            
            data.Add($"{id}:[{sequentialId}]->{description}");
        }
        
        return new QueryResult<string>("continuation-token", data.ToArray());
    }

    async Task<QueryResult<string>> IDatabase.ContinueQuery(string continuationToken, CancellationToken cancellation)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        await using var db = await OpenDatabase(cts.Token);
        return new QueryResult<string>(null, ["Second", "Page", "Is", "The", "Last", "One"]);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task<DbConnection> OpenDatabase(CancellationToken cancellation)
    {
        var csb = new NpgsqlConnectionStringBuilder()
        {
            Host = _config.Host,
            Port = _config.Port,
            Database = _config.Database,
            Username = _config.User,
            Password = _config.Password,
            SearchPath = _config.Schema,
            ApplicationName = "fetcherski.database",
            SslMode = SslMode.VerifyFull,
            Pooling = true
        };

        var connection = new NpgsqlConnection(csb.ConnectionString);

        await connection.OpenAsync(cancellation);
        
        return connection;
    }
}