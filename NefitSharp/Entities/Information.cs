namespace NefitSharp.Entities
{
    public struct Information
    {
        public string OwnerName { get; }
        public string OwnerCellNumber { get; }
        public string OwnerEmail { get; }
        public string InstallerName { get; }
        public string InstallerCellNumber { get; }
        public string InstallerEmail { get; }

        public string EasySerialNumber { get; }
        public string EasyUpdateStrategy { get; }
        public string EasyFirmwareVersion { get; }
        public string EasyHardwareVersion { get; }
        public string EasyUuid { get; }

        public string CVSerial { get; }
        public string CVVersion { get; }
        public string CVBurnerType { get; }

        public Information(string ownerName, string ownerCellNumber, string ownerEmail, string installerName, string installerCellNumber, string installerEmail, string easySerialNumber, string easyUpdateStrategy, string easyFirmwareVersion, string easyHardwareVersion, string easyUuid, string cvSerial, string cvVersion, string cvBurnerType)
        {
            OwnerName = ownerName;
            OwnerCellNumber = ownerCellNumber;
            OwnerEmail = ownerEmail;
            InstallerName = installerName;
            InstallerCellNumber = installerCellNumber;
            InstallerEmail = installerEmail;
            EasySerialNumber = easySerialNumber;
            EasyUpdateStrategy = easyUpdateStrategy;
            EasyFirmwareVersion = easyFirmwareVersion;
            EasyHardwareVersion = easyHardwareVersion;
            EasyUuid = easyUuid;
            CVSerial = cvSerial;
            CVVersion = cvVersion;
            CVBurnerType = cvBurnerType;
        }
    }
}