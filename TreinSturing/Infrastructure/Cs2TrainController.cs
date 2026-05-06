using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TreinSturing.Infrastructure
{
    public sealed class Cs2TrainController : TreinSturing.Domain.ITrainController
    {
        private readonly string _host;
        private readonly int _port;
        private readonly ILogSink _log;

        private UdpClient _udp;
        private IPEndPoint _remote;

        // Kies een vaste, unieke node UID voor jouw app.
        // Die gebruik je alleen voor hash-opbouw.
        private const uint MyNodeUid = 0x43533201;

        public Cs2TrainController(string host, int port, ILogSink log)
        {
            _host = host;
            _port = port;
            _log = log;
        }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_udp != null)
                return Task.CompletedTask;

            _udp = new UdpClient();
            _remote = new IPEndPoint(IPAddress.Parse(_host), _port);

            _log.Info($"CS2 UDP klaar voor {_host}:{_port}");
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            try { _udp?.Dispose(); } catch { }
            _udp = null;
            _remote = null;
            _log.Info("CS2 UDP disconnected.");
            return Task.CompletedTask;
        }

        public async Task SetSpeedAsync(int locoAddress, byte rawSpeed, CancellationToken cancellationToken)
        {
            if (_udp == null)
                await ConnectAsync(cancellationToken).ConfigureAwait(false);

            // Eerste werkende versie: ga uit van MM2-locadressen.
            uint locId = (uint)locoAddress;

            // Oude PLC-waarde zat effectief in 0..31.
            int oldScale = rawSpeed & 0x1F;

            // Nieuwe CS2-schaal is 0..1000.
            ushort cs2Speed = (ushort)Math.Round(oldScale * 1000.0 / 31.0);

            byte[] packet = BuildSpeedPacket(locId, cs2Speed);

            await _udp.SendAsync(packet, packet.Length, _remote).ConfigureAwait(false);

            _log.Info($"CS2 TX -> loc={locoAddress}, locId=0x{locId:X8}, raw={rawSpeed}, speed={cs2Speed}, hex={BitConverter.ToString(packet)}");
        }

        private static byte[] BuildSpeedPacket(uint locId, ushort speed)
        {
            ushort hash = GenerateHash(MyNodeUid);

            // command 0x04 -> CAN-ID command part 0x08 volgens protocolvoorbeelden
            uint canId = ((uint)0x04 << 17) | hash;

            byte[] packet = new byte[13];

            // 4 bytes CAN-ID big-endian
            packet[0] = (byte)((canId >> 24) & 0xFF);
            packet[1] = (byte)((canId >> 16) & 0xFF);
            packet[2] = (byte)((canId >> 8) & 0xFF);
            packet[3] = (byte)(canId & 0xFF);

            // DLC = 6
            packet[4] = 0x06;

            // data[0..3] = Loc-ID big-endian
            packet[5] = (byte)((locId >> 24) & 0xFF);
            packet[6] = (byte)((locId >> 16) & 0xFF);
            packet[7] = (byte)((locId >> 8) & 0xFF);
            packet[8] = (byte)(locId & 0xFF);

            // data[4..5] = snelheid big-endian
            packet[9] = (byte)((speed >> 8) & 0xFF);
            packet[10] = (byte)(speed & 0xFF);

            // data[6..7] padding
            packet[11] = 0x00;
            packet[12] = 0x00;

            return packet;
        }

        private static ushort GenerateHash(uint uid)
        {
            ushort high = (ushort)(uid >> 16);
            ushort low = (ushort)(uid & 0xFFFF);
            ushort x = (ushort)(high ^ low);

            // Veelgebruikte implementatie van de hash-encoding voor CS2 CAN
            return (ushort)((((x << 3) & 0xFF00) | 0x0300) | (x & 0x007F));
        }
    }
}