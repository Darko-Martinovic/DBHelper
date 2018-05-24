using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using static IOHelper.DriveHelper;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void FreeSpace
    (
        [SqlFacet(MaxSize = 1)] SqlString driveLetter,
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 unitOfMeasure,
        ref SqlDouble freeSpace,
        ref SqlString errorMessage
    )
    {
        SqlString captureMessage = null;
        freeSpace = 0;
        try
        {
            freeSpace = IOHelper.DriveHelper.FreeSpace(driveLetter.Value, (DiskSizeUnit)unitOfMeasure.Value);
        }
        catch (Exception ex)
        {
            captureMessage = new SqlString(ex.Message);
            if (ex.InnerException != null)
                captureMessage += "\r\n" + ex.InnerException.Message;
        }
        errorMessage = captureMessage;
    }
}
