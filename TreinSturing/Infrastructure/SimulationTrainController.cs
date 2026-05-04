using System.Threading;
using System.Threading.Tasks;
using TreinSturing.Domain;

namespace TreinSturing.Infrastructure
{
    public sealed class SimulationTrainController : ITrainController
    {
        private readonly ILogSink _log;
        private bool _connected;

        public SimulationTrainController(ILogSink log)
        {
            _log = log;
        }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            _connected = true;
            _log.Info("Simulation controller verbonden.");
            return Task.CompletedTask;
        }

        public Task SetSpeedAsync(int locoAddress, byte rawSpeed, CancellationToken cancellationToken)
        {
            if (!_connected)
                throw new System.InvalidOperationException("Simulation controller is niet verbonden.");

            _log.Info($"SIM TX SPEED -> loc={locoAddress}, rawSpeed={rawSpeed}");
            return Task.CompletedTask;
        }

        public Task SetDirectionAsync(int locoAddress, byte direction, CancellationToken cancellationToken)
        {
            if (!_connected)
                throw new System.InvalidOperationException("Simulation controller is niet verbonden.");

            _log.Info($"SIM TX DIRECTION -> loc={locoAddress}, direction={direction}");
            return Task.CompletedTask;
        }

        public Task SetFunctionAsync(int locoAddress, byte functionNumber, byte value, CancellationToken cancellationToken)
        {
            if (!_connected)
                throw new System.InvalidOperationException("Simulation controller is niet verbonden.");

            _log.Info($"SIM TX FUNCTION -> loc={locoAddress}, F{functionNumber}={(value == 0 ? "uit" : "aan")}");
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            if (_connected)
            {
                _connected = false;
                _log.Info("Simulation controller ontkoppeld.");
            }

            return Task.CompletedTask;
        }
    }
}