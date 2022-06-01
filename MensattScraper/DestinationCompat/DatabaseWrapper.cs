using System.ComponentModel;
using System.Data;
using System.Reflection;
using MensattScraper.SourceCompat;
using Npgsql;
using NpgsqlTypes;

namespace MensattScraper.DestinationCompat;

public class DatabaseWrapper : IDisposable
{
    private readonly NpgsqlConnection _databaseConnection;

    private readonly NpgsqlCommand _selectDishByNameCommand = new(DatabaseConstants.SelectIdByNameSql)
    {
        Parameters =
        {
            new("name", NpgsqlDbType.Varchar)
        }
    };

    private readonly NpgsqlCommand _selectDishByAliasNameCommand = new(DatabaseConstants.SelectDishIdByAliasNameSql)
    {
        Parameters =
        {
            new("alias_name", NpgsqlDbType.Varchar)
        }
    };

    private readonly NpgsqlCommand _selectOccurrenceIdNameDateCommand =
        new(DatabaseConstants.SelectOccurrenceIdNameDateSql);

    private readonly NpgsqlCommand _insertDishCommand = new(DatabaseConstants.InsertDishWithNameSql)
    {
        Parameters =
        {
            new("name", NpgsqlDbType.Varchar)
        }
    };

    private readonly NpgsqlCommand _insertOccurrenceCommand = new(DatabaseConstants.InsertOccurrenceSql)
    {
        Parameters =
        {
            new("dish", NpgsqlDbType.Uuid),
            new("date", NpgsqlDbType.Date),
            new("review_status", NpgsqlDbType.Unknown),
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
            new("original_name", NpgsqlDbType.Varchar),
            new("alias_name", NpgsqlDbType.Varchar),
            new("dish", NpgsqlDbType.Uuid)
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

    public DatabaseWrapper(string connectionString)
    {
        _databaseConnection = new(connectionString);

        // Set database connection for all non batch commands
        foreach (var field in typeof(DatabaseWrapper).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.FieldType != typeof(NpgsqlCommand)) continue;

            if (field.GetValue(this) is NpgsqlCommand command)
                command.Connection = _databaseConnection;
        }

        _commandBatch.Connection = _databaseConnection;
    }

    public void ConnectAndPrepare()
    {
        _databaseConnection.Open();
        _databaseConnection.TypeMapper.MapEnum<ReviewStatus>("review_status");

        _selectDishByNameCommand.Prepare();
        _selectDishByAliasNameCommand.Prepare();
        _insertDishCommand.Prepare();
        _insertDishAliasCommand.Prepare();
        _insertOccurrenceCommand.Prepare();
        _deleteOccurrenceCommand.Prepare();
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

    public Guid? ExecuteSelectDishByNameCommand(string name)
    {
        _selectDishByNameCommand.Parameters["name"].Value =
            Converter.ExtractElementFromTitle(name, Converter.TitleElement.Name);
        return (Guid?) _selectDishByNameCommand.ExecuteScalar();
    }

    public Guid? ExecuteSelectDishAliasByNameCommand(string name)
    {
        _selectDishByAliasNameCommand.Parameters["alias_name"].Value =
            Converter.SanitizeString(Converter.ExtractElementFromTitle(name, Converter.TitleElement.Name));
        return (Guid?) _selectDishByAliasNameCommand.ExecuteScalar();
    }

    public Dictionary<DateOnly, List<Tuple<Guid, Guid>>> ExecuteSelectOccurrenceIdNameDateCommand()
    {
        var dateMapping = new Dictionary<DateOnly, List<Tuple<Guid, Guid>>>();

        using var reader = _selectOccurrenceIdNameDateCommand.ExecuteReader();
        while (reader.Read())
        {
            var occurrenceDate = DateOnly.FromDateTime(reader.GetDateTime("date"));
            var occurrenceDishTuple = new Tuple<Guid, Guid>(reader.GetGuid("dish"), reader.GetGuid("id"));
            if (!dateMapping.ContainsKey(occurrenceDate))
                dateMapping.Add(occurrenceDate, new List<Tuple<Guid, Guid>>());
            dateMapping[occurrenceDate].Add(occurrenceDishTuple);
        }

        return dateMapping;
    }

    public Guid? ExecuteInsertDishCommand(string title)
    {
        _insertDishCommand.Parameters["name"].Value =
            Converter.ExtractElementFromTitle(title, Converter.TitleElement.Name);
        return (Guid?) _insertDishCommand.ExecuteScalar();
    }

    public Guid? ExecuteInsertOccurrenceCommand(Tag tag, Item item, Guid dish, ReviewStatus status)
    {
        _insertOccurrenceCommand.Parameters["dish"].Value = dish;
        _insertOccurrenceCommand.Parameters["date"].Value = Converter.GetDateFromTimestamp(tag.Timestamp);
        _insertOccurrenceCommand.Parameters["review_status"].Value = status;
        var kj = Converter.FloatStringToInt(item.Kj);
        _insertOccurrenceCommand.Parameters["kj"].Value = kj == null ? DBNull.Value : (int) kj / 1000;
        var kcal = Converter.FloatStringToInt(item.Kcal);
        _insertOccurrenceCommand.Parameters["kcal"].Value = kcal == null ? DBNull.Value : (int) kcal / 1000;
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

    public Guid? ExecuteInsertDishAliasCommand(string dishName, Guid dish)
    {
        var extractedDishName = Converter.ExtractElementFromTitle(dishName, Converter.TitleElement.Name);
        _insertDishAliasCommand.Parameters["original_name"].Value = extractedDishName;
        _insertDishAliasCommand.Parameters["alias_name"].Value = Converter.SanitizeString(extractedDishName);
        _insertDishAliasCommand.Parameters["dish"].Value = dish;
        return (Guid?) _insertDishAliasCommand.ExecuteScalar();
    }

    public void ExecuteDeleteOccurrenceByIdCommand(Guid id)
    {
        _deleteOccurrenceCommand.Parameters["id"].Value = id;
        _deleteOccurrenceCommand.ExecuteNonQuery();
    }


    private static void SetParameterToValueOrNull(IDataParameter param, int? value)
    {
        param.Value = value.HasValue ? value : DBNull.Value;
    }

    public void Dispose()
    {
        _databaseConnection.Dispose();
        _selectDishByNameCommand.Dispose();
        _insertDishCommand.Dispose();
        _insertOccurrenceCommand.Dispose();
        _deleteOccurrenceCommand.Dispose();
        _commandBatch.Dispose();
    }
}