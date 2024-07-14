using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using CommunityToolkit.HighPerformance;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace fetcherski.tools;

public class KerbungleAuthentication(
    IOptionsMonitor<KerbungleAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder) : AuthenticationHandler<KerbungleAuthenticationOptions>(options, loggerFactory, encoder)
{
    private static readonly EventId UnauthenticatedEventId = new EventId(1, "Unauthenticated");
    private static readonly EventId AuthenticatedEventId = new EventId(2, "Authenticated");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var logger = loggerFactory.CreateLogger<KerbungleAuthentication>();
        ReadOnlySpan<char> kerbungle = Scheme.Name.AsSpan();

        foreach (string? headerValue in Request.Headers.Authorization)
        {
            if (headerValue is not null)
            {
                string? token = RetrieveAuthorizationToken(headerValue, kerbungle);

                if (token is not null)
                {
                    return ValidateTokenAsync(token, logger, Request.HttpContext.RequestAborted);
                }
            }
        }

        logger.LogTrace(UnauthenticatedEventId, "No authentication | {endpoint}", Request.Path);
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    private async Task<AuthenticateResult> ValidateTokenAsync(
        string token,
        ILogger logger,
        CancellationToken cancellation)
    {
        await Task.Delay(50, cancellation);
        var identity = new ClaimsIdentity([
            new(ClaimTypes.Name, token),
            new(ClaimTypes.Role, "service"),
        ], Scheme.Name);
        var principal = new GenericPrincipal(identity, ["service"]);
        identity.Actor = null;
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        logger.LogTrace(AuthenticatedEventId, "Authenticated | {endpoint}", Request.Path);
        return AuthenticateResult.Success(ticket);
    }

    private string? RetrieveAuthorizationToken(string authorizationHeader, ReadOnlySpan<char> type)
    {
        int i = 0;

        foreach (var token in authorizationHeader.Tokenize(' '))
        {
            switch (i++)
            {
                case 0:
                    if (!token.SequenceEqual(type))
                    {
                        return null;
                    }

                    break;

                case 1:
                    return token.ToString();

                default:
                    return null;
            }
        }

        return null;
    }
}