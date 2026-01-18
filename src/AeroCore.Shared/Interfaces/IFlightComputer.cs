using System.Threading;
using System.Threading.Tasks;

namespace AeroCore.Shared.Interfaces
{
    public interface IFlightComputer : IAeroComponent
    {
        Task ProcessLoopAsync(CancellationToken ct);
    }
}
