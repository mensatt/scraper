namespace MensattScraper.DatabaseSupport;

internal static class DatabaseConstants
{
    internal const string SelectIdByGermanNameSql = "SELECT id FROM dish WHERE name_de=@name_de";

    internal const string SelectDishIdByNormalizedAliasNameSql =
        "SELECT dish FROM dish_alias WHERE normalized_alias_name=@normalized_alias_name";

    internal const string SelectOccurrenceIdNameDateSql = "SELECT id, dish, date FROM occurrence";

    internal const string SelectLocationIdNameLocationIdSql = "SELECT id, name, external_id FROM location";

    internal const string SelectTagAllSql = "SELECT key FROM tag";

    internal const string SelectDishAliasNormalizedDishSql = "SELECT normalized_alias_name, dish FROM dish_alias";

    internal const string InsertDishWithGermanNameSql =
        "INSERT INTO dish (id, name_de, name_en) VALUES (@id, @name_de, @name_en) ON CONFLICT (name_de) DO NOTHING RETURNING id";

    internal const string InsertOccurrenceSql =
        "INSERT INTO occurrence (id, location, dish, date, status, kj, kcal, fat, saturated_fat, " +
        "carbohydrates, sugar, fiber, protein, salt, price_student, " +
        "price_staff, price_guest) " +
        "VALUES (@id, @location, @dish, @date, @status, @kj, @kcal, @fat, @saturated_fat, " +
        "@carbohydrates, @sugar, @fiber, @protein, @salt, @price_student, " +
        "@price_staff, @price_guest) RETURNING id";

    internal const string InsertOccurrenceSideDishSql =
        "INSERT INTO occurrence_side_dishes VALUES (@occurrence, @dish)";

    internal const string InsertOccurrenceTagSql = "INSERT INTO occurrence_tags VALUES (@occurrence, @tag)";

    internal const string InsertDishAliasSql =
        "INSERT INTO dish_alias VALUES(@alias_name, @normalized_alias_name, @dish) RETURNING dish";

    internal const string UpdateOccurrenceReviewStatusByIdSql =
        "UPDATE occurrence SET status = @status WHERE id=@id";

    internal const string DeleteOccurrenceByIdSql = "DELETE FROM occurrence WHERE id=@id";

    #region Confidence Suggestion Support

    internal const string UpdateOccurrenceDishByIdSql = "UPDATE occurrence SET dish = @dish WHERE id=@id";

    internal const string UpdateDishAliasDishByAliasNameSql =
        "UPDATE dish_alias SET dish = @dish WHERE alias_name=@alias_name";

    internal const string DeleteDishByIdSql = "DELETE FROM dish WHERE id=@id";

    #endregion
}
