namespace fetcherski.tools;

public class GrpcTagAttribute(string action) : Attribute
{
    public string Action => action;
}