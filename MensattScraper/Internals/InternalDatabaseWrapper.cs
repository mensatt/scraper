using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace MensattScraper.Internals;

using static InternalDatabaseConstants;

public class InternalDatabaseWrapper
{
    private readonly NpgsqlConnection _databaseConnection;

    private readonly NpgsqlCommand _selectConfidenceSuggestionByIdCommand;
    private readonly NpgsqlCommand _insertConfidenceSuggestionCommand;
    private readonly NpgsqlCommand _deleteConfidenceSuggestionCommand;

    public event ConfidenceSuggestionInsertionEventHandler? ConfidenceSuggestionInsertion;

    public InternalDatabaseWrapper()
    {
        _databaseConnection = new(Environment.GetEnvironmentVariable("MENSATT_SCRAPER_INTERNAL_DB") ??
                                  throw new ArgumentException("Internal database url not set"));

        _selectConfidenceSuggestionByIdCommand = new(SelectConfidenceSuggestionSql, _databaseConnection)
        {
            Parameters =
            {
                new("occurrence_id", NpgsqlDbType.Uuid)
            }
        };
        _insertConfidenceSuggestionCommand = new(InsertConfidenceSuggestionSql, _databaseConnection)
        {
            Parameters =
            {
                new("occurrence_id", NpgsqlDbType.Uuid),
                new("dish_id", NpgsqlDbType.Uuid),
                new("dish_alias", NpgsqlDbType.Varchar),
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                new("suggestions", NpgsqlDbType.Array | NpgsqlDbType.Varchar),
            }
        };
        _deleteConfidenceSuggestionCommand = new(DeleteConfidenceSuggestionSql, _databaseConnection)
        {
            Parameters =
            {
                new("occurrence_id", NpgsqlDbType.Uuid),
            }
        };
    }

    public void Init()
    {
        _databaseConnection.Open();

        using var createConfidenceSuggestionsCommand =
            new NpgsqlCommand(CreateConfidenceSuggestionsTableSql, _databaseConnection);
        createConfidenceSuggestionsCommand.ExecuteNonQuery();

        _selectConfidenceSuggestionByIdCommand.Prepare();
        _insertConfidenceSuggestionCommand.Prepare();
        _deleteConfidenceSuggestionCommand.Prepare();
    }

    public void InsertConfidenceSuggestion(ConfidenceSuggestion confidenceSuggestion)
    {
        var nameSuggestions = confidenceSuggestion.Suggestions.Select(tuple => tuple.Item2).ToArray();

        _insertConfidenceSuggestionCommand.Parameters["occurrence_id"].Value = confidenceSuggestion.OccurrenceId;
        _insertConfidenceSuggestionCommand.Parameters["dish_id"].Value = confidenceSuggestion.DishId;
        _insertConfidenceSuggestionCommand.Parameters["dish_alias"].Value =
            confidenceSuggestion.CreatedDishAlias;
        _insertConfidenceSuggestionCommand.Parameters["suggestions"].Value = nameSuggestions;

        _insertConfidenceSuggestionCommand.ExecuteNonQuery();

        OnConfidenceSuggestionInsert(new(confidenceSuggestion));
    }

    public ConfidenceSuggestion GetConfidenceSuggestion(Guid occurrenceId)
    {
        _selectConfidenceSuggestionByIdCommand.Parameters["occurrence_id"].Value = occurrenceId;
        using var reader = _selectConfidenceSuggestionByIdCommand.ExecuteReader();
        reader.Read();
        return new(reader.GetGuid("occurrence_id"), reader.GetGuid("dish_id"),
            reader.GetString("dish_alias"), reader.GetFieldValue<string[]>("suggestions"));
    }

    public void DeleteConfidenceSuggestion(Guid occurrenceId)
    {
        _deleteConfidenceSuggestionCommand.Parameters["occurrence_id"].Value = occurrenceId;
        _deleteConfidenceSuggestionCommand.ExecuteNonQuery();
    }

    private void OnConfidenceSuggestionInsert(ConfidenceSuggestionInsertionEventArgs e)
    {
        var handler = ConfidenceSuggestionInsertion;
        handler?.Invoke(this, e);
    }
}

public delegate void ConfidenceSuggestionInsertionEventHandler(object sender,
    ConfidenceSuggestionInsertionEventArgs args);

public class ConfidenceSuggestionInsertionEventArgs
{
    public ConfidenceSuggestionInsertionEventArgs(ConfidenceSuggestion confidenceSuggestion)
    {
        ConfidenceSuggestion = confidenceSuggestion;
    }

    public ConfidenceSuggestion ConfidenceSuggestion { get; }
}