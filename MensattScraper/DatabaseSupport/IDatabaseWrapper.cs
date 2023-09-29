using MensattScraper.DestinationCompat;
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
    Dictionary<DateOnly, List<Occurrence>> ExecuteSelectOccurrenceIdNameDateByLocationCommand(Guid locationId);
    List<Location> ExecuteSelectIdNameLocationIdCommand();
    List<string> ExecuteSelectTagAllCommand();
    Dictionary<string, Guid> ExecuteSelectDishAliasesNormalizedDishCommand();

    Guid? ExecuteInsertDishCommand(string? primaryTitle, string? secondaryTitle);

    Guid? ExecuteInsertOccurrenceCommand(Guid locationId, DayTag dayTag, Item item, Guid dish);

    Guid? ExecuteInsertDishAliasCommand(string? dishName, Guid dish);

    void ExecuteUpdateOccurrenceNotAvailableAfterByIdCommand(Guid id, DateTime notAvailableAfter);


    void Dispose();
}
