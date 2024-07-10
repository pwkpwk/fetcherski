namespace fetcherski.tools;

public interface IAuthorization
{
    Task<bool> AuthorizeActionAsync(string actionName, CancellationToken cancellationToken);

    Task<bool> AuthorizeTokenAsync(string token, CancellationToken cancellationToken);
}