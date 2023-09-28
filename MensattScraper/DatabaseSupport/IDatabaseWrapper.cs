﻿using MensattScraper.DestinationCompat;
using MensattScraper.SourceCompat;

namespace MensattScraper.DatabaseSupport;

public interface IDatabaseWrapper
{
    void ConnectAndPrepare();
    void ResetBatch();
    void ExecuteBatch();
    void AddInsertOccurrenceTagCommandToBatch(Guid occurrence, string tag);
    void AddInsertOccurrenceSideDishCommandToBatch(Guid occurrence, Guid sideDish);
    Guid? ExecuteSelectDishByGermanNameCommand(string? name);
    Guid? ExecuteSelectDishNormalizedAliasByNameCommand(string? name);
    Dictionary<DateOnly, List<Tuple<Guid, Guid>>> ExecuteSelectOccurrenceIdNameDateByLocationCommand(Guid locationId);
    List<Location> ExecuteSelectIdNameLocationIdCommand();
    List<string> ExecuteSelectTagAllCommand();
    Dictionary<string, Guid> ExecuteSelectDishAliasesNormalizedDishCommand();

    Guid? ExecuteInsertDishCommand(string? primaryTitle, string? secondaryTitle);

    Guid? ExecuteInsertOccurrenceCommand(Guid locationId, DayTag dayTag, Item item, Guid dish,
        OccurrenceStatus status);

    Guid? ExecuteInsertDishAliasCommand(string? dishName, Guid dish);

    void ExecuteUpdateOccurrenceReviewStatusByIdCommand(OccurrenceStatus status, Guid id);

    void ExecuteUpdateOccurrenceDishByIdCommand(Guid dish, Guid id);

    void ExecuteUpdateDishAliasDishByAliasNameCommand(Guid dish, string aliasName);

    void ExecuteDeleteDishByIdCommand(Guid id);

    void ExecuteDeleteOccurrenceByIdCommand(Guid id);

    void Dispose();
}
