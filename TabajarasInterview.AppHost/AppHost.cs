var builder = DistributedApplication.CreateBuilder(args);

// Shared HS256 signing secret. The Rust API signs access tokens with SECRET and the Blazor
// frontend validates them on the server with Jwt__Secret, so both must use the SAME value.
var jwtSecret = builder.AddParameter("jwt-secret", secret: true);

// Rust API now runs outside Aspire (cargo run / VS Code), no Docker needed.
var rustApiUrl = builder.Configuration["RustApi:BaseUrl"] ?? "http://localhost:8080";

// Blazor
builder.AddProject<Projects.TabajarasInterview_Web>("web-frontend")
    .WithEnvironment("RustApi__BaseUrl", rustApiUrl)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithExternalHttpEndpoints();

builder.Build().Run();