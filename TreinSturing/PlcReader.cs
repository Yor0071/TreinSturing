using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snap7;

namespace TreinSturing
{
    public class  PlcReader : IDisposable
    {
        private readonly S7Client _client = new S7Client();
        public bool IsConnected { get; private set; }

        public int Connect(string ip, int rack = 0, int slot = 2)
        {
            var rc = _client.ConnectTo(ip, rack, slot);
            IsConnected = (rc == 0);
            return rc;
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                _client.Disconnect();
                IsConnected = false;
            }
        }

        public byte[] ReadDbBytes(int dbNumber, int start, int length)
        {
            if (!IsConnected) throw new InvalidOperationException("PLC niet verbonden.");
            var buffer = new byte[length];
            var rc = _client.ReadArea(S7Client.S7AreaDB, dbNumber, start, length, S7Client.S7WLByte, buffer);
            if (rc != 0) throw new Exception($"Snap7 ReadArea foutcode: {rc}");
            return buffer;
        }

        public void Dispose() => Disconnect();
    }
}
