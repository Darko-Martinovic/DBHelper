using System;
using System.IO;

namespace IOHelper.Common
{
    public static class DriveHelper
    {
        public enum DiskSizeUnit
        {
            Bytes = 0,
            KiloBytes = 1,
            MegaBytes = 2,
            GigaBytes = 3,
            TeraBytes = 4
        }

        public static double FreeSpace(string driveLetter, DiskSizeUnit sizeUnit = DiskSizeUnit.MegaBytes)
        {
            double whatIsFreeSpace = -1;
            var driveInfo = new DriveInfo(driveLetter);

            {
                var freeSpaceNative = driveInfo.AvailableFreeSpace;
                var dividedBy = Math.Pow(1024, (int)sizeUnit);

                whatIsFreeSpace = freeSpaceNative / dividedBy;
            }

            return whatIsFreeSpace;
        }
    }


}
