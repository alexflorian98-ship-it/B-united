using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace BUnited.Api.OpenApi;

/// <summary>
/// Registers the "Bearer" JWT security scheme on the generated OpenAPI document so Swagger UI
/// renders an "Authorize" button. Applied via <c>AddOpenApi(options => ...)</c> in Program.cs.
/// </summary>
public sealed class BearerSecuritySchemeDocumentTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT access token issued by the Identity module's login/refresh endpoints.",
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Adds a "Bearer" security requirement to any operation whose endpoint carries
/// <see cref="IAuthorizeData"/> (i.e. an <c>[Authorize]</c> attribute or policy), so Swagger UI
/// only prompts for a token on endpoints that actually require one.
/// </summary>
public sealed class BearerSecurityRequirementOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (!requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = BearerSecuritySchemeDocumentTransformer.SchemeName,
                },
            }] = Array.Empty<string>(),
        });

        return Task.CompletedTask;
    }
}
