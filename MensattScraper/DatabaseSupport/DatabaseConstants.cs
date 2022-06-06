namespace MensattScraper.DatabaseSupport;

internal static class DatabaseConstants
{
    internal const string SelectIdByGermanNameSql = "SELECT id FROM dish WHERE name_de=@name_de";

    internal const string SelectDishIdByAliasNameSql =
        "SELECT dish FROM dish_alias WHERE normalized_alias_name=@normalized_alias_name";

    internal const string SelectOccurrenceIdNameDateSql = "SELECT id, dish, date FROM occurrence";

    internal const string SelectLocationIdNameLocationId = "SELECT id, name, location_id FROM location";

    internal const string SelectTagAll = "SELECT * FROM tag";

    internal const string InsertDishWithGermanNameSql =
        "INSERT INTO dish (name_de, name_en) VALUES (@name_de, @name_en) ON CONFLICT (name_de) DO NOTHING RETURNING id";

    internal const string InsertOccurrenceSql =
        "INSERT INTO occurrence (location, dish, date, review_status, kj, kcal, fat, saturated_fat, " +
        "carbohydrates, sugar, fiber, protein, salt, price_student, " +
        "price_staff, price_guest) " +
        "VALUES (@location, @dish, @date, @review_status, @kj, @kcal, @fat, @saturated_fat, " +
        "@carbohydrates, @sugar, @fiber, @protein, @salt, @price_student, " +
        "@price_staff, @price_guest) RETURNING id";

    internal const string InsertOccurrenceSideDishSql =
        "INSERT INTO occurrence_side_dishes VALUES (@occurrence, @dish)";

    internal const string InsertOccurrenceTagSql = "INSERT INTO occurrence_tag VALUES (@occurrence, @tag)";

    internal const string InsertDishAliasSql =
        "INSERT INTO dish_alias VALUES(@alias_name, @normalized_alias_name, @dish) RETURNING dish";

    internal const string UpdateOccurrenceReviewStatusByIdSql =
        "UPDATE occurrence SET review_status = @review_status WHERE id=@id";

    internal const string DeleteOccurrenceByIdSql = "DELETE FROM occurrence WHERE id=@id";
}