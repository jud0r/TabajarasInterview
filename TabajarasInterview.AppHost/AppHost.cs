var builder = DistributedApplication.CreateBuilder(args);

// API Rust
builder.AddContainer("rust-api", "rust-api")
    .WithHttpEndpoint(port: 8080, targetPort: 8080);

// Blazor
builder.AddProject<Projects.TabajarasInterview_Web>("web-frontend")
    .WithExternalHttpEndpoints();

builder.Build().Run();