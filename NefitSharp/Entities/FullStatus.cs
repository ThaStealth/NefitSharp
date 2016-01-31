using NefitSharp.Entities.Internal;

namespace NefitSharp.Entities
{
    public class FullStatus : UIStatus
    {
        public string LastThermalDesinfectResult { get; }
        public ServiceStatus ServiceStatus { get; }
        public double OutdoorTemperature { get; }
        public string OutdoorTemperatureSource { get; }        
        public OperationModes OperationMode { get; }
        public bool RefillNeeded { get; }
        public bool Ignition { get; }
        public bool ClosingValve { get; }
        public bool ShortTapping { get; }
        public bool SystemLeaking { get; }
        public double CentralHeatingSystemPressure { get; }
        public double CentralHeatingSystemSupplyTemperature { get; }
        public StatusCode? CurrentStatus { get; }
        internal FullStatus(NefitStatus stats,string serviceStatus,double outdoorTemp,string operationMode,bool refillNeeded,bool ignition,bool closingValve,bool shortTapping,bool systemLeaking,double centralHeatingSystemPressure, double centralHeatingSystemSupplyTemperature, StatusCode? code)
            :base(stats)
        {
            CurrentStatus = code;
            switch (serviceStatus.ToLower())
            {
                case "no service":                    
                    ServiceStatus = ServiceStatus.NoService;
                    break;

                default:
                    ServiceStatus = ServiceStatus.Unknown;
                    break;
            }
            OutdoorTemperature =outdoorTemp;
            OutdoorTemperatureSource = "unknown";
            switch (operationMode.ToLower())
            {
                case "selflearning":
                    OperationMode = OperationModes.SelfLearning;
                    break;

                default:
                    OperationMode = OperationModes.Unknown;
                    break;
            }
            RefillNeeded = refillNeeded;
            Ignition = ignition;
            ClosingValve = closingValve;
            ShortTapping = shortTapping;
            SystemLeaking = systemLeaking;
            CentralHeatingSystemPressure = centralHeatingSystemPressure;
            CentralHeatingSystemSupplyTemperature = centralHeatingSystemSupplyTemperature;
            LastThermalDesinfectResult = "";
        }        
    }
}