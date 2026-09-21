using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var isTestOrCi = builder.Environment.IsEnvironment("Testing")
    || builder.Configuration.GetValue<bool>("UseEphemeralContainers");

var postgresBuilder = builder.AddPostgres("postgres")
    .WithImage("postgres:17-alpine");

if (!isTestOrCi)
{
    postgresBuilder
        .WithDataVolume("duongnhan-postgres-data", isReadOnly: false)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithPgAdmin(pgAdmin => pgAdmin.WithLifetime(ContainerLifetime.Persistent));
}

var postgresdb = postgresBuilder.AddDatabase("postgresdb");

var apiService = builder.AddProject<Projects.DuongNhan_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

builder.AddProject<Projects.DuongNhan_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();