using MensattScraper.SourceCompat;

namespace MensattScraper.DestinationCompat;

public interface IDatabaseWrapper : IDisposable
{
    void ConnectAndPrepare();
    void ResetBatch();
    void ExecuteBatch();
    void AddInsertOccurrenceTagCommandToBatch(Guid occurrence, string tag);
    void AddInsertOccurrenceSideDishCommandToBatch(Guid occurrence, Guid sideDish);
    Guid? ExecuteSelectDishByNameCommand(string name);
    Guid? ExecuteSelectDishAliasByNameCommand(string name);
    Dictionary<DateOnly, List<Tuple<Guid, Guid>>> ExecuteSelectOccurrenceIdNameDateCommand();
    Guid? ExecuteInsertDishCommand(string title);
    Guid? ExecuteInsertOccurrenceCommand(Tag tag, Item item, Guid dish, ReviewStatus status);
    Guid? ExecuteInsertDishAliasCommand(string dishName, Guid dish);
    void ExecuteUpdateOccurrenceReviewStatusByIdCommand(ReviewStatus status, Guid id);
    void ExecuteDeleteOccurrenceByIdCommand(Guid id);
}