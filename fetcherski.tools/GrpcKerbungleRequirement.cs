using Microsoft.AspNetCore.Authorization;

namespace fetcherski.tools;

public record GrpcKerbungleRequirement(bool KerbungleTokenRequired) : IAuthorizationRequirement;