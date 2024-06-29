namespace fetcherski.tools;

public interface IAuthorization
{
    Task<bool> AuthorizeAsync(string actionName, CancellationToken cancellationToken);
}