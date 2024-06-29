using Microsoft.Extensions.Logging;

namespace fetcherski.tools;

public class DummyAuthorization(ILogger<DummyAuthorization> logger) : IAuthorization
{
    private static readonly EventId AuthorizeAsyncEventId = new(1, nameof(IAuthorization.AuthorizeAsync));

    Task<bool> IAuthorization.AuthorizeAsync(string actionName, CancellationToken cancellationToken)
    {
        logger.LogInformation(AuthorizeAsyncEventId, "Authorized action '{actionName}'", actionName);
        return Task.FromResult(true);
    }
}