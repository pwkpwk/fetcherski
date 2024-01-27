using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using fetcherski.client;
using fetcherski.database.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace fetcherski.database;

public sealed class CockroachDatabase(
    IOptionsSnapshot<CockroachConfig> options,
    ILogger<CockroachDatabase> logger) : IDatabase, IDisposable
{
    private readonly CockroachConfig _config = options.Value;
    private readonly CancellationTokenSource _cts = new();
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

    private static readonly EventId TokenDecodeError = new(1, nameof(TokenDecodeError));
    private static readonly EventId SkipCount = new(2, nameof(SkipCount));

    Task<QueryResult<Client.Item>> IDatabase.QueryLooseItemsAsync(
        int pageSize,
        IDatabase.Order order,
        CancellationToken cancellation) => QueryTableAsync("looseitems", pageSize, order, cancellation);

    Task<QueryResult<Client.Item>> IDatabase.QueryPackItemsAsync(
        int pageSize,
        IDatabase.Order order,
        CancellationToken cancellation) => QueryTableAsync("pack_contents_view", pageSize, order, cancellation);

    async Task<QueryResult<Client.Item>> IDatabase.ContinueAsync(
        string continuationToken,
        CancellationToken cancellation)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var node = await DecodeContinuationTokenAsync(continuationToken, cts.Token);

        if (node is null)
        {
            logger.LogError(TokenDecodeError, "Unable to decode the continuation token.");
            return new QueryResult<Client.Item>(null, null);
        }

        var decodedToken = node.AsObject();
        string table = decodedToken["q"]!.GetValue<string>();
        DateTime timestamp = new DateTime(decodedToken["t"]!.GetValue<long>());
        long sequentialId = decodedToken["s"]!.GetValue<long>();
        string sqlOrder = decodedToken["o"]!.GetValue<string>();
        int pageSize = decodedToken["p"]!.GetValue<int>();
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
        await using var reader = await OpenReaderAsyncAsync(
            db,
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

    private async Task<bool> SkipReaderAsync(
        DbDataReader reader,
        long sequentialId,
        CancellationToken cancellation)
    {
        int count = 0;

        while (await reader.ReadAsync(cancellation))
        {
            ++count;
            if (sequentialId == reader.GetInt64(1))
            {
                logger.LogInformation(SkipCount, "Skipped {0}", count);
                return true;
            }
        }

        return false;
    }

    private async Task<QueryResult<Client.Item>> ProcessReaderAsync(
        DbDataReader reader,
        string table,
        int pageSize,
        string sqlOrder,
        CancellationToken cancellation)
    {
        var data = new List<Client.Item>(pageSize);
        Client.Item item = default;
        int count = 0;

        while (count < pageSize && await reader.ReadAsync(cancellation))
        {
            item = new Client.Item(
                reader.GetGuid(0),
                reader.GetString(3),
                reader.GetInt64(1),
                reader.GetDateTime(2));
            data.Add(item);
            count++;
        }

        string? newContinuationToken = null;

        if (count == pageSize && await reader.ReadAsync(cancellation))
        {
            newContinuationToken = MakeContinuationToken(item, sqlOrder, pageSize, table);
        }

        return new QueryResult<Client.Item>(newContinuationToken, data.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string MakeContinuationToken(
        Client.Item item,
        string sqlOrder,
        int pageSize,
        string table)
    {
        byte[] buffer = _bufferPool.Rent(1024);

        try
        {
            using var stream = new MemoryStream(buffer, true);
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();
            writer.WriteString("q", table);
            writer.WriteNumber("t", item.timestamp.Ticks);
            writer.WriteNumber("s", item.sid);
            writer.WriteString("o", sqlOrder);
            writer.WriteNumber("p", pageSize);
            writer.WriteEndObject();
            writer.Flush();

            return Convert.ToBase64String(buffer, 0, (int)stream.Position);
        }
        finally
        {
            _bufferPool.Return(buffer);
        }
    }
}