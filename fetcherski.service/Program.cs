using fetcherski.controllers;
using fetcherski.controllers.fs;
using fetcherski.database;
using fetcherski.database.Configuration;
using fetcherski.tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace fetcherski.service;

public static class Program
{
    public static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // The double underscore in the names of environment variables is the hierarchy separator.
        // The variable named "fetcherski.CockroachDB__Password" will provide value for the Password
        // property in the CockroachDB section.
        builder.Configuration.AddEnvironmentVariables("fetcherski.");
        builder.Services.Configure<CockroachConfig>(builder.Configuration.GetSection("CockroachDB"));

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddScoped<IDatabase, CockroachDatabase>();
        // Register the custom authorization provider as a disposable scoped object for illustration purposes only.
        // In practice, an object like this won't build up any state that must be discarded after authorizing
        // one incoming request.
        builder.Services.AddScoped<IFetcherskiAuthorization, DummyFetcherskiAuthorization>();
        
        // Main authorization handler called by the authorization middleware inserted by app.UseAuthorization below.  
        builder.Services.AddSingleton<IAuthorizationHandler, FetcherskiAuthorizationHandler>();
        // Handler of results of authorization produced by FetcherskiAuthorizationHandler registered above.
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationMiddlewareResultHandler>();
        
        // Register a singleton IHttpContextAccessor object that can be used by any composed object
        // to obtain the HTTP context associated with the current request.
        builder.Services.AddHttpContextAccessor();

        var mvcConfig = builder.Services.AddControllers();
        mvcConfig.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(DefaultController).Assembly));
        mvcConfig.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(SupplementalController).Assembly));

        builder.Services.AddGrpc();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                nameof(ActionNameRequirement), // Policy name used in the Authorize attribute applied to API controllers
                // Add ActionNameRequirement with the requirement turned on to the policy "ActionNameRequirement"
                policy => policy.AddRequirements(new ActionNameRequirement(TagRequired: true)));
            options.AddPolicy(
                nameof(KerbungleRequirement), // Policy name used in the Authorize attribute applied to API controllers
                // Add KerbungleRequirement with the requirement turned on to the policy "KerbungleRequirement"
                policy => policy.AddRequirements(new KerbungleRequirement(KerbungleTokenRequired: true)));
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();

        // Authentication is not needed here, but it can be used to add information about the calling user
        // to the request context.
        // app.UseAuthentication();
        
        // UseAuthentication & UseAuthorization must be placed between UseRouting and MapControllers
        // yo inject authorization middleware in the right order
        // app.UseAuthentication();
        app.UseAuthorization();

        app.MapGrpcService<GrpcFetcherskiService>();
        app.MapControllers();
        // app.UseHttpsRedirection();

        return app.RunAsync();
    }
}