var builder = DistributedApplication.CreateBuilder(args);

// Shared HS256 signing secret. The Rust API signs access tokens with SECRET and the Blazor
// frontend validates them on the server with Jwt__Secret, so both resources must receive the
// SAME value. HS256 also requires a key of at least 128 bits, so this must be a sufficiently
// long secret (its default value lives in appsettings.json under Parameters:jwt-secret).
var jwtSecret = builder.AddParameter("jwt-secret", secret: true);

// API Rust
var rustApi = builder.AddContainer("rust-api", "rust-api")
    .WithHttpEndpoint(port: 8080, targetPort: 8080)
    .WithEnvironment("SECRET", jwtSecret);

// Blazor
builder.AddProject<Projects.TabajarasInterview_Web>("web-frontend")
    .WithReference(rustApi.GetEndpoint("http"))
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WaitFor(rustApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();