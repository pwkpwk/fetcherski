namespace fetcherski.service;

public class GrpcTagAttribute(string name) : Attribute
{
    public string Name => name;
}