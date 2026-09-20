var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.DuongNhan_project2_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.DuongNhan_project2_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
