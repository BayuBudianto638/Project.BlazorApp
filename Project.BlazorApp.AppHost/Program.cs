var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.Project_BlazorApp_ApiService>("apiservice");

builder.AddProject<Projects.Project_BlazorApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(apiService);

builder.AddProject<Projects.Project_ServiceApi>("project-serviceapi");

builder.Build().Run();
