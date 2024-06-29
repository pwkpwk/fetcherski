using Microsoft.Extensions.Logging;

namespace fetcherski.tools;

public class DummyAuthorization(ILogger<DummyAuthorization> logger) : IAuthorization
{
    private static readonly EventId AuthorizedAsyncEventId = new(100, nameof(IAuthorization.AuthorizeAsync));
    private static readonly EventId UnauthorizedAsyncEventId = new(101, nameof(IAuthorization.AuthorizeAsync));

    Task<bool> IAuthorization.AuthorizeAsync(string actionName, CancellationToken cancellationToken)
    {
        bool authorized = actionName != "Unauthorized";
        if (authorized)
        {
            logger.LogInformation(AuthorizedAsyncEventId, "Authorized action '{actionName}'", actionName);
        }
        else
        {
            logger.LogError(UnauthorizedAsyncEventId, "Unauthorized action '{actionName}'", actionName);
        }
        return Task.FromResult(authorized);
    }
}