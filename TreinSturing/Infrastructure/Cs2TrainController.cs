using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TreinSturing.Configuration;
using TreinSturing.Domain;

namespace TreinSturing.Infrastructure
{
    public sealed class Cs2TrainController : ITrainController
    {
        private readonly string _host;
        private readonly int _port;
        private readonly ILogSink _log;

        private UdpClient _udp;
        private IPEndPoint _remote;

        private const byte PrioLocCommand = 4;
        private const byte CommandSetSpeed = 0x04;
        private const byte CommandSetDirection = 0x05;
        private const byte CommandSetFunction = 0x06;

        // Voor testen gebruiken we de vaste hash uit het protocolvoorbeeld.
        // Later kunnen we hier een echte UID-hash + receive/response-logica van maken.
        private const ushort CanHash = 0x4711;

        public Cs2TrainController(AppSettings settings, ILogSink log)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            _host = settings.Cs2Host;
            _port = settings.Cs2Port;
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
            try
            {
                _udp?.Dispose();
            }
            catch
            {
            }

            _udp = null;
            _remote = null;

            _log.Info("CS2 UDP disconnected.");
            return Task.CompletedTask;
        }

        public async Task SetSpeedAsync(int locoAddress, byte rawSpeed, CancellationToken cancellationToken)
        {
            if (_udp == null)
                await ConnectAsync(cancellationToken).ConfigureAwait(false);

            // Jouw regel:
            // DB-nummer = locadres op de baan.
            //
            // Voor mfx maken we daar een mfx Loc-ID van:
            // DB 1  -> 00 00 40 01
            // DB 18 -> 00 00 40 12
            uint locId = BuildMfxLocId(locoAddress);

            // PLC-snelheid is 1 byte: 0..255.
            // CS2/mfx snelheid is 0..1000.
            ushort cs2Speed = PlcSpeedToCs2Speed(rawSpeed);

            byte[] packet = BuildMfxSpeedPacket(locId, cs2Speed);

            await _udp.SendAsync(packet, packet.Length, _remote).ConfigureAwait(false);

            _log.Info(
                $"CS2 TX SPEED mfx -> DB={locoAddress}, locId=0x{locId:X8}, " +
                $"rawSpeed={rawSpeed}, cs2Speed={cs2Speed}, packet={BitConverter.ToString(packet)}");
        }

        public async Task SetDirectionAsync(int locoAddress, byte direction, CancellationToken cancellationToken)
        {
            if (direction > 3)
                throw new ArgumentOutOfRangeException(nameof(direction), "Richting moet 0, 1, 2 of 3 zijn.");

            if (_udp == null)
                await ConnectAsync(cancellationToken).ConfigureAwait(false);

            uint locId = BuildMfxLocId(locoAddress);
            byte[] packet = BuildMfxDirectionPacket(locId, direction);

            await _udp.SendAsync(packet, packet.Length, _remote).ConfigureAwait(false);

            _log.Info(
                $"CS2 TX DIRECTION mfx -> DB={locoAddress}, locId=0x{locId:X8}," +
                $"direction={direction}, packet={BitConverter.ToString(packet)}");
        }

        public async Task SetFunctionAsync(int locoAddress, byte functionNumber, byte value, CancellationToken cancellationToken)
        {
            if (functionNumber > 31)
                throw new ArgumentOutOfRangeException(nameof(functionNumber), "mfx functienummer moet F0...F31 zijn");

            if (value > 31)
                throw new ArgumentOutOfRangeException(nameof(value), "Functiewaarde moet 0..31 zijn. Gebruik 0=uit, 1=aan");

            if (_udp == null)
                await ConnectAsync(cancellationToken).ConfigureAwait(false);

            uint locId = BuildMfxLocId(locoAddress);
            byte[] packet = BuildMfxFunctionPacket(locId, functionNumber, value);

            await _udp.SendAsync(packet, packet.Length, _remote).ConfigureAwait(false);

            _log.Info(
                $"CS2 TX FUNCTION mfx -> DB={locoAddress}, locId=0x{locId:X8}" +
                $"F{functionNumber}={(value == 0 ? "uit" : "aan")}, packet={BitConverter.ToString(packet)}");
        }

        private static uint BuildMfxLocId(int locoAddress)
        {
            if (locoAddress < 0 || locoAddress > 0x3FFF)
                throw new ArgumentOutOfRangeException(nameof(locoAddress), "mfx adres/SID moet binnen 0..16383 vallen.");

            return 0x00004000u | (uint)locoAddress;
        }

        private static byte[] BuildMfxDirectionPacket(uint locId, byte direction)
        {
            byte[] data =
            {
                (byte)((locId >> 24) & 0xFF),
                (byte)((locId >> 16) & 0xFF),
                (byte)((locId >> 8) & 0xFF),
                (byte)(locId & 0xFF),

                direction
            };

            return BuildCanUdpPacket(
                prio: PrioLocCommand,
                command: CommandSetDirection,
                hash: CanHash,
                response: false,
                data: data,
                dlc: 5);
        }

        private static byte[] BuildMfxFunctionPacket(uint locId, byte functionNumber, byte value)
        {
            byte[] data =
            {
                (byte)((locId >> 24) & 0xFF),
                (byte)((locId >> 16) & 0xFF),
                (byte)((locId >> 8) & 0xFF),
                (byte)(locId & 0xFF),

                functionNumber,
                value
            };

            return BuildCanUdpPacket(
                prio: PrioLocCommand,
                command: CommandSetFunction,
                hash: CanHash,
                response: false,
                data: data,
                dlc: 6);
        }

        private static ushort PlcSpeedToCs2Speed(byte rawSpeed)
        {
            // rawSpeed 0   -> 0
            // rawSpeed 255 -> 1000
            var speed = (int)Math.Round(rawSpeed * (1000.0 / 255.0));

            if (speed < 0) speed = 0;
            if (speed > 1000) speed = 1000;

            return (ushort)speed;
        }

        private static byte[] BuildMfxSpeedPacket(uint locId, ushort speed)
        {
            byte[] data =
            {
                (byte)((locId >> 24) & 0xFF),
                (byte)((locId >> 16) & 0xFF),
                (byte)((locId >> 8) & 0xFF),
                (byte)(locId & 0xFF),

                (byte)((speed >> 8) & 0xFF),
                (byte)(speed & 0xFF)
            };

            return BuildCanUdpPacket(
                prio: PrioLocCommand,
                command: CommandSetSpeed,
                hash: CanHash,
                response: false,
                data: data,
                dlc: 6);
        }

        private static byte[] BuildCanUdpPacket(byte prio, byte command, ushort hash, bool response, byte[] data, byte dlc)
        {
            if (dlc > 8)
                throw new ArgumentOutOfRangeException(nameof(dlc), "DLC mag maximaal 8 zijn.");

            if (data == null)
                data = Array.Empty<byte>();

            if (data.Length < dlc)
                throw new ArgumentException("Data bevat minder bytes dan de opgegeven DLC.", nameof(data));

            byte[] packet = new byte[13];

            packet[0] = (byte)((prio << 4) | (command >> 7));
            packet[1] = (byte)(((command & 0x7F) << 1) | (response ? 1 : 0));
            packet[2] = (byte)((hash >> 8) & 0xFF);
            packet[3] = (byte)(hash & 0xFF);

            packet[4] = dlc;

            // Alleen de DLC-bytes kopiëren.
            // De overige payloadbytes blijven automatisch 0x00.
            Array.Copy(data, 0, packet, 5, dlc);

            return packet;
        }
    }
}