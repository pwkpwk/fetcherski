namespace fetcherski.database.Configuration;

public class CockroachConfig
{
    public string? Host { get; set; }

    public int Port { get; set; } = 26257;
    
    public string? Database { get; set; }
    
    public string? Schema { get; set; }
    
    public string? User { get; set; }
    
    public string? Password { get; set; }
}