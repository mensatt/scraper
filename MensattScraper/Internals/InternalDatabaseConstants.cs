namespace MensattScraper.Internals;

internal static class InternalDatabaseConstants
{
    internal const string CreateConfidenceSuggestionsTableSql = @"
CREATE TABLE IF NOT EXISTS confidence_suggestions
(
    occurrence_id uuid NOT NULL PRIMARY KEY,
    dish_id uuid NOT NULL,
    dish_alias varchar NOT NULL UNIQUE,
    suggestions varchar[] NOT NULL
);";

    internal const string SelectConfidenceSuggestionSql =
        @"SELECT occurrence_id, dish_id, dish_alias, suggestions FROM confidence_suggestions WHERE occurrence_id=@occurrence_id";

    internal const string InsertConfidenceSuggestionSql =
        @"INSERT INTO confidence_suggestions (occurrence_id, dish_id, dish_alias, suggestions) VALUES(@occurrence_id, @dish_id, @dish_alias, @suggestions)";

    internal const string DeleteConfidenceSuggestionSql =
        @"DELETE FROM confidence_suggestions WHERE occurrence_id=@occurrence_id";
}