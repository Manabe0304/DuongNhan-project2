var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("duongnhan-postgres-data", isReadOnly: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithImage("postgres:17-alpine")
    .WithPgAdmin(pgAdmin => pgAdmin
        .WithLifetime(ContainerLifetime.Persistent)
        .WithImage("dpage/pgadmin4", "9.17"));

var postgresdb = postgres.AddDatabase("postgresdb");

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