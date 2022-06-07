using Npgsql;

namespace MensattScraper.DatabaseSupport;

public class PostgresNameTranslator : INpgsqlNameTranslator
{
    public string TranslateTypeName(string clrName)
    {
        return clrName;
    }

    public string TranslateMemberName(string clrName)
    {
        return clrName;
    }
}