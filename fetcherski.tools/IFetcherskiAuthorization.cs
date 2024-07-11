namespace fetcherski.tools;

public interface IFetcherskiAuthorization
{
    Task<bool> AuthorizeActionAsync(string actionName, CancellationToken cancellationToken);

    Task<bool> AuthorizeTokenAsync(string token, CancellationToken cancellationToken);
}