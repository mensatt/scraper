using System.Diagnostics;
using System.Xml.Serialization;
using MensattScraper.DataIngest;
using MensattScraper.DestinationCompat;
using MensattScraper.SourceCompat;
using MensattScraper.Util;

namespace MensattScraper;

public class Program
{
    private const string ApiUrl = "https://www.max-manager.de/daten-extern/sw-erlangen-nuernberg/xml/mensa-sued.xml";
    private const string DbConnection = "HOST=localhost;Port=8080;Username=mensatt;Password=mensatt;Database=mensatt";

    private readonly IDatabaseWrapper _databaseWrapper;

    #region Singleton Creation

    private static Program? _instance;

    private Program()
    {
        _databaseWrapper = new NpgsqlDatabaseWrapper(DbConnection);
    }

    private static Program Instance => _instance ??= new();

    #endregion

    public static void Main(string[] args)
    {
        Instance.InitDbConnection();

        Instance.Scrape();
    }

    private void InitDbConnection()
    {
        _databaseWrapper.ConnectAndPrepare();
    }

    private void Scrape()
    {
        var serializer = new XmlSerializer(typeof(Speiseplan));

        // Dict<01.01.1970, List<Dish UUID -> Occurrence UUID>>
        var dailyOccurrences = _databaseWrapper.ExecuteSelectOccurrenceIdNameDateCommand();

        IDataProvider dataProvider = new HttpDataProvider(ApiUrl);

        var timer = new Stopwatch();

        while (dataProvider.HasNextStream())
        {
            timer.Restart();

            // Free up entries in the past
            dailyOccurrences.RemoveAllByKey(key => key < DateOnly.FromDateTime(DateTime.Today));

            using var reader = dataProvider.RetrieveStream();

            // Only save data coming from the network, as it may not be readily available
            if (dataProvider is HttpDataProvider)
            {
                using var outputFile =
                    File.Create(
                        $"content{Path.DirectorySeparatorChar}{DateTime.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss")}.xml");
                reader.CopyTo(outputFile);
                reader.Position = 0;
            }

            var menu = (Speiseplan?) serializer.Deserialize(reader);

            if (menu is null)
            {
                Console.Error.WriteLine("Could not deserialize menu");
                continue;
            }

            // Happens on holidays, where the xml is provided but empty
            if (menu.Tags is null)
            {
                Console.Error.WriteLine("Menu Tag was null");
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
                if (!dailyOccurrences.ContainsKey(currentDay))
                {
                    dailyOccurrences.Add(currentDay, new());
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

                    var occurrenceStatus = firstPullOfTheDay ? ReviewStatus.AWAITING_APPROVAL : ReviewStatus.UPDATED;

                    if (!firstPullOfTheDay)
                    {
                        var savedDishOccurrence = dailyOccurrences[currentDay].Find(x => x.Item1 == dishUuid);

                        // If we got an occurrence with this dish already, do nothing
                        if (savedDishOccurrence is not null)
                            continue; // Update in the future

                        // If it is in the far future, the old one will be replaced by this
                        if (isInFarFuture)
                            occurrenceStatus = ReviewStatus.AWAITING_APPROVAL;
                    }

                    var occurrenceUuid =
                        (Guid) _databaseWrapper.ExecuteInsertOccurrenceCommand(current, item, dishUuid,
                            occurrenceStatus)!;

                    dailyOccurrences[currentDay].Add(new(dishUuid, occurrenceUuid));

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


                foreach (var (dishId, occurrenceId) in dailyOccurrences[currentDay])
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


            Console.WriteLine(" --> this took " + timer.ElapsedMilliseconds + "ms");

            Thread.Sleep((int) dataProvider.GetDataDelayInSeconds * 1000);
        }

        // Dispose if needed
        if (dataProvider is IDisposable disposableDataProvider)
            disposableDataProvider.Dispose();

        _databaseWrapper.Dispose();
    }

    private Guid InsertDishIfNotExists(string dishTitle)
    {
        return (Guid) (_databaseWrapper.ExecuteSelectDishAliasByNameCommand(dishTitle) ??
                       _databaseWrapper.ExecuteInsertDishAliasCommand(dishTitle,
                           (Guid) (_databaseWrapper.ExecuteSelectDishByNameCommand(dishTitle) ??
                                   _databaseWrapper.ExecuteInsertDishCommand(dishTitle)!)))!;
    }
}