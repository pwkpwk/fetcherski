using Microsoft.AspNetCore.Authorization;

namespace fetcherski.tools;

public record KerbungleRequirement(bool KerbungleTokenRequired) : IAuthorizationRequirement;