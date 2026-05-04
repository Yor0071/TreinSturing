using System;
using System.Configuration;

namespace TreinSturing.Configuration
{
    public sealed class AppSettings
    {
        public string PlcIp { get; private set; }
        public int PlcRack { get; private set; }
        public int PlcSlot { get; private set; }
        public int PlcStart { get; private set; }
        public int PlcLength { get; private set; }
        public int PollIntervalMs { get; private set; }
        public int DbScanStart { get; private set; }
        public int DbScanEnd { get; private set; }

        public string ControllerType { get; private set; }
        public string Cs2Host { get; private set; }
        public int Cs2Port { get; private set; }
        public int Cs2ConnectTimeoutMs { get; private set; }
        public bool SimulationEnabled { get; private set; }

        public static AppSettings Load()
        {
            return new AppSettings
            {
                PlcIp = Get("Plc.Ip", "192.168.0.1"),
                PlcRack = GetInt("Plc.Rack", 0),
                PlcSlot = GetInt("Plc.Slot", 2),
                PlcStart = GetInt("Plc.Start", 2),
                PlcLength = GetInt("Plc.Length", 16),
                PollIntervalMs = GetInt("Plc.PollIntervalMs", 200),
                DbScanStart = GetInt("Plc.DbScanStart", 1),
                DbScanEnd = GetInt("Plc.DbScanEnd", 81),
                ControllerType = Get("Controller.Type", "Simulation"),
                Cs2Host = Get("Controller.Cs2.Host", "192.168.0.50"),
                Cs2Port = GetInt("Controller.Cs2.Port", 15731),
                Cs2ConnectTimeoutMs = GetInt("Controller.Cs2.ConnectTimeoutMs", 3000),
                SimulationEnabled = GetBool("Controller.SimulationEnabled", true)
            };
        }

        private static string Get(string key, string fallback)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int GetInt(string key, int fallback)
        {
            int value;
            return int.TryParse(ConfigurationManager.AppSettings[key], out value) ? value : fallback;
        }

        private static bool GetBool(string key, bool fallback)
        {
            bool value;
            return bool.TryParse(ConfigurationManager.AppSettings[key], out value) ? value : fallback;
        }
    }
}
