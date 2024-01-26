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

    Task<QueryResult<Client.Item>> IDatabase.QueryLooseItemsAsync(
        int pageSize,
        IDatabase.Order order,
        CancellationToken cancellation) => QueryTableAsync("looseitems", pageSize, order, cancellation);

    Task<QueryResult<Client.Item>> IDatabase.QueryPackItemsAsync(
        int pageSize,
        IDatabase.Order order,
        CancellationToken cancellation) => QueryTableAsync("pack_contents_view", pageSize, order, cancellation);

    async Task<QueryResult<Client.Item>> IDatabase.ContinueAsync(string continuationToken,
        CancellationToken cancellation)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var decodedToken = (await DecodeContinuationTokenAsync(continuationToken, cts.Token)).AsObject();
        string table = decodedToken["q"].GetValue<string>();
        DateTime timestamp = new DateTime(decodedToken["t"].GetValue<long>());
        long sequentialId = decodedToken["s"].GetValue<long>();
        string sqlOrder = decodedToken["o"].GetValue<string>();
        int pageSize = decodedToken["p"].GetValue<int>();
        await using var db = await OpenDatabaseAsync(cts.Token);
        await using var reader = await OpenReaderAsyncAsync(db, table, timestamp, sqlOrder, pageSize, cancellation);

        if (!await SkipReaderAsync(reader, sequentialId, cts.Token))
        {
            return new QueryResult<Client.Item>(null, null);
        }

        return await ProcessReaderAsync(reader, table, pageSize, sqlOrder, cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task<QueryResult<Client.Item>> QueryTableAsync(
        string table,
        int pageSize,
        IDatabase.Order order,
        CancellationToken cancellation)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var sqlOrder = SqlOrderFromOrder(order);
        await using var db = await OpenDatabaseAsync(cts.Token);
        await using var reader = await OpenReaderAsyncAsync(db,
            table,
            order == IDatabase.Order.Ascending ? DateTime.MinValue : DateTime.MaxValue,
            sqlOrder,
            pageSize,
            cancellation);

        return await ProcessReaderAsync(reader, table, pageSize, sqlOrder, cts.Token);
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
        string table,
        DateTime startingTimestamp,
        string sqlOrder,
        int pageSize,
        CancellationToken cancellation)
    {
        await using var command = connection.CreateCommand();
        bool firstPage = startingTimestamp == DateTime.MinValue || startingTimestamp == DateTime.MaxValue;
        StringBuilder queryText = new("select id,sequential_id,created,description from ");

        queryText.Append(table);

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

        command.CommandType = CommandType.Text;
        command.CommandText = queryText.ToString();

        return await command.ExecuteReaderAsync(cancellation);
    }

    private static async Task<bool> SkipReaderAsync(
        DbDataReader reader,
        long sequentialId,
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
        string table,
        int pageSize,
        string sqlOrder,
        CancellationToken cancellation)
    {
        var data = new List<Client.Item>(pageSize);
        long sid = default, ticks = default;
        int count = 0;

        while (count < pageSize && await reader.ReadAsync(cancellation))
        {
            var item = new Client.Item(
                reader.GetGuid(0),
                reader.GetString(3),
                reader.GetInt64(1),
                reader.GetDateTime(2));
            ticks = item.timestamp.Ticks;
            sid = item.sid;
            data.Add(item);
            count++;
        }

        string? newContinuationToken = null;

        if (count == pageSize && await reader.ReadAsync(cancellation))
        {
            using var stream = new MemoryStream();
            await using var writer = new Utf8JsonWriter(stream);

            var tokenData = new JsonObject
            {
                ["q"] = table,
                ["t"] = ticks, // timestamp of the last item on the returned page
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