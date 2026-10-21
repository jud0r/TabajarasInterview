using System.Text.Json;
using TabajarasInterview.Api.Configuration;
using TabajarasInterview.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
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

app.UseHttpsRedirection();

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


