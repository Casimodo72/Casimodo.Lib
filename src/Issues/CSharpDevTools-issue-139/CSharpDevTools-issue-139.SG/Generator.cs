using Microsoft.CodeAnalysis;

namespace Issue;

[Generator]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // NOOP
    }
}
