using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace TabajarasInterview.Api.Configuration;

/// <summary>OpenAPI setup (tags and bearer scheme), mirroring the Rust ApiDoc and SecurityAddon.</summary>
public static class OpenApiExtensions
{
    public const string DocumentPath = "/api-docs/openapi.json";

    private static readonly (string Name, string Description)[] Tags =
    [
        ("auth", "Authentication endpoints"),
        ("users", "User management endpoints"),
        ("candidates", "Candidate management endpoints"),
        ("positions", "Position management endpoints"),
        ("candidateApplications", "Candidate application management endpoints"),
        ("stacks", "Stack management endpoints"),
        ("questions", "Question management endpoints"),
        ("interviews", "Interview management endpoints"),
        ("interviewQuestions", "Interview question management endpoints"),
        ("interviewReviewers", "Interview reviewer management endpoints"),
        ("positionStacks", "Position stack management endpoints"),
        ("locations", "Location endpoints"),
    ];

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Tags = new HashSet<OpenApiTag>(
                    Tags.Select(t => new OpenApiTag { Name = t.Name, Description = t.Description }));

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["bearerAuth"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                };

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var requiresAuth = context.Description.ActionDescriptor.EndpointMetadata
                    .OfType<IAuthorizeData>()
                    .Any();

                if (requiresAuth)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("bearerAuth", context.Document)] = [],
                    });
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
