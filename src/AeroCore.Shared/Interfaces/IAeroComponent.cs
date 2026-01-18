using System.Threading;
using System.Threading.Tasks;

namespace AeroCore.Shared.Interfaces
{
    public interface IAeroComponent
    {
        Task InitializeAsync(CancellationToken ct);
    }
}
