using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreinSturing.Configuration;
using TreinSturing.Infrastructure;

namespace TreinSturing.Application
{
    public sealed class PlcDiscoveryService
    {
        private readonly IPlcReader _plcReader;
        private readonly AppSettings _settings;

        public PlcDiscoveryService(IPlcReader plcReader, AppSettings settings)
        {
            _plcReader = plcReader;
            _settings = settings;
        }

        public IReadOnlyList<int> DiscoverLocomotiveDbs()
        {
            var result = new List<int>();

            for (var dbNumber = _settings.DbScanStart; dbNumber <= _settings.DbScanEnd; dbNumber++)
            {
                try
                {
                    _plcReader.ReadDbBytes(dbNumber, _settings.PlcStart, 2);
                    result.Add(dbNumber);
                }
                catch
                {
                    // DB bestaat niet of is niet leesbaar.
                }
            }

            return result;
        }
    }
}

