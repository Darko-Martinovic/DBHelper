using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using Microsoft.SqlServer.Server;

public partial class UserDefinedFunctions
{
    [SqlFunction(
     Name = "FileHelper",
     FillRowMethodName = "FileFillRow",
     TableDefinition = @"Name nvarchar(4000), SizeInBytes bigint")]
    public static IEnumerable FileHelper(SqlString path, SqlString filter)
    {
        DirectoryInfo di = new DirectoryInfo(path.Value);
        if (filter.IsNull || filter.Value == string.Empty)
            return di.GetFileSystemInfos();
        else
            return di.GetFileSystemInfos(filter.Value);
    }

    private static void FileFillRow(object obj, out SqlString altname, out SqlInt64 size)
    {
        FileInfo fsi = (FileInfo)obj;
        altname = fsi.Name;
        size = fsi.Length;
    }

}
