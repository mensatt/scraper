namespace MensattScraper.DestinationCompat;

internal static class DatabaseConstants
{
    internal const string SelectIdByNameSql = "SELECT id FROM dish WHERE name=@name";

    internal const string InsertDishWithNameSql =
        "INSERT INTO dish (name) VALUES (@name) ON CONFLICT (name) DO NOTHING RETURNING id";

    internal const string InsertOccurrenceSql =
        "INSERT INTO occurrence (dish, date, review_status, kj, kcal, fat, saturated_fat, " +
        "carbohydrates, sugar, fiber, protein, salt, price_student, " +
        "price_staff, price_guest) " +
        "VALUES (@dish, @date, @review_status, @kj, @kcal, @fat, @saturated_fat, " +
        "@carbohydrates, @sugar, @fiber, @protein, @salt, @price_student, " +
        "@price_staff, @price_guest) RETURNING id";

    internal const string InsertOccurrenceSideDishSql =
        "INSERT INTO occurrence_side_dishes VALUES (@occurrence, @dish)";

    internal const string InsertOccurrenceTagSql = "INSERT INTO occurrence_tag VALUES (@occurrence, @tag)";
    internal const string DeleteOccurrenceByIdSql = "DELETE FROM occurrence WHERE id=@id";
}