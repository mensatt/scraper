using System.Data;
using System.Data.Common;
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

    private readonly NpgsqlCommand _selectFullOccurrenceByLocationDateCommand =
        new(DatabaseConstants.SelectFullOccurrenceByLocationDateSql)
        {
            Parameters =
            {
                new("location", NpgsqlDbType.Uuid),
                new("date", NpgsqlDbType.Date)
            }
        };

    private readonly NpgsqlCommand _selectOccurrenceTagsByIdCommand =
        new(DatabaseConstants.SelectOccurrenceTagsByIdSql)
        {
            Parameters =
            {
                new("id", NpgsqlDbType.Uuid)
            }
        };

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

    private readonly NpgsqlCommand _updateOccurrenceNotAvailableAfterByIdCommand =
        new(DatabaseConstants.UpdateOccurrenceNotAvailableAfterByIdSql)
        {
            Parameters =
            {
                new("not_available_after", NpgsqlDbType.TimestampTz),
                new("id", NpgsqlDbType.Uuid)
            }
        };

    private readonly NpgsqlCommand _updateOccurrenceContentsByIdCommand =
        new(DatabaseConstants.UpdateOccurrenceContentsByIdSql)
        {
            Parameters =
            {
                new("id", NpgsqlDbType.Uuid),
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

    private readonly NpgsqlCommand _deleteOccurrenceTagByIdTagCommand =
        new(DatabaseConstants.DeleteOccurrenceTagByIdTagSql)
        {
            Parameters =
            {
                new("id", NpgsqlDbType.Uuid),
                new("tag", NpgsqlDbType.Varchar)
            }
        };

    private readonly NpgsqlBatch _commandBatch = new();

    private readonly NpgsqlCommand _insertOccurrenceTagCommand =
        new(DatabaseConstants.InsertOccurrenceTagSql)
        {
            Parameters =
            {
                new("occurrence", NpgsqlDbType.Uuid),
                new("tag", NpgsqlDbType.Varchar)
            }
        };

    private readonly NpgsqlCommand _insertOccurrenceSideDishCommand =
        new(DatabaseConstants.InsertOccurrenceSideDishSql)
        {
            Parameters =
            {
                new("occurrence", NpgsqlDbType.Uuid),
                new("dish", NpgsqlDbType.Uuid)
            }
        };

    #endregion

    public NpgsqlDatabaseWrapper(string connectionString)
    {
        SharedLogger.LogInformation("Creating DatabaseWrapper with connection string: {ConnectionString}",
            connectionString);
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

    public void ExecuteInsertOccurrenceTagCommand(Guid occurrence, string tag)
    {
        try
        {
            _insertOccurrenceTagCommand.Parameters["occurrence"].Value = occurrence;
            _insertOccurrenceTagCommand.Parameters["tag"].Value = tag;
            _insertOccurrenceTagCommand.ExecuteNonQuery();
        }
        catch (PostgresException pe)
        {
            // TODO: Diagnose
            SharedLogger.LogError(pe, "Error while inserting tag {Tag} for occurrence {Occurrence}", tag, occurrence);
        }
    }

    public void ExecuteInsertOccurrenceSideDishCommand(Guid occurrence, Guid sideDish)
    {
        _insertOccurrenceSideDishCommand.Parameters["occurrence"].Value = occurrence;
        _insertOccurrenceSideDishCommand.Parameters["dish"].Value = sideDish;
        _insertOccurrenceSideDishCommand.ExecuteNonQuery();
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

    public Dictionary<DateOnly, List<Occurrence>> ExecuteSelectOccurrenceIdNameDateByLocationCommand(
        Guid locationId)
    {
        var dateMapping = new Dictionary<DateOnly, List<Occurrence>>();

        _selectOccurrenceIdNameDateByLocationCommand.Parameters["location"].Value = locationId;
        using var reader = _selectOccurrenceIdNameDateByLocationCommand.ExecuteReader();
        while (reader.Read())
        {
            var dishId = reader.GetGuid("dish");
            var occurrenceId = reader.GetGuid("id");
            var occurrenceDate = DateOnly.FromDateTime(reader.GetDateTime("date"));
            DateTime? notAvailableAfter;
            if (reader.IsDBNull("not_available_after"))
                notAvailableAfter = null;
            else
                notAvailableAfter = reader.GetDateTime("not_available_after");

            if (!dateMapping.ContainsKey(occurrenceDate))
                dateMapping.Add(occurrenceDate, new());
            dateMapping[occurrenceDate].Add(new(occurrenceId, dishId, notAvailableAfter));
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

    public Dictionary<DateOnly, List<Occurrence>> RetrieveFullOccurrencesWithTags(Guid locationId,
        DateOnly newerThanDate)
    {
        _selectFullOccurrenceByLocationDateCommand.Parameters["location"].Value = locationId;
        _selectFullOccurrenceByLocationDateCommand.Parameters["date"].Value = newerThanDate;
        var dateMapping = new Dictionary<DateOnly, List<Occurrence>>();
        {
            using var reader = _selectFullOccurrenceByLocationDateCommand.ExecuteReader();

            while (reader.Read())
            {
                var occurrenceDate = DateOnly.FromDateTime(reader.GetDateTime("date"));

                var occurrence = new Occurrence(reader.GetGuid("id"), reader.GetGuid("dish"),
                    reader.GetDateTimeOrNull("not_available_after"), reader.GetIntOrNull("price_student"),
                    reader.GetIntOrNull("price_staff"),
                    reader.GetIntOrNull("price_guest"), null, reader.GetIntOrNull("kj"), reader.GetIntOrNull("kcal"),
                    reader.GetIntOrNull("fat"), reader.GetIntOrNull("saturated_fat"),
                    reader.GetIntOrNull("carbohydrates"),
                    reader.GetIntOrNull("sugar"), reader.GetIntOrNull("fiber"), reader.GetIntOrNull("protein"),
                    reader.GetIntOrNull("salt"));

                if (!dateMapping.ContainsKey(occurrenceDate))
                    dateMapping.Add(occurrenceDate, new());
                dateMapping[occurrenceDate].Add(occurrence);
            }
        }

        foreach (List<Occurrence> occurrenceList in dateMapping.Values)
        {
            foreach (Occurrence occurrence in occurrenceList)
            {
                _selectOccurrenceTagsByIdCommand.Parameters["id"].Value = occurrence.Id;
                using var tagReader = _selectOccurrenceTagsByIdCommand.ExecuteReader();
                occurrence.Tags = new();
                while (tagReader.Read())
                {
                    occurrence.Tags.Add(tagReader.GetString("tag"));
                }
            }
        }

        return dateMapping;
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
    public Guid? ExecuteInsertOccurrenceCommand(Guid locationId, DayTag dayTag, Item item, Guid dish)
    {
        _insertOccurrenceCommand.Parameters["id"].Value = Guid.NewGuid();
        _insertOccurrenceCommand.Parameters["location"].Value = locationId;
        _insertOccurrenceCommand.Parameters["dish"].Value = dish;
        _insertOccurrenceCommand.Parameters["date"].Value = Converter.GetDateFromTimestamp(dayTag.Timestamp);

        SetItemContentParameters(_insertOccurrenceCommand, item);

        return (Guid?) _insertOccurrenceCommand.ExecuteScalar();
    }

    private static void SetItemContentParameters(NpgsqlCommand command, Item item)
    {
        SetParameterToValueOrNull(command.Parameters["kj"], Converter.BigFloatStringToInt(item.Kj));
        SetParameterToValueOrNull(command.Parameters["kcal"],
            Converter.BigFloatStringToInt(item.Kcal));
        SetParameterToValueOrNull(command.Parameters["fat"], Converter.FloatStringToInt(item.Fett));
        SetParameterToValueOrNull(command.Parameters["saturated_fat"],
            Converter.FloatStringToInt(item.Gesfett));
        SetParameterToValueOrNull(command.Parameters["carbohydrates"],
            Converter.FloatStringToInt(item.Kh));
        SetParameterToValueOrNull(command.Parameters["sugar"],
            Converter.FloatStringToInt(item.Zucker));
        SetParameterToValueOrNull(command.Parameters["fiber"],
            Converter.FloatStringToInt(item.Ballaststoffe));
        SetParameterToValueOrNull(command.Parameters["protein"],
            Converter.FloatStringToInt(item.Eiweiss));
        SetParameterToValueOrNull(command.Parameters["salt"], Converter.FloatStringToInt(item.Salz));
        SetParameterToValueOrNull(command.Parameters["price_student"],
            Converter.FloatStringToInt(item.Preis1));
        SetParameterToValueOrNull(command.Parameters["price_staff"],
            Converter.FloatStringToInt(item.Preis2));
        SetParameterToValueOrNull(command.Parameters["price_guest"],
            Converter.FloatStringToInt(item.Preis3));
    }

    public Guid? ExecuteInsertDishAliasCommand(string? dishName, Guid dish)
    {
        var extractedDishName = Converter.ExtractElementFromTitle(dishName, Converter.TitleElement.Name);
        SharedLogger.LogInformation("Inserting new dish alias, extractedDishName={ExtractedDishName}",
            extractedDishName);
        _insertDishAliasCommand.Parameters["alias_name"].Value = extractedDishName;
        _insertDishAliasCommand.Parameters["normalized_alias_name"].Value = Converter.SanitizeString(extractedDishName);
        _insertDishAliasCommand.Parameters["dish"].Value = dish;
        return (Guid?) _insertDishAliasCommand.ExecuteScalar();
    }

    public void ExecuteUpdateOccurrenceNotAvailableAfterByIdCommand(Guid id, DateTime notAvailableAfter)
    {
        _updateOccurrenceNotAvailableAfterByIdCommand.Parameters["id"].Value = id;
        _updateOccurrenceNotAvailableAfterByIdCommand.Parameters["not_available_after"].Value = notAvailableAfter;
        _updateOccurrenceNotAvailableAfterByIdCommand.ExecuteNonQuery();
    }

    public void ExecuteUpdateOccurrenceContentsByIdCommand(Guid occurrenceId, Item i)
    {
        SetItemContentParameters(_updateOccurrenceContentsByIdCommand, i);

        _updateOccurrenceContentsByIdCommand.Parameters["id"].Value = occurrenceId;

        var rows = _updateOccurrenceContentsByIdCommand.ExecuteNonQuery();
        SharedLogger.LogTrace("Content update affected {RowCount} rows", rows);
    }

    public void ExecuteDeleteOccurrenceTagByIdTagCommand(Guid id, string tag)
    {
        _deleteOccurrenceTagByIdTagCommand.Parameters["id"].Value = id;
        _deleteOccurrenceTagByIdTagCommand.Parameters["tag"].Value = tag;

        _deleteOccurrenceTagByIdTagCommand.ExecuteNonQuery();
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
        SharedLogger.LogInformation("Disposing DatabaseWrapper {S}", ToString());
        foreach (var npgsqlCommand in ReflectionUtil.GetFieldValuesWithType<NpgsqlCommand>(
                     typeof(NpgsqlDatabaseWrapper), this))
            npgsqlCommand.Dispose();

        _commandBatch.Dispose();
        _databaseConnection.Dispose();
    }
}
