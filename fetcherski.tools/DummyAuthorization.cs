using Microsoft.Extensions.Logging;

namespace fetcherski.tools;

public class DummyAuthorization(ILogger<DummyAuthorization> logger) : IAuthorization, IAsyncDisposable
{
    private static readonly EventId AuthorizedEventId = new(100, nameof(IAuthorization.AuthorizeActionAsync));
    private static readonly EventId UnauthorizedEventId = new(101, nameof(IAuthorization.AuthorizeActionAsync));
    private static readonly EventId EndOfLifeEventId = new(103, nameof(IAuthorization.AuthorizeActionAsync));

    async Task<bool> IAuthorization.AuthorizeActionAsync(string actionName, CancellationToken cancellationToken)
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

    Task<bool> IAuthorization.AuthorizeTokenAsync(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public ValueTask DisposeAsync()
    {
        logger.LogTrace(EndOfLifeEventId, "End of the line");
        return ValueTask.CompletedTask;
    }
}