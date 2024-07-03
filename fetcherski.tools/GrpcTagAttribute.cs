namespace fetcherski.tools;

public class GrpcTagAttribute(string name) : Attribute
{
    public string Name => name;
}