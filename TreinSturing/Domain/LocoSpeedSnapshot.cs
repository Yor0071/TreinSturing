using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreinSturing.Domain
{
    public sealed class LocoSpeedSnapshot
    {
        public LocoSpeedSnapshot(int dbNumber, int locoAddress, byte rawSpeed)
        {
            DbNumber = dbNumber;
            LocoAddress = locoAddress;
            RawSpeed = rawSpeed;
        }

        public int DbNumber { get; }
        public int LocoAddress { get; }
        public byte RawSpeed { get; }
    }
}

