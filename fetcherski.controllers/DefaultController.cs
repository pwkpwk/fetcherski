using fetcherski.client;
using fetcherski.database;
using Microsoft.AspNetCore.Mvc;

namespace fetcherski.controllers;

[ApiController, Route("api")]
public class DefaultController(IDatabase database) : Controller
{
    private const string ContinuationTokenHeader = "X-Continuation-Token";
    private const int DefaultPageSize = 10;

    [HttpGet, Route("query"), Produces("application/json")]
    public async Task<Client.Item[]?> StartQuery([FromQuery] int? pageSize, [FromQuery] string? order,
        CancellationToken cancellation)
    {
        var result = await database.StartQuery(
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
    public async Task<Client.Item[]?> ContinueQuery(
        [FromHeader(Name = ContinuationTokenHeader)]
        string continuationToken,
        CancellationToken cancellation)
    {
        var result = await database.ContinueQuery(continuationToken, cancellation);

        if (result.ContinuationToken is not null)
        {
            Response.Headers[ContinuationTokenHeader] = result.ContinuationToken;
        }

        return result.Data ?? Array.Empty<Client.Item>();
    }
}