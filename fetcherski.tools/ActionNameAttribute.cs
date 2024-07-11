namespace fetcherski.tools;

public class ActionNameAttribute(string action) : Attribute
{
    public string Action => action;
}