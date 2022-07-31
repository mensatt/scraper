namespace MensattScraper.DatabaseSupport;

internal static class DatabaseConstants
{
    internal const string SelectIdByGermanNameSql = "SELECT id FROM dish WHERE name_de=@name_de";

    internal const string SelectDishIdByAliasNameSql =
        "SELECT dish FROM dish_alias WHERE normalized_alias_name=@normalized_alias_name";

    internal const string SelectOccurrenceIdNameDateSql = "SELECT id, dish, date FROM occurrence";

    internal const string SelectLocationIdNameLocationId = "SELECT id, name, external_id FROM location";

    internal const string SelectTagAll = "SELECT key FROM tag";

    internal const string SelectDishAliasIdNormalized = "SELECT dish, normalized_alias_name FROM dish_alias";

    internal const string InsertDishWithGermanNameSql =
        "INSERT INTO dish (name_de, name_en) VALUES (@name_de, @name_en) ON CONFLICT (name_de) DO NOTHING RETURNING id";

    internal const string InsertOccurrenceSql =
        "INSERT INTO occurrence (location, dish, date, status, kj, kcal, fat, saturated_fat, " +
        "carbohydrates, sugar, fiber, protein, salt, price_student, " +
        "price_staff, price_guest) " +
        "VALUES (@location, @dish, @date, @status, @kj, @kcal, @fat, @saturated_fat, " +
        "@carbohydrates, @sugar, @fiber, @protein, @salt, @price_student, " +
        "@price_staff, @price_guest) RETURNING id";

    internal const string InsertOccurrenceSideDishSql =
        "INSERT INTO occurrence_side_dishes VALUES (@occurrence, @dish)";

    internal const string InsertOccurrenceTagSql = "INSERT INTO occurrence_tag VALUES (@occurrence, @tag)";

    internal const string InsertDishAliasSql =
        "INSERT INTO dish_alias VALUES(@alias_name, @normalized_alias_name, @dish) RETURNING dish";

    internal const string UpdateOccurrenceReviewStatusByIdSql =
        "UPDATE occurrence SET status = @status WHERE id=@id";

    internal const string UpdateOccurrenceDishByIdSql = "UPDATE occurrence SET dish = @dish WHERE id=@id";

    internal const string UpdateDishAliasDishByAliasNameSql =
        "UPDATE dish_alias SET dish = @dish WHERE alias_name=@alias_name";

    internal const string DeleteDishByIdSql = "DELETE FROM dish WHERE id=@id";

    internal const string DeleteOccurrenceByIdSql = "DELETE FROM occurrence WHERE id=@id";
}