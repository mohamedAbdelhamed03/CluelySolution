using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cluely.Api.Infrastructure;

public sealed class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.ApiDescription.HttpMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relativePath = context.ApiDescription.RelativePath;
        var isCreate = string.Equals(relativePath, "api/content", StringComparison.OrdinalIgnoreCase);
        var isClone = relativePath?.EndsWith("/clone", StringComparison.OrdinalIgnoreCase) == true;
        var isPublish = relativePath?.EndsWith("/publish", StringComparison.OrdinalIgnoreCase) == true;
        if (!isCreate && !isClone && !isPublish)
        {
            return;
        }

        operation.Parameters ??= [];
        var parameter = operation.Parameters.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, IdempotencyKeyAccessor.HeaderName, StringComparison.OrdinalIgnoreCase));

        if (parameter is null)
        {
            parameter = new OpenApiParameter
            {
                Name = IdempotencyKeyAccessor.HeaderName,
                In = ParameterLocation.Header,
                Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
            };
            operation.Parameters.Add(parameter);
        }

        parameter.Required = isPublish;
        parameter.Description = isPublish
            ? "Required UUID. Retrying with the same key returns the original published version."
            : "Optional UUID. Reuse the same key to replay this command without creating a duplicate.";
        parameter.Example = new OpenApiString("7e7d0195-1d35-4ec8-b6d5-8d79fb77b509");
    }
}
