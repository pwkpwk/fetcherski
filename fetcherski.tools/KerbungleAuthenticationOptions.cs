using Microsoft.AspNetCore.Authentication;

namespace fetcherski.tools;

public class KerbungleAuthenticationOptions : AuthenticationSchemeOptions
{
    public static readonly string Scheme = "Kerbungle";

    public static void Configure(KerbungleAuthenticationOptions options)
    {
    }
}