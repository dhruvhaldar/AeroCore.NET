using System.Collections.Generic;
using System.Threading;
using AeroCore.Shared.Models;

namespace AeroCore.Shared.Interfaces
{
    public interface ITelemetryProvider : IAeroComponent
    {
        IAsyncEnumerable<TelemetryPacket> StreamTelemetryAsync(CancellationToken ct);
    }
}
