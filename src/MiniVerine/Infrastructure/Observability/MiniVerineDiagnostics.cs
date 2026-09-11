using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MiniVerine.Infrastructure.Observability;

/// <summary>
/// Shared BCL diagnostics surface for MiniVerine. No-op by default: nothing is exported
/// until an external <see cref="ActivityListener"/> subscribes to <see cref="Source"/>
/// or an external <see cref="MeterListener"/> subscribes to instruments on <see cref="Meter"/>.
/// </summary>
public static class MiniVerineDiagnostics
{
    public const string SourceName = "MiniVerine";
    public const string MeterName = "MiniVerine";

    public static readonly ActivitySource Source = new(SourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> Failures =
        Meter.CreateCounter<long>("miniverine.failures");
}
