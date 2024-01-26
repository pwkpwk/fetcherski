using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using fetcherski.client;
using fetcherski.database.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace fetcherski.database;

public sealed class CockroachDatabase(IOptionsSnapshot<CockroachConfig> options) : IDatabase, IDisposable
{
    private readonly CockroachConfig _config = options.Value;
    private readonly CancellationTokenSource _cts = new();

    async Task<QueryResult<Client.Item>> IDatabase.StartQuery(
        int pageSize,
        IDatabase.Order order,
        CancellationToken cancellation)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var sqlOrder = SqlOrderFromOrder(order);
        await using var db = await OpenDatabaseAsync(cts.Token);
        await using var reader = await OpenReaderAsyncAsync(db,
            order == IDatabase.Order.Ascending ? DateTime.MinValue : DateTime.MaxValue,
            sqlOrder,
            pageSize,
            cancellation);

        return await ProcessReaderAsync(reader, pageSize, sqlOrder, cts.Token);
    }

    async Task<QueryResult<Client.Item>> IDatabase.ContinueQuery(string continuationToken, CancellationToken cancellation)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var decodedToken = (await DecodeContinuationTokenAsync(continuationToken, cts.Token)).AsObject();
        DateTime timestamp = new DateTime(decodedToken["t"].GetValue<long>());
        long sequentialId = decodedToken["s"].GetValue<long>();
        string sqlOrder = decodedToken["o"].GetValue<string>();
        int pageSize = decodedToken["p"].GetValue<int>();
        await using var db = await OpenDatabaseAsync(cts.Token);
        await using var reader = await OpenReaderAsyncAsync(db, timestamp, sqlOrder, pageSize, cancellation);

        if (!await SkipReaderAsync(reader, sequentialId, cts.Token))
        {
            return new QueryResult<Client.Item>(null, null);
        }

        return await ProcessReaderAsync(reader, pageSize, sqlOrder, cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task<DbConnection> OpenDatabaseAsync(CancellationToken cancellation)
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

    private static string SqlOrderFromOrder(IDatabase.Order order) =>
        order == IDatabase.Order.Ascending ? "asc" : "desc";

    private static Task<JsonNode?> DecodeContinuationTokenAsync(string base64Token, CancellationToken cancellation)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(base64Token));
        return JsonNode.ParseAsync(stream, null, default, cancellation);
    }

    private static async Task<DbDataReader> OpenReaderAsyncAsync(
        DbConnection connection,
        DateTime startingTimestamp,
        string sqlOrder,
        int pageSize,
        CancellationToken cancellation)
    {
        await using var command = connection.CreateCommand();
        bool firstPage = startingTimestamp == DateTime.MinValue || startingTimestamp == DateTime.MaxValue;
        StringBuilder queryText = new("select id,sequential_id,created,description from looseitems");

        command.CommandType = CommandType.Text;

        if (!firstPage)
        {
            queryText.Append(" where created ");
            queryText.Append(sqlOrder == "asc" ? ">=" : "<=");
            queryText.Append(" @timestamp");

            var timestampParameter = command.CreateParameter();
            timestampParameter.ParameterName = "@timestamp";
            timestampParameter.DbType = DbType.DateTime2;
            timestampParameter.Value = startingTimestamp;
            command.Parameters.Add(timestampParameter);
        }

        queryText.Append(" order by created ");
        queryText.Append(sqlOrder);
        queryText.Append(",sequential_id ");
        queryText.Append(sqlOrder);

        if (firstPage)
        {
            queryText.Append(" limit ");
            queryText.Append(pageSize + 1);
        }

        command.CommandText = queryText.ToString();

        return await command.ExecuteReaderAsync(cancellation);
    }

    private static async Task<bool> SkipReaderAsync(DbDataReader reader, long sequentialId,
        CancellationToken cancellation)
    {
        while (await reader.ReadAsync(cancellation))
        {
            var sid = reader.GetInt64(1);
            if (sequentialId == sid)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<QueryResult<Client.Item>> ProcessReaderAsync(
        DbDataReader reader,
        int pageSize,
        string sqlOrder,
        CancellationToken cancellation)
    {
        var data = new List<Client.Item>(pageSize);
        long sid = -1;
        long ticks = -1;
        int count = 0;

        while (count < pageSize && await reader.ReadAsync(cancellation))
        {
            var id = reader.GetGuid(0);
            var description = reader.GetString(3);
            sid = reader.GetInt64(1);
            ticks = reader.GetDateTime(2).Ticks;

            data.Add(new Client.Item { id = id, description = description });
            count++;
        }

        string? newContinuationToken = null;

        if (count == pageSize && await reader.ReadAsync(cancellation))
        {
            using var stream = new MemoryStream();
            await using var writer = new Utf8JsonWriter(stream);

            var tokenData = new JsonObject
            {
                ["t"] = ticks,
                ["s"] = sid, // sequential ID of the last item on the returned page
                ["o"] = sqlOrder,
                ["p"] = pageSize
            };

            tokenData.WriteTo(writer);
            await writer.FlushAsync(cancellation);
            newContinuationToken = Convert.ToBase64String(stream.ToArray());
        }

        return new QueryResult<Client.Item>(newContinuationToken, data.ToArray());
    }
}