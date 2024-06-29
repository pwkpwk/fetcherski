using fetcherski.client;
using fetcherski.database;
using fetcherski.tools;
using Microsoft.AspNetCore.Mvc;

namespace fetcherski.controllers;

[ApiController, Route("api")]
public class DefaultController(
    IDatabase database,
    ILogger<DefaultController> logger) : Controller
{
    private const string ContinuationTokenHeader = "X-Continuation-Token";
    private const int DefaultPageSize = 10;

    private static readonly EventId QueryLooseItemsEventId = new(1, nameof(QueryLooseItemsAsync));
    private static readonly EventId QueryPackItemsEventId = new(2, nameof(QueryPackItemsAsync));
    private static readonly EventId ContinueEventId = new(3, nameof(ContinueQueryAsync));

    [HttpGet, Route("query-loose-items"), Produces("application/json")]
    [FetcherskiAuthorization]
    public async Task<Client.Item[]?> QueryLooseItemsAsync(
        [FromQuery] int? pageSize,
        [FromQuery] string? order,
        CancellationToken cancellation)
    {
        logger.LogInformation(QueryLooseItemsEventId, "pageSize={0}, order={1}", pageSize, order);
        
        var result = await database.QueryLooseItemsAsync(
            pageSize ?? DefaultPageSize,
            order is null
                ? IDatabase.Order.Ascending
                : Enum.Parse<IDatabase.Order>(order, true),
            cancellation);

        if (result.ContinuationToken is not null)
        {
            Response.Headers[ContinuationTokenHeader] = result.ContinuationToken;
        }

        return result.Data ?? Array.Empty<Client.Item>();
    }

    [HttpGet, Route("query-pack-items"), Produces("application/json")]
    [FetcherskiAuthorization(nameof(QueryPackItemsAsync))]
    public async Task<Client.Item[]?> QueryPackItemsAsync([FromQuery] int? pageSize, [FromQuery] string? order,
        CancellationToken cancellation)
    {
        logger.LogInformation(QueryPackItemsEventId, "pageSize={0}, order={1}", pageSize, order);
        
        var result = await database.QueryPackItemsAsync(
            pageSize ?? DefaultPageSize,
            order is null
                ? IDatabase.Order.Ascending
                : Enum.Parse<IDatabase.Order>(order, true),
            cancellation);

        if (result.ContinuationToken is not null)
        {
            Response.Headers[ContinuationTokenHeader] = result.ContinuationToken;
        }

        return result.Data ?? Array.Empty<Client.Item>();
    }

    [HttpGet, Route("continue"), Produces("application/json")]
    [FetcherskiAuthorization("Just Some Trash")]
    public async Task<Client.Item[]?> ContinueQueryAsync(
        [FromHeader(Name = ContinuationTokenHeader)]
        string continuationToken,
        CancellationToken cancellation)
    {
        logger.LogInformation(ContinueEventId, "token length={0}", continuationToken.Length);
        
        var result = await database.ContinueAsync(continuationToken, cancellation);

        if (result.ContinuationToken is not null)
        {
            Response.Headers[ContinuationTokenHeader] = result.ContinuationToken;
        }

        return result.Data ?? Array.Empty<Client.Item>();
    }

    [HttpGet, Route("unauthorized")]
    [FetcherskiAuthorization]
    public Task UnauthorizedAsync(CancellationToken _) => Task.CompletedTask;
}