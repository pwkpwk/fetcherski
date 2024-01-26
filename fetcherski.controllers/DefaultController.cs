using fetcherski.client;
using fetcherski.database;
using Microsoft.AspNetCore.Mvc;

namespace fetcherski.controllers;

[ApiController, Route("api")]
public class DefaultController(IDatabase database) : Controller
{
    private const string ContinuationTokenHeader = "X-Continuation-Token";
    private const int DefaultPageSize = 10;

    [HttpGet, Route("query-loose-items"), Produces("application/json")]
    public async Task<Client.Item[]?> QueryLooseItemsAsync([FromQuery] int? pageSize, [FromQuery] string? order,
        CancellationToken cancellation)
    {
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
    public async Task<Client.Item[]?> QueryPackItemsAsync([FromQuery] int? pageSize, [FromQuery] string? order,
        CancellationToken cancellation)
    {
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
    public async Task<Client.Item[]?> ContinueQueryAsync(
        [FromHeader(Name = ContinuationTokenHeader)]
        string continuationToken,
        CancellationToken cancellation)
    {
        var result = await database.ContinueAsync(continuationToken, cancellation);

        if (result.ContinuationToken is not null)
        {
            Response.Headers[ContinuationTokenHeader] = result.ContinuationToken;
        }

        return result.Data ?? Array.Empty<Client.Item>();
    }
}