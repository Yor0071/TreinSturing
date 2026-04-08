using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TreinSturing.Configuration;
using TreinSturing.Domain;

namespace TreinSturing.Infrastructure
{
    public sealed class Cs2TrainController : ITrainController
    {
        private readonly AppSettings _settings;
        private readonly ILogSink _log;
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public Cs2TrainController(AppSettings settings, ILogSink log)
        {
            _settings = settings;
            _log = log;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_tcpClient != null && _tcpClient.Connected)
            {
                return;
            }

            _tcpClient = new TcpClient();
            var connectTask = _tcpClient.ConnectAsync(_settings.Cs2Host, _settings.Cs2Port);
            var timeoutTask = Task.Delay(_settings.Cs2ConnectTimeoutMs, cancellationToken);
            var completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

            if (completed != connectTask || !_tcpClient.Connected)
            {
                throw new TimeoutException($"Kan niet verbinden met CS2 op {_settings.Cs2Host}:{_settings.Cs2Port}.");
            }

            _stream = _tcpClient.GetStream();
            _log.Info($"CS2 TCP verbonden met {_settings.Cs2Host}:{_settings.Cs2Port}.");
        }

        public async Task SetSpeedAsync(int locoAddress, byte rawSpeed, CancellationToken cancellationToken)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("CS2 transport is niet verbonden.");
            }

            var frame = BuildSetSpeedFrame(locoAddress, rawSpeed);
            await _stream.WriteAsync(frame, 0, frame.Length, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            _log.Info($"CS2 TX -> loc={locoAddress}, rawSpeed={rawSpeed}, bytes={BitConverter.ToString(frame)}");
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }

                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient.Dispose();
                    _tcpClient = null;
                }

                _log.Info("CS2 verbinding gesloten.");
            }
            catch (Exception ex)
            {
                _log.Error("Fout bij sluiten CS2 verbinding: " + ex.Message);
            }

            return Task.CompletedTask;
        }

        private static byte[] BuildSetSpeedFrame(int locoAddress, byte rawSpeed)
        {
            // BELANGRIJK:
            // Dit is bewust een geïsoleerde placeholder. De app-architectuur is nu refactored,
            // maar het exacte Märklin CAN/TCP frame voor 'set locomotiefsnelheid' moet nog
            // definitief uit de protocolreferentie worden overgenomen en getest op jouw CS2.
            // Tot die tijd gooien we een duidelijke fout i.p.v. ongeldige bytes te sturen.
            throw new NotSupportedException(
                "CS2 protocolframe voor snelheid is nog niet ingevuld. Gebruik tijdelijk Controller.Type=Simulation totdat het definitieve frame is toegevoegd.");
        }
    }
}
