namespace MensattScraper.DatabaseSupport;

internal static class DatabaseConstants
{
    internal const string SelectIdByGermanNameSql = "SELECT id FROM dishes WHERE name_de=@name_de";

    internal const string SelectDishIdByNormalizedAliasNameSql =
        "SELECT dish FROM dishes_aliases WHERE normalized_alias_name=@normalized_alias_name";

    internal const string SelectOccurrenceIdNameDateByLocationSql =
        "SELECT id, dish, date, not_available_after FROM occurrences WHERE location=@location";

    internal const string SelectFullOccurrenceByLocationDateSql =
        "SELECT id, date, kj, kcal, fat, saturated_fat, carbohydrates," +
        " sugar, fiber, protein, salt, price_student, price_staff, price_guest," +
        " dish, not_available_after FROM occurrences WHERE location=@location" +
        " AND date>=@date";

    internal const string SelectOccurrenceTagsByIdSql =
        "SELECT ot.tag FROM occurrences o JOIN occurrences_tags ot on o.id = ot.occurrence" +
        " WHERE o.id = @id";

    internal const string SelectLocationIdNameLocationIdSql = "SELECT id, name, external_id FROM locations";

    internal const string SelectTagAllSql = "SELECT key FROM tags";

    internal const string InsertDishWithGermanNameSql =
        "INSERT INTO dishes (id, name_de, name_en) VALUES (@id, @name_de, @name_en) ON CONFLICT (name_de) DO NOTHING RETURNING id";

    internal const string InsertOccurrenceSql =
        "INSERT INTO occurrences (id, location, dish, date, kj, kcal, fat, saturated_fat, " +
        "carbohydrates, sugar, fiber, protein, salt, price_student, " +
        "price_staff, price_guest) " +
        "VALUES (@id, @location, @dish, @date, @kj, @kcal, @fat, @saturated_fat, " +
        "@carbohydrates, @sugar, @fiber, @protein, @salt, @price_student, " +
        "@price_staff, @price_guest) RETURNING id";

    internal const string InsertOccurrenceSideDishSql =
        "INSERT INTO occurrences_side_dishes VALUES (@occurrence, @dish)";

    internal const string InsertOccurrenceTagSql = "INSERT INTO occurrences_tags VALUES (@occurrence, @tag)";

    internal const string InsertDishAliasSql =
        "INSERT INTO dishes_aliases VALUES(@alias_name, @normalized_alias_name, @dish) RETURNING dish";

    internal const string UpdateOccurrenceNotAvailableAfterByIdSql =
        "UPDATE occurrences SET not_available_after=@not_available_after WHERE id=@id";

    internal const string UpdateOccurrenceContentsByIdSql =
        "UPDATE occurrences SET kj=@kj, kcal=@kcal, fat=@fat, saturated_fat=@saturated_fat, " +
        "carbohydrates=@carbohydrates, sugar=@sugar, fiber=@fiber, protein=@protein, salt=@salt, " +
        "price_student=@price_student, price_staff=@price_staff, price_guest=@price_guest " +
        "WHERE id=@id";

    internal const string DeleteOccurrenceTagByIdTagSql =
        "DELETE FROM occurrences_tags WHERE occurrence=@id AND tag=@tag";
}
