using fetcherski.database;
using Microsoft.AspNetCore.Mvc;

namespace fetcherski.controllers;

[ApiController, Route("api")]
public class DefaultController(IDatabase database) : Controller
{
    private const string ContinuationTokenHeader = "X-Continuation-Token";
    
    [HttpGet, Route("query"), Produces("application/json")]
    public async Task<string[]?> StartQuery(CancellationToken cancellation)
    {
        var result = await database.StartQuery(cancellation);

        if (result.ContinuationToken is not null)
        {
            Response.Headers[ContinuationTokenHeader] = result.ContinuationToken;
        }
        
        return result.Data ?? Array.Empty<string>();
    }

    [HttpGet, Route("continue"), Produces("application/json")]
    public async Task<string[]?> ContinueQuery(CancellationToken cancellation)
    {
        var tokens = Request.Headers[ContinuationTokenHeader];
        if (tokens.Count != 1)
        {
            Response.StatusCode = 400;
            await Response.CompleteAsync();
            return null;
        }

        var token = tokens[0];
        var result = await database.ContinueQuery(token, cancellation);

        if (result.ContinuationToken is not null)
        {
            Response.Headers[ContinuationTokenHeader] = result.ContinuationToken;
        }
        
        return result.Data ?? Array.Empty<string>();
    }
}