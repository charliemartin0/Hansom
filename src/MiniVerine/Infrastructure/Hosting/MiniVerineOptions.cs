using System.Reflection;

namespace MiniVerine;

public sealed class MiniVerineOptions
{
    public ICollection<Assembly> HandlerAssemblies { get; } = new List<Assembly>();
}
