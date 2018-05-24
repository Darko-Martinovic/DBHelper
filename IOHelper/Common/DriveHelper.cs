using System;
using System.IO;

namespace IOHelper
{
    static public class DriveHelper
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
            double dividedBy= 1;
            DriveInfo driveInfo = new DriveInfo(driveLetter);

            if (driveInfo != null)
            {
                long freeSpaceNative = driveInfo.AvailableFreeSpace;
                dividedBy = Math.Pow(1024, (int)sizeUnit);

                whatIsFreeSpace = freeSpaceNative / dividedBy;
            }

            return whatIsFreeSpace;
        }
    }


}
