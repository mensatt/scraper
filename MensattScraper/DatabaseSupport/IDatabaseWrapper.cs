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
    Guid? ExecuteSelectDishAliasByNameCommand(string? name);
    Dictionary<DateOnly, List<Tuple<Guid, Guid>>> ExecuteSelectOccurrenceIdNameDateCommand();
    List<Location> ExecuteSelectIdNameLocationIdCommand();
    List<string> ExecuteSelectTagAllCommand();
    public Guid? ExecuteInsertDishCommand(string? primaryTitle, string? secondaryTitle);

    Guid? ExecuteInsertOccurrenceCommand(Guid locationId, DayTag dayTag, Item item, Guid dish,
        ReviewStatus status);

    Guid? ExecuteInsertDishAliasCommand(string? dishName, Guid dish);
    void ExecuteUpdateOccurrenceReviewStatusByIdCommand(ReviewStatus status, Guid id);
    void ExecuteDeleteOccurrenceByIdCommand(Guid id);
    void Dispose();
}