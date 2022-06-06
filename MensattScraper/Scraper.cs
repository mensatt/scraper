using System.Diagnostics;
using System.Xml.Serialization;
using MensattScraper.DatabaseSupport;
using MensattScraper.DataIngest;
using MensattScraper.DestinationCompat;
using MensattScraper.SourceCompat;
using MensattScraper.Util;

namespace MensattScraper;

public class Scraper : IDisposable
{
    private readonly XmlSerializer _xmlSerializer;
    private readonly IDataProvider _dataProvider;
    private readonly IDatabaseWrapper _databaseWrapper;
    private DatabaseMapping _databaseMapping = null!;

    private Dictionary<DateOnly, List<Tuple<Guid, Guid>>>? _dailyOccurrences;

    public Scraper(IDatabaseWrapper databaseWrapper, IDataProvider dataProvider)
    {
        _databaseWrapper = databaseWrapper;
        _dataProvider = dataProvider;
        _xmlSerializer = new(typeof(Speiseplan));
    }

    public void Initialize()
    {
        _databaseWrapper.ConnectAndPrepare();
        _dailyOccurrences = _databaseWrapper.ExecuteSelectOccurrenceIdNameDateCommand();
        // TODO: Make global over all scrapers
        _databaseMapping = new(_databaseWrapper);
        _databaseMapping.RefreshDatabaseMappings();
    }

    public void Scrape()
    {
        if (_dailyOccurrences is null)
            throw new NullReferenceException("_dailyOccurrences must not be null");

        var timer = new Stopwatch();

        foreach (var rawStream in _dataProvider.RetrieveStream())
        {
            using var dataStream = rawStream;

            timer.Restart();

            // Free up entries in the past
            _dailyOccurrences.RemoveAllByKey(key => key < DateOnly.FromDateTime(DateTime.Today));

            // Only save data coming from the network, as it may not be readily available
            if (_dataProvider is HttpDataProvider)
            {
                using var outputFile =
                    File.Create(
                        $"content{Path.DirectorySeparatorChar}{DateTime.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss")}.xml");
                dataStream.CopyTo(outputFile);
                dataStream.Position = 0;
            }

            var menu = (Speiseplan?) _xmlSerializer.Deserialize(dataStream);

            if (menu is null)
            {
                Console.Error.WriteLine("Could not deserialize menu");
                continue;
            }

            // Happens on holidays, where the xml is provided but empty
            if (menu.Tags is null)
            {
                Console.Error.WriteLine("Menu DayTag was null");
                continue;
            }

            foreach (var current in menu.Tags)
            {
                if (current.Items is null)
                {
                    Console.Error.WriteLine("Day contained no items");
                    continue;
                }

                var currentDay = Converter.GetDateFromTimestamp(current.Timestamp);
                var isInFarFuture = DateOnly.FromDateTime(DateTime.Now).AddDays(2) < currentDay;
                bool firstPullOfTheDay;
                if (!_dailyOccurrences.ContainsKey(currentDay))
                {
                    _dailyOccurrences.Add(currentDay, new());
                    firstPullOfTheDay = true;
                }
                else
                {
                    firstPullOfTheDay = false;
                }

                // Used to compare the dishes of one day with the dishes of the same day, on a previous scrape
                // Without this, it would not be possible to check for removed dishes (which get deleted if they are
                // to far in the future)
                var dailyDishes = new HashSet<Guid>();

                _databaseWrapper.ResetBatch();

                foreach (var item in current.Items)
                {
                    if (item.Title is null)
                    {
                        Console.Error.WriteLine("Item did not contain title");
                        continue;
                    }

                    // This gets shown as a placeholder, before the different kinds of pizza are known
                    if (item.Title == "Heute ab 15.30 Uhr Pizza an unserer Cafebar")
                        continue;

                    var dishUuid = InsertDishIfNotExists(item.Title);

                    dailyDishes.Add(dishUuid);

                    var occurrenceStatus =
                        firstPullOfTheDay ? ReviewStatus.AWAITING_APPROVAL : ReviewStatus.UPDATED;

                    if (!firstPullOfTheDay)
                    {
                        var savedDishOccurrence = _dailyOccurrences[currentDay].Find(x => x.Item1 == dishUuid);

                        // If we got an occurrence with this dish already, do nothing
                        if (savedDishOccurrence is not null)
                            continue; // Update in the future

                        // If it is in the far future, the old one will be replaced by this
                        if (isInFarFuture)
                            occurrenceStatus = ReviewStatus.AWAITING_APPROVAL;
                    }

                    var occurrenceUuid =
                        (Guid) _databaseWrapper.ExecuteInsertOccurrenceCommand(
                            _databaseMapping.GetLocationGuidByLocationId(menu.LocationId), current, item, dishUuid,
                            occurrenceStatus)!;

                    _dailyOccurrences[currentDay].Add(new(dishUuid, occurrenceUuid));

                    foreach (var tag in Converter.ExtractSingleTagsFromTitle(item.Title))
                    {
                        _databaseWrapper.AddInsertOccurrenceTagCommandToBatch(occurrenceUuid, tag);
                    }

                    if (item.Beilagen is null)
                        continue;

                    foreach (var sideDish in Converter.GetSideDishes(item.Beilagen))
                    {
                        var sideDishUuid = InsertDishIfNotExists(sideDish);

                        _databaseWrapper.AddInsertOccurrenceSideDishCommandToBatch(occurrenceUuid, sideDishUuid);
                    }
                }

                _databaseWrapper.ExecuteBatch();


                // Delete all dishes, that were removed on a day which is more than two days in the future


                foreach (var (dishId, occurrenceId) in _dailyOccurrences[currentDay])
                {
                    // If this dish does not exist in the current XML, delete it
                    if (!dailyDishes.Contains(dishId))
                    {
                        if (isInFarFuture)
                            _databaseWrapper.ExecuteDeleteOccurrenceByIdCommand(occurrenceId);
                        else
                            _databaseWrapper.ExecuteUpdateOccurrenceReviewStatusByIdCommand(
                                ReviewStatus.PENDING_DELETION, occurrenceId);
                    }
                }
            }

            Console.WriteLine($"Scraping took {timer.ElapsedMilliseconds}ms");

            Thread.Sleep((int) _dataProvider.GetDataDelayInSeconds * 1000);
        }
    }

    private Guid InsertDishIfNotExists(string dishTitle)
    {
        return (Guid) (_databaseWrapper.ExecuteSelectDishAliasByNameCommand(dishTitle) ??
                       _databaseWrapper.ExecuteInsertDishAliasCommand(dishTitle,
                           (Guid) (_databaseWrapper.ExecuteSelectDishByGermanNameCommand(dishTitle) ??
                                   _databaseWrapper.ExecuteInsertGermanDishCommand(dishTitle)!)))!;
    }

    public void Dispose()
    {
        if (_dataProvider is IDisposable disposableDataProvider)
            disposableDataProvider.Dispose();

        _databaseWrapper.Dispose();
    }
}