using fetcherski.database;
using Microsoft.AspNetCore.Mvc;

namespace fetcherski.controllers;

[Route("api")]
public class DefaultController(IDatabase database) : Controller
{
    private const string ContinuationTokenHeader = "x-continuation-token";
    
    [HttpGet, Route("query")]
    public async Task<string[]?> StartQuery(CancellationToken cancellation)
    {
        var result = await database.StartQuery(cancellation);

        if (result.ContinuationToken is not null)
        {
            Response.Headers[ContinuationTokenHeader] = result.ContinuationToken;
        }
        
        return result.Data;
    }

    [HttpGet, Route("continue")]
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
        
        return result.Data;
    }
}