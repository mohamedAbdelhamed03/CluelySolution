using Cluely.Api.Infrastructure;

namespace Cluely.Api.Mapping;

public static class CorrelationIdAccessor
{
    public static Guid GetCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(CorrelationId.ItemKey, out var value)
            && value is string correlationId
            && Guid.TryParse(correlationId, out var parsed))
        {
            return parsed;
        }

        return Guid.NewGuid();
    }
}
