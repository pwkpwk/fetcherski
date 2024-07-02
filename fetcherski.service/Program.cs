using fetcherski.controllers;
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
        builder.Services.AddScoped<IAuthorization, DummyAuthorization>();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationMiddlewareResultHandler>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IAuthorizationHandler, GrpcAuthorizationHandler>();

        var mvcConfig = builder.Services.AddControllers();
        mvcConfig.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(DefaultController).Assembly));

        builder.Services.AddGrpc();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(nameof(GrpcAuthorization), policy =>
            {
                policy.AddRequirements(new GrpcAuthorization());
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        
        // UseAuthentication & UseAuthorization must be placed between UseRouting and MapControllers
        // yo inject authorization middleware in the right order
        // app.UseAuthentication();
        app.UseAuthorization();
        // app.UseAuthentication();

        app.MapGrpcService<FetcherskiService>();
        app.MapControllers();
        // app.UseHttpsRedirection();

        return app.RunAsync();
    }
}