using fetcherski.controllers;
using fetcherski.database;
using fetcherski.database.Configuration;
using fetcherski.tools;
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

        var mvcConfig = builder.Services.AddControllers();
        mvcConfig.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(DefaultController).Assembly));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.MapControllers();
        app.UseHttpsRedirection();

        return app.RunAsync();
    }
}