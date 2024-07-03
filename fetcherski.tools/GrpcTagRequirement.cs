using Microsoft.AspNetCore.Authorization;

namespace fetcherski.tools;

public record GrpcTagRequirement(bool TagRequired) : IAuthorizationRequirement;
