namespace fetcherski.database;

public record QueryResult<T>(string? ContinuationToken, T[]? Data);