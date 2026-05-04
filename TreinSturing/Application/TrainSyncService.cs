using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TreinSturing.Configuration;
using TreinSturing.Infrastructure;
using TreinSturing.Domain;

namespace TreinSturing.Application
{
    public sealed class TrainSyncService
    {
        private readonly IPlcReader _plcReader;
        private readonly ITrainController _trainController;
        private readonly AppSettings _settings;
        private readonly ILogSink _log;
        private readonly Dictionary<int, byte> _lastSpeedByDb = new Dictionary<int, byte>();
        private readonly Dictionary<int, byte> _lastDirectionByDb = new Dictionary<int, byte>();
        private readonly Dictionary<int, byte[]> _lastFunctionsByDb = new Dictionary<int, byte[]>();

        public TrainSyncService(IPlcReader plcReader, ITrainController trainController, AppSettings settings, ILogSink log)
        {
            _plcReader = plcReader;
            _trainController = trainController;
            _settings = settings;
            _log = log;
        }

        public IReadOnlyDictionary<int, byte> LastSpeedByDb => _lastSpeedByDb;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var rc = _plcReader.Connect(_settings.PlcIp, _settings.PlcRack, _settings.PlcSlot);
            if (rc != 0)
            {
                throw new InvalidOperationException("PLC connect error: code " + rc);
            }

            _log.Info($"PLC verbonden ({_settings.PlcIp}). Locomotief-DB's zoeken...");

            var discovery = new PlcDiscoveryService(_plcReader, _settings);
            var locomotiveDbs = discovery.DiscoverLocomotiveDbs();
            if (locomotiveDbs.Count == 0)
            {
                throw new InvalidOperationException("Geen locomotief-DB's gevonden in het scanbereik.");
            }

            _log.Info("Gevonden DB's: " + string.Join(", ", locomotiveDbs));
            _lastSpeedByDb.Clear();
            _lastDirectionByDb.Clear();
            _lastFunctionsByDb.Clear();

            await _trainController.ConnectAsync(cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var dbNumber in locomotiveDbs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await PollDbAsync(dbNumber, cancellationToken).ConfigureAwait(false);
                }

                await Task.Delay(_settings.PollIntervalMs, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task PollDbAsync(int dbNumber, CancellationToken cancellationToken)
        {
            var data = _plcReader.ReadDbBytes(dbNumber, _settings.PlcStart, _settings.PlcLength);
            if (data == null || data.Length < 3)
            {
                return;
            }

            // Afspraak:
            // DB-nummer = locadres.
            // De code maakt hier zelf de juiste mfx Loc-ID van.
            int locoAddress = dbNumber;

            var currentSpeed = data[2];
            await SyncSpeedAsync(dbNumber, locoAddress, currentSpeed, cancellationToken).ConfigureAwait(false);

            if (data.Length >= 4)
            {
                var currentDirection = data[3];
                await SyncDirectionAsync(dbNumber, locoAddress, currentDirection, cancellationToken).ConfigureAwait(false);
            }

            if (data.Length >= 36)
            {
                await SyncFunctionsAsync(dbNumber, locoAddress, data, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SyncSpeedAsync(int dbNumber, int locoAddress, byte currentSpeed, CancellationToken cancellationToken)
        {
            if (!_lastSpeedByDb.TryGetValue(dbNumber, out var lastSpeed))
            {
                _lastSpeedByDb[dbNumber] = currentSpeed;
                _log.Info($"DB{dbNumber}: eerste meting, snelheid = {currentSpeed}. Geen CS2-commando verzonden.");
                return;
            }

            if (lastSpeed == currentSpeed)
            {
                return;
            }

            _lastSpeedByDb[dbNumber] = currentSpeed;

            _log.Info($"DB{dbNumber}: snelheid gewijzigd van {lastSpeed} naar {currentSpeed}.");
            await _trainController.SetSpeedAsync(locoAddress, currentSpeed, cancellationToken).ConfigureAwait(false);
        }

        private async Task SyncDirectionAsync(int dbNumber, int locoAddress, byte currentDirection, CancellationToken cancellationToken)
        {
            if (currentDirection > 3)
            {
                _log.Info($"DB{dbNumber}: richting {currentDirection} genegeerd. Geldig is 0, 1, 2 of 3.");
                return;
            }

            if (!_lastDirectionByDb.TryGetValue(dbNumber, out var lastDirection))
            {
                _lastDirectionByDb[dbNumber] = currentDirection;
                return;
            }

            if (lastDirection == currentDirection)
            {
                return;
            }

            _lastDirectionByDb[dbNumber] = currentDirection;

            if (currentDirection == 0)
            {
                _log.Info($"DB{dbNumber}: richting 0 = ongewijzigd, geen CS2-commando verzonden.");
                return;
            }

            _log.Info($"DB{dbNumber}: richting gewijzigd van {lastDirection} naar {currentDirection}.");
            await _trainController.SetDirectionAsync(locoAddress, currentDirection, cancellationToken).ConfigureAwait(false);
        }

        private async Task SyncFunctionsAsync(int dbNumber, int locoAddress, byte[] data, CancellationToken cancellationToken)
        {
            if (!_lastFunctionsByDb.TryGetValue(dbNumber, out var lastFunctions))
            {
                lastFunctions = new byte[32];

                for (int functionNumber = 0; functionNumber < 32; functionNumber++)
                {
                    lastFunctions[functionNumber] = NormalizeFunctionValue(data[4 + functionNumber]);
                }

                _lastFunctionsByDb[dbNumber] = lastFunctions;
                return;
            }

            for (int functionNumber = 0; functionNumber < 32; functionNumber++)
            {
                byte currentValue = NormalizeFunctionValue(data[4 + functionNumber]);
                byte lastValue = lastFunctions[functionNumber];

                if (lastValue == currentValue)
                {
                    continue;
                }

                lastFunctions[functionNumber] = currentValue;

                _log.Info($"DB{dbNumber}: F{functionNumber} -> {(currentValue == 0 ? "uit" : "aan")}.");
                await _trainController.SetFunctionAsync(locoAddress, (byte)functionNumber, currentValue, cancellationToken).ConfigureAwait(false);
            }
        }

        private static byte NormalizeFunctionValue(byte value)
        {
            return value == 0 ? (byte)0 : (byte)1;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _plcReader.Disconnect();
            await _trainController.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            _log.Info("Synchronisatie gestopt. PLC disconnected.");
        }
    }
}
