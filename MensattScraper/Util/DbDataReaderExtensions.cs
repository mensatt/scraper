using System.Data;
using System.Data.Common;

namespace MensattScraper.Util;

public static class DbReaderExtension
{
    public static string? GetStringOrNull(this DbDataReader reader, string column)
    {
        return reader.IsDBNull(column) ? null : reader.GetString(column);
    }

    public static int? GetIntOrNull(this DbDataReader reader, string column)
    {
        return reader.IsDBNull(column) ? null : reader.GetInt32(column);
    }

    public static DateTime? GetDateTimeOrNull(this DbDataReader reader, string column)
    {
        return reader.IsDBNull(column) ? null : reader.GetDateTime(column);
    }
}
