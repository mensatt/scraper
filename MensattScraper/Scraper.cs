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

    private readonly IDataProvider _primaryDataProvider;

    // Used to support another language
    private readonly IDataProvider? _secondaryDataProvider;

    private readonly IDatabaseWrapper _databaseWrapper;
    private readonly DatabaseMapping _databaseMapping;

    private Dictionary<DateOnly, List<Tuple<Guid, Guid>>>? _dailyOccurrences;

    public Scraper(IDatabaseWrapper databaseWrapper, DatabaseMapping databaseMapping, IDataProvider primaryDataProvider)
    {
        _databaseWrapper = databaseWrapper;
        _primaryDataProvider = primaryDataProvider;
        _xmlSerializer = new(typeof(Speiseplan));
        _databaseMapping = databaseMapping;

        // Multi-lang is only supported for HttpDataProviders for now
        if (_primaryDataProvider is HttpDataProvider httpDataProvider)
        {
            _secondaryDataProvider = new HttpDataProvider(httpDataProvider.ApiUrl.Replace("xml/", "xml/en/"));
        }
    }

    public void Initialize()
    {
        _databaseWrapper.ConnectAndPrepare();
        _dailyOccurrences = _databaseWrapper.ExecuteSelectOccurrenceIdNameDateCommand();
        _databaseMapping.RefreshDatabaseMappings();
    }

    public void Scrape()
    {
        if (_dailyOccurrences is null)
            throw new NullReferenceException("_dailyOccurrences must not be null");

        var timer = new Stopwatch();

        foreach (var primaryRawStream in _primaryDataProvider.RetrieveStream())
        {
            timer.Restart();

            // Free up entries in the past
            _dailyOccurrences.RemoveAllByKey(key => key < DateOnly.FromDateTime(DateTime.Today));

            using var primaryDataStream = primaryRawStream;

            // Retrieve secondary stream if necessary
            var secondaryRawStream = _secondaryDataProvider?.RetrieveStream();
            using var secondaryDataStream = secondaryRawStream?.First();

            SaveDataStream(_primaryDataProvider, primaryDataStream);
            SaveDataStream(_secondaryDataProvider, secondaryDataStream);

            var primaryMenu = (Speiseplan?) _xmlSerializer.Deserialize(primaryDataStream);
            var secondaryMenu = secondaryDataStream != null
                ? (Speiseplan?) _xmlSerializer.Deserialize(secondaryDataStream)
                : null;

            if (primaryMenu is null)
            {
                Console.Error.WriteLine("Could not deserialize menu");
                continue;
            }

            // Happens on holidays, where the xml is provided but empty
            if (primaryMenu.Tags is null)
            {
                Console.Error.WriteLine("Menu DayTag was null");
                continue;
            }

            uint secondaryDayTagIndex = 0;
            foreach (var current in primaryMenu.Tags)
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

                uint secondaryItemIndex = 0;
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

                    var secondaryTitle = secondaryMenu?.Tags?[secondaryDayTagIndex].Items?[secondaryItemIndex].Title;
                    secondaryItemIndex++;

                    var dishUuid = InsertDishIfNotExists(item.Title, secondaryTitle);

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
                            _databaseMapping.GetLocationGuidByLocationId(primaryMenu.LocationId), current, item,
                            dishUuid,
                            occurrenceStatus)!;

                    _dailyOccurrences[currentDay].Add(new(dishUuid, occurrenceUuid));

                    foreach (var tag in Converter.ExtractSingleTagsFromTitle(item.Title))
                    {
                        _databaseWrapper.AddInsertOccurrenceTagCommandToBatch(occurrenceUuid, tag);
                    }

                    if (item.Beilagen is null)
                        continue;

                    var secondarySideDishIndex = 0;
                    foreach (var sideDish in Converter.GetSideDishes(item.Beilagen))
                    {
                        var secondarySideDishTitle =
                            Converter.GetSideDishes(secondaryMenu?.Tags?[secondaryDayTagIndex]
                                .Items?[secondaryItemIndex].Beilagen ?? string.Empty)[secondarySideDishIndex];

                        var sideDishUuid = InsertDishIfNotExists(sideDish, secondarySideDishTitle);

                        _databaseWrapper.AddInsertOccurrenceSideDishCommandToBatch(occurrenceUuid, sideDishUuid);
                        secondarySideDishIndex++;
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

                secondaryDayTagIndex++;
            }

            Console.WriteLine($"Scraping took {timer.ElapsedMilliseconds}ms");

            Thread.Sleep((int) _primaryDataProvider.GetDataDelayInSeconds * 1000);
        }
    }

    private static void SaveDataStream(IDataProvider? dataProvider, Stream? stream)
    {
        if (dataProvider is null || stream is null)
            return;
        // Only save data coming from the network, as it may not be readily available
        if (dataProvider is not HttpDataProvider) return;
        using var outputFile =
            File.Create(
                $"content{Path.DirectorySeparatorChar}{DateTime.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss.fff")}.xml");
        stream.CopyTo(outputFile);
        stream.Position = 0;
    }

    private Guid InsertDishIfNotExists(string primaryDishTitle, string secondaryDishTitle)
    {
        return (Guid) (_databaseWrapper.ExecuteSelectDishAliasByNameCommand(primaryDishTitle) ??
                       _databaseWrapper.ExecuteInsertDishAliasCommand(primaryDishTitle,
                           (Guid) (_databaseWrapper.ExecuteSelectDishByGermanNameCommand(primaryDishTitle) ??
                                   _databaseWrapper.ExecuteInsertDishCommand(primaryDishTitle, secondaryDishTitle)!)))!;
    }

    public void Dispose()
    {
        if (_primaryDataProvider is IDisposable disposableDataProvider)
            disposableDataProvider.Dispose();

        _databaseWrapper.Dispose();
    }
}