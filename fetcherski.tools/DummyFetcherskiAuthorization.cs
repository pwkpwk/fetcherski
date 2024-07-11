using Microsoft.Extensions.Logging;

namespace fetcherski.tools;

/// <summary>
/// Illustrative implementation of <see cref="IFetcherskiAuthorization"/> that does not perform any meaningful checks.
/// </summary>
/// <param name="logger">Logger to write diagnostic messages.</param>
public class DummyFetcherskiAuthorization(ILogger<DummyFetcherskiAuthorization> logger) : IFetcherskiAuthorization, IAsyncDisposable
{
    private static readonly EventId AuthorizedEventId = new(100, nameof(IFetcherskiAuthorization.AuthorizeActionAsync));
    private static readonly EventId UnauthorizedEventId = new(101, nameof(IFetcherskiAuthorization.AuthorizeActionAsync));
    private static readonly EventId EndOfLifeEventId = new(103, nameof(IFetcherskiAuthorization.AuthorizeActionAsync));

    async Task<bool> IFetcherskiAuthorization.AuthorizeActionAsync(string actionName, CancellationToken cancellationToken)
    {
        bool authorized = actionName != "Unauthorized";
        if (authorized)
        {
            logger.LogInformation(AuthorizedEventId, "Authorized action '{actionName}'", actionName);
        }
        else
        {
            logger.LogError(UnauthorizedEventId, "Unauthorized action '{actionName}'", actionName);
        }

        await Task.Delay(100, cancellationToken);

        return authorized;
    }

    Task<bool> IFetcherskiAuthorization.AuthorizeTokenAsync(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public ValueTask DisposeAsync()
    {
        logger.LogTrace(EndOfLifeEventId, "End of the line");
        return ValueTask.CompletedTask;
    }
}