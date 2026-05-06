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
            if (data == null || data.Length <= 1)
            {
                return;
            }

            var currentSpeed = data[1];
            if (_lastSpeedByDb.TryGetValue(dbNumber, out var lastSpeed) && lastSpeed == currentSpeed)
            {
                return;
            }

            if (_lastSpeedByDb.ContainsKey(dbNumber))
            {
                _log.Info($"DB{dbNumber}: snelheid gewijzigd van {lastSpeed} naar {currentSpeed}.");
            }
            else
            {
                _log.Info($"DB{dbNumber}: eerste meting, snelheid = {currentSpeed}.");
            }

            _lastSpeedByDb[dbNumber] = currentSpeed;
            await _trainController.SetSpeedAsync(dbNumber, currentSpeed, cancellationToken).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _plcReader.Disconnect();
            await _trainController.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            _log.Info("Synchronisatie gestopt. PLC disconnected.");
        }
    }
}
