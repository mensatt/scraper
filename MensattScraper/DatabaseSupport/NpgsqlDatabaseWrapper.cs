using System.Data;
using MensattScraper.DestinationCompat;
using MensattScraper.SourceCompat;
using MensattScraper.Util;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace MensattScraper.DatabaseSupport;

public class NpgsqlDatabaseWrapper : IDatabaseWrapper
{
    private readonly NpgsqlConnection _databaseConnection;

    #region Command Properties

    private readonly NpgsqlCommand _selectDishByGermanNameCommand = new(DatabaseConstants.SelectIdByGermanNameSql)
    {
        Parameters =
        {
            new("name_de", NpgsqlDbType.Varchar)
        }
    };

    private readonly NpgsqlCommand _selectDishByAliasNameCommand =
        new(DatabaseConstants.SelectDishIdByNormalizedAliasNameSql)
        {
            Parameters =
            {
                new("normalized_alias_name", NpgsqlDbType.Varchar)
            }
        };

    private readonly NpgsqlCommand _selectOccurrenceIdNameDateByLocationCommand =
        new(DatabaseConstants.SelectOccurrenceIdNameDateByLocationSql)
        {
            Parameters =
            {
                new("location", NpgsqlDbType.Uuid)
            }
        };

    private readonly NpgsqlCommand _selectLocationIdNameLocationIdCommand =
        new(DatabaseConstants.SelectLocationIdNameLocationIdSql);

    private readonly NpgsqlCommand _selectTagAllCommand = new(DatabaseConstants.SelectTagAllSql);

    private readonly NpgsqlCommand _selectDishAliasNormalizedDishCommand =
        new(DatabaseConstants.SelectDishAliasNormalizedDishSql);

    private readonly NpgsqlCommand _insertDishCommand = new(DatabaseConstants.InsertDishWithGermanNameSql)
    {
        Parameters =
        {
            new("id", NpgsqlDbType.Uuid),
            new("name_de", NpgsqlDbType.Varchar),
            new("name_en", NpgsqlDbType.Varchar)
        }
    };

    private readonly NpgsqlCommand _insertOccurrenceCommand = new(DatabaseConstants.InsertOccurrenceSql)
    {
        Parameters =
        {
            new("id", NpgsqlDbType.Uuid),
            new("location", NpgsqlDbType.Uuid),
            new("dish", NpgsqlDbType.Uuid),
            new("date", NpgsqlDbType.Date),
            new("status", NpgsqlDbType.Varchar),
            new("kj", NpgsqlDbType.Integer),
            new("kcal", NpgsqlDbType.Integer),
            new("fat", NpgsqlDbType.Integer),
            new("saturated_fat", NpgsqlDbType.Integer),
            new("carbohydrates", NpgsqlDbType.Integer),
            new("sugar", NpgsqlDbType.Integer),
            new("fiber", NpgsqlDbType.Integer),
            new("protein", NpgsqlDbType.Integer),
            new("salt", NpgsqlDbType.Integer),
            new("price_student", NpgsqlDbType.Integer),
            new("price_staff", NpgsqlDbType.Integer),
            new("price_guest", NpgsqlDbType.Integer)
        }
    };

    private readonly NpgsqlCommand _insertDishAliasCommand = new(DatabaseConstants.InsertDishAliasSql)
    {
        Parameters =
        {
            new("alias_name", NpgsqlDbType.Varchar),
            new("normalized_alias_name", NpgsqlDbType.Varchar),
            new("dish", NpgsqlDbType.Uuid)
        }
    };

    private readonly NpgsqlCommand _updateOccurrenceReviewStatusByIdCommand =
        new(DatabaseConstants.UpdateOccurrenceReviewStatusByIdSql)
        {
            Parameters =
            {
                new("status", NpgsqlDbType.Varchar),
                new("id", NpgsqlDbType.Uuid)
            }
        };

    private readonly NpgsqlCommand _deleteOccurrenceCommand = new(DatabaseConstants.DeleteOccurrenceByIdSql)
    {
        Parameters =
        {
            new("id", NpgsqlDbType.Uuid)
        }
    };

    private readonly NpgsqlBatch _commandBatch = new();

    private static NpgsqlBatchCommand InsertOccurrenceTagBatchCommand =>
        new(DatabaseConstants.InsertOccurrenceTagSql)
        {
            Parameters =
            {
                new("occurrence", NpgsqlDbType.Uuid),
                new("tag", NpgsqlDbType.Varchar)
            }
        };

    private static NpgsqlBatchCommand InsertOccurrenceSideDishCommand =>
        new(DatabaseConstants.InsertOccurrenceSideDishSql)
        {
            Parameters =
            {
                new("occurrence", NpgsqlDbType.Uuid),
                new("dish", NpgsqlDbType.Uuid)
            }
        };

    #endregion

    #region Confidence matching support

    private readonly NpgsqlCommand _updateOccurrenceDishByIdCommand = new(DatabaseConstants.UpdateOccurrenceDishByIdSql)
    {
        Parameters =
        {
            new("dish", NpgsqlDbType.Uuid),
            new("id", NpgsqlDbType.Uuid)
        }
    };

    private readonly NpgsqlCommand _updateDishAliasDishByAliasNameCommand =
        new(DatabaseConstants.UpdateDishAliasDishByAliasNameSql)
        {
            Parameters =
            {
                new("dish", NpgsqlDbType.Uuid),
                new("alias_name", NpgsqlDbType.Varchar)
            }
        };

    private readonly NpgsqlCommand _deleteDishByIdCommand = new(DatabaseConstants.DeleteDishByIdSql)
    {
        Parameters =
        {
            new("id", NpgsqlDbType.Uuid)
        }
    };

    #endregion

    public NpgsqlDatabaseWrapper(string connectionString)
    {
        SharedLogger.LogInformation($"Creating DatabaseWrapper with connection string: {connectionString}");
        _databaseConnection = new(connectionString);

        foreach (var npgsqlCommand in ReflectionUtil.GetFieldValuesWithType<NpgsqlCommand>(
                     typeof(NpgsqlDatabaseWrapper), this))
            npgsqlCommand.Connection = _databaseConnection;

        _commandBatch.Connection = _databaseConnection;
    }

    public void ConnectAndPrepare()
    {
        _databaseConnection.Open();

        foreach (var npgsqlCommand in ReflectionUtil.GetFieldValuesWithType<NpgsqlCommand>(
                     typeof(NpgsqlDatabaseWrapper), this))
            npgsqlCommand.Prepare();
    }

    public void ResetBatch() => _commandBatch.BatchCommands.Clear();

    public void ExecuteBatch() => _commandBatch.ExecuteNonQuery();

    public void AddInsertOccurrenceTagCommandToBatch(Guid occurrence, string tag)
    {
        var command = InsertOccurrenceTagBatchCommand;
        command.Parameters["occurrence"].Value = occurrence;
        command.Parameters["tag"].Value = tag;
        _commandBatch.BatchCommands.Add(command);
    }

    public void AddInsertOccurrenceSideDishCommandToBatch(Guid occurrence, Guid sideDish)
    {
        var command = InsertOccurrenceSideDishCommand;
        command.Parameters["occurrence"].Value = occurrence;
        command.Parameters["dish"].Value = sideDish;
        _commandBatch.BatchCommands.Add(command);
    }

    public Guid? ExecuteSelectDishByGermanNameCommand(string? name)
    {
        _selectDishByGermanNameCommand.Parameters["name_de"].Value =
            Converter.ExtractElementFromTitle(name, Converter.TitleElement.Name);
        return (Guid?) _selectDishByGermanNameCommand.ExecuteScalar();
    }

    public Guid? ExecuteSelectDishNormalizedAliasByNameCommand(string? name)
    {
        _selectDishByAliasNameCommand.Parameters["normalized_alias_name"].Value =
            Converter.SanitizeString(Converter.ExtractElementFromTitle(name, Converter.TitleElement.Name));
        return (Guid?) _selectDishByAliasNameCommand.ExecuteScalar();
    }

    public Dictionary<DateOnly, List<Tuple<Guid, Guid>>> ExecuteSelectOccurrenceIdNameDateByLocationCommand(Guid locationId)
    {
        var dateMapping = new Dictionary<DateOnly, List<Tuple<Guid, Guid>>>();

        _selectOccurrenceIdNameDateByLocationCommand.Parameters["location"].Value = locationId;
        using var reader = _selectOccurrenceIdNameDateByLocationCommand.ExecuteReader();
        while (reader.Read())
        {
            var occurrenceDate = DateOnly.FromDateTime(reader.GetDateTime("date"));
            var occurrenceDishTuple = new Tuple<Guid, Guid>(reader.GetGuid("dish"), reader.GetGuid("id"));
            if (!dateMapping.ContainsKey(occurrenceDate))
                dateMapping.Add(occurrenceDate, new());
            dateMapping[occurrenceDate].Add(occurrenceDishTuple);
        }

        return dateMapping;
    }

    public List<Location> ExecuteSelectIdNameLocationIdCommand()
    {
        var locationList = new List<Location>();
        using var reader = _selectLocationIdNameLocationIdCommand.ExecuteReader();
        while (reader.Read())
            locationList.Add(new(reader.GetGuid("id"), reader.GetString("name"),
                (uint) reader.GetInt32("external_id")));
        return locationList;
    }

    public List<string> ExecuteSelectTagAllCommand()
    {
        var tagList = new List<string>();
        using var reader = _selectTagAllCommand.ExecuteReader();
        while (reader.Read())
            tagList.Add(reader.GetString("key"));
        return tagList;
    }

    public Dictionary<string, Guid> ExecuteSelectDishAliasesNormalizedDishCommand()
    {
        var aliases = new Dictionary<string, Guid>();
        using var reader = _selectDishAliasNormalizedDishCommand.ExecuteReader();
        while (reader.Read())
            aliases.Add(reader.GetString("normalized_alias_name"), reader.GetGuid("dish"));
        return aliases;
    }

    public Guid? ExecuteInsertDishCommand(string? primaryTitle, string? secondaryTitle)
    {
        _insertDishCommand.Parameters["id"].Value = Guid.NewGuid();
        _insertDishCommand.Parameters["name_de"].Value =
            Converter.ExtractElementFromTitle(primaryTitle, Converter.TitleElement.Name);
        SetParameterToValueOrNull(_insertDishCommand.Parameters["name_en"],
            Converter.ExtractElementFromTitle(secondaryTitle, Converter.TitleElement.Name));
        return (Guid?) _insertDishCommand.ExecuteScalar();
    }

    // TODO: Use timestamp directly, instead of passing DayTag
    public Guid? ExecuteInsertOccurrenceCommand(Guid locationId, DayTag dayTag, Item item, Guid dish,
        OccurrenceStatus status)
    {
        _insertOccurrenceCommand.Parameters["id"].Value = Guid.NewGuid();
        _insertOccurrenceCommand.Parameters["location"].Value = locationId;
        _insertOccurrenceCommand.Parameters["dish"].Value = dish;
        _insertOccurrenceCommand.Parameters["date"].Value = Converter.GetDateFromTimestamp(dayTag.Timestamp);
        _insertOccurrenceCommand.Parameters["status"].Value = Enum.GetName(status);
        var kj = Converter.FloatStringToInt(item.Kj);
        _insertOccurrenceCommand.Parameters["kj"].Value = kj == null ? DBNull.Value : (int) kj / 10;
        var kcal = Converter.FloatStringToInt(item.Kcal);
        _insertOccurrenceCommand.Parameters["kcal"].Value = kcal == null ? DBNull.Value : (int) kcal / 10;
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["fat"], Converter.FloatStringToInt(item.Fett));
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["saturated_fat"],
            Converter.FloatStringToInt(item.Gesfett));
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["carbohydrates"],
            Converter.FloatStringToInt(item.Kh));
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["sugar"],
            Converter.FloatStringToInt(item.Zucker));
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["fiber"],
            Converter.FloatStringToInt(item.Ballaststoffe));
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["protein"],
            Converter.FloatStringToInt(item.Eiweiss));
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["salt"], Converter.FloatStringToInt(item.Salz));
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["price_student"],
            Converter.FloatStringToInt(item.Preis1));
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["price_staff"],
            Converter.FloatStringToInt(item.Preis2));
        SetParameterToValueOrNull(_insertOccurrenceCommand.Parameters["price_guest"],
            Converter.FloatStringToInt(item.Preis3));

        return (Guid?) _insertOccurrenceCommand.ExecuteScalar();
    }

    public Guid? ExecuteInsertDishAliasCommand(string? dishName, Guid dish)
    {
        var extractedDishName = Converter.ExtractElementFromTitle(dishName, Converter.TitleElement.Name);
        SharedLogger.LogInformation($"Inserting new dish alias, extractedDishName={extractedDishName}");
        _insertDishAliasCommand.Parameters["alias_name"].Value = extractedDishName;
        _insertDishAliasCommand.Parameters["normalized_alias_name"].Value = Converter.SanitizeString(extractedDishName);
        _insertDishAliasCommand.Parameters["dish"].Value = dish;
        return (Guid?) _insertDishAliasCommand.ExecuteScalar();
    }

    public void ExecuteUpdateOccurrenceReviewStatusByIdCommand(OccurrenceStatus status, Guid id)
    {
        _updateOccurrenceReviewStatusByIdCommand.Parameters["status"].Value = Enum.GetName(status);
        _updateOccurrenceReviewStatusByIdCommand.Parameters["id"].Value = id;
        _updateOccurrenceReviewStatusByIdCommand.ExecuteNonQuery();
    }

    public void ExecuteUpdateOccurrenceDishByIdCommand(Guid dish, Guid id)
    {
        _updateOccurrenceDishByIdCommand.Parameters["dish"].Value = dish;
        _updateOccurrenceDishByIdCommand.Parameters["id"].Value = id;
        _updateOccurrenceDishByIdCommand.ExecuteNonQuery();
    }

    public void ExecuteUpdateDishAliasDishByAliasNameCommand(Guid dish, string aliasName)
    {
        _updateDishAliasDishByAliasNameCommand.Parameters["dish"].Value = dish;
        _updateDishAliasDishByAliasNameCommand.Parameters["alias_name"].Value = aliasName;
        _updateDishAliasDishByAliasNameCommand.ExecuteNonQuery();
    }

    public void ExecuteDeleteOccurrenceByIdCommand(Guid id)
    {
        _deleteOccurrenceCommand.Parameters["id"].Value = id;
        _deleteOccurrenceCommand.ExecuteNonQuery();
    }

    public void ExecuteDeleteDishByIdCommand(Guid id)
    {
        _deleteDishByIdCommand.Parameters["id"].Value = id;
        _deleteDishByIdCommand.ExecuteNonQuery();
    }

    private static void SetParameterToValueOrNull(IDataParameter param, string? value)
    {
        param.Value = value is null ? DBNull.Value : value.Trim().Length == 0 ? DBNull.Value : value;
    }

    private static void SetParameterToValueOrNull(IDataParameter param, int? value)
    {
        param.Value = value.HasValue ? value : DBNull.Value;
    }

    public void Dispose()
    {
        SharedLogger.LogInformation($"Disposing DatabaseWrapper {ToString()}");
        foreach (var npgsqlCommand in ReflectionUtil.GetFieldValuesWithType<NpgsqlCommand>(
                     typeof(NpgsqlDatabaseWrapper), this))
            npgsqlCommand.Dispose();

        _commandBatch.Dispose();
        _databaseConnection.Dispose();
    }
}
