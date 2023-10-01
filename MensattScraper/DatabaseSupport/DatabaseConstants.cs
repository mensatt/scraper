namespace MensattScraper.DatabaseSupport;

internal static class DatabaseConstants
{
    internal const string SelectIdByGermanNameSql = "SELECT id FROM dish WHERE name_de=@name_de";

    internal const string SelectDishIdByNormalizedAliasNameSql =
        "SELECT dish FROM dish_alias WHERE normalized_alias_name=@normalized_alias_name";

    internal const string SelectOccurrenceIdNameDateByLocationSql =
        "SELECT id, dish, date, not_available_after FROM occurrence WHERE location=@location";

    internal const string SelectFullOccurrenceByLocationDateSql =
        "SELECT id, date, kj, kcal, fat, saturated_fat, carbohydrates," +
        " sugar, fiber, protein, salt, price_student, price_staff, price_guest," +
        " dish, not_available_after FROM occurrence WHERE location=@location" +
        " AND date>=@date";

    internal const string SelectOccurrenceTagsByIdSql =
        "SELECT ot.tag FROM occurrence o JOIN occurrence_tags ot on o.id = ot.occurrence" +
        " WHERE o.id = @id";

    internal const string SelectLocationIdNameLocationIdSql = "SELECT id, name, external_id FROM location";

    internal const string SelectTagAllSql = "SELECT key FROM tag";

    internal const string InsertDishWithGermanNameSql =
        "INSERT INTO dish (id, name_de, name_en) VALUES (@id, @name_de, @name_en) ON CONFLICT (name_de) DO NOTHING RETURNING id";

    internal const string InsertOccurrenceSql =
        "INSERT INTO occurrence (id, location, dish, date, kj, kcal, fat, saturated_fat, " +
        "carbohydrates, sugar, fiber, protein, salt, price_student, " +
        "price_staff, price_guest) " +
        "VALUES (@id, @location, @dish, @date, @kj, @kcal, @fat, @saturated_fat, " +
        "@carbohydrates, @sugar, @fiber, @protein, @salt, @price_student, " +
        "@price_staff, @price_guest) RETURNING id";

    internal const string InsertOccurrenceSideDishSql =
        "INSERT INTO occurrence_side_dishes VALUES (@occurrence, @dish)";

    internal const string InsertOccurrenceTagSql = "INSERT INTO occurrence_tags VALUES (@occurrence, @tag)";

    internal const string InsertDishAliasSql =
        "INSERT INTO dish_alias VALUES(@alias_name, @normalized_alias_name, @dish) RETURNING dish";

    internal const string UpdateOccurrenceNotAvailableAfterByIdSql =
        "UPDATE occurrence SET not_available_after=@not_available_after WHERE id=@id";
}
