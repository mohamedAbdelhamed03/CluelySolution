using Cluely.Application.Common.Ports;

namespace Cluely.Infrastructure.Common;

public sealed class GuidGenerator : IGuidGenerator
{
    public Guid Generate() => Guid.NewGuid();
}
