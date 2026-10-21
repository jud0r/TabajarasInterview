var builder = DistributedApplication.CreateBuilder(args);

// Shared HS256 signing secret. The API signs access tokens with Jwt__Secret and the Blazor
// frontend validates them on the server with the same Jwt__Secret, so both must use the SAME value.
var jwtSecret = builder.AddParameter("jwt-secret", secret: true);

// ASP.NET Core minimal API.
var api = builder.AddProject<Projects.TabajarasInterview_Api>("tabajarasinterview-api")
    .WithEnvironment("Jwt__Secret", jwtSecret);

// Blazor
builder.AddProject<Projects.TabajarasInterview_Web>("web-frontend")
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("http"))
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();