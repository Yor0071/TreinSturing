using System;
using TreinSturing.Configuration;
using TreinSturing.Domain;

namespace TreinSturing.Infrastructure
{
    public static class TrainControllerFactory
    {
        public static ITrainController Create(AppSettings settings, ILogSink log)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var type = (settings.ControllerType ?? string.Empty).Trim().ToUpperInvariant();
            switch (type)
            {
                case "CS2":
                case "CENTRALSTATION":
                case "CENTRALSTATION2":
                    return new Cs2TrainController(settings, log);

                case "SIMULATION":
                default:
                    return new SimulationTrainController(log);
            }
        }
    }
}
