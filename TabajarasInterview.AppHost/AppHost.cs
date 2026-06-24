var builder = DistributedApplication.CreateBuilder(args);

// API Rust
var rustApi = builder.AddContainer("rust-api", "rust-api")
    .WithHttpEndpoint(port: 8080, targetPort: 8080);

// Blazor
builder.AddProject<Projects.TabajarasInterview_Web>("web-frontend")
    .WithReference(rustApi.GetEndpoint("http"))
    .WaitFor(rustApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();