using System;
using System.Data.SqlTypes;
using System.IO;
using Microsoft.SqlServer.Server;

namespace IOHelper.Stored_procedure
{
    public partial class StoredProcedures
    {
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void DeleteFiles 
        (
            [SqlFacet(MaxSize = 1000)] SqlString path,
            [SqlFacet(MaxSize = 1000,IsNullable =true)] SqlString filter, 
            ref SqlString errorMessage
        )
        {
            var di = new DirectoryInfo(path.Value);
            FileSystemInfo[] fsi = null;
            SqlString captureMessage = null;
            if (filter.IsNull || filter.Value == string.Empty)
                fsi = di.GetFileSystemInfos();
            else
                fsi = di.GetFileSystemInfos(filter.Value);
            foreach (var f in fsi)
            {
                try
                {
                    File.Delete(f.FullName);
                }
                catch (Exception ex)
                {
                    captureMessage = new SqlString(ex.Message);
                    if (ex.InnerException != null)
                        captureMessage += $"\r\n{ex.InnerException.Message}";
                    break;

                }
            }
            di = null;
            fsi = null;
            errorMessage = captureMessage;

        }
    }
}
