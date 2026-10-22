using System.Text.Json;
using TabajarasInterview.Api.Configuration;
using TabajarasInterview.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddApiDocumentation();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapOpenApi(OpenApiExtensions.DocumentPath);

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.SwaggerEndpoint(OpenApiExtensions.DocumentPath, "Tabajaras Interview API");
});

// In Development the Web app calls the API over http; redirecting to https makes HttpClient
// drop the Authorization header on the redirected request.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapCandidateEndpoints();
app.MapPositionEndpoints();
app.MapLocationEndpoints();
app.MapStackEndpoints();
app.MapQuestionEndpoints();
app.MapCandidateApplicationEndpoints();
app.MapInterviewEndpoints();

await app.WaitForDatabaseAsync();

app.Run();


