using System.Threading;
using System.Threading.Tasks;

namespace TreinSturing.Domain
{
    public interface ITrainController
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task SetSpeedAsync(int locoAddress, byte rawSpeed, CancellationToken cancellationToken);
        Task SetDirectionAsync(int locoAddress, byte direction, CancellationToken cancellationToken);
        Task SetFunctionAsync(int locoAddress, byte functionNumber, byte value, CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
    }
}
