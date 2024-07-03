using Microsoft.AspNetCore.Authorization;

namespace fetcherski.service;

public record GrpcTagRequirement(bool TagRequired) : IAuthorizationRequirement;
