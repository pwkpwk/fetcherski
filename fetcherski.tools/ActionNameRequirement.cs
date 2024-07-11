using Microsoft.AspNetCore.Authorization;

namespace fetcherski.tools;

public record ActionNameRequirement(bool TagRequired) : IAuthorizationRequirement;
