﻿using System.Diagnostics;
using System.Xml.Serialization;
using MensattScraper.DestinationCompat;
using MensattScraper.SourceCompat;

namespace MensattScraper;

public class Program
{
    private const string ApiUrl = "https://www.max-manager.de/daten-extern/sw-erlangen-nuernberg/xml/mensa-sued.xml";
    private const string DbConnection = "HOST=localhost;Port=8080;Username=mensatt;Password=mensatt;Database=mensatt";
    private const int ScrapeDelayInSeconds = 1800;

    private readonly DatabaseWrapper _databaseWrapper;

    #region Singleton Creation

    private static Program? _instance;

    private Program()
    {
        _databaseWrapper = new DatabaseWrapper(DbConnection);
    }

    private static Program Instance => _instance ??= new Program();

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
        var client = new HttpClient();
        var serializer = new XmlSerializer(typeof(Speiseplan));

        // Dict<01.01.1970, List<Dish UUID -> Occurrence UUID>>
        var dailyOccurrences = new Dictionary<DateOnly, List<Tuple<Guid, Guid>>>();

        var timer = new Stopwatch();

        foreach (var file in Directory.EnumerateFiles("."))
        {
            if (!file.EndsWith("xml"))
                continue;
            timer.Restart();
            Console.Write("Now reading " + file);

            //client.GetStreamAsync(ApiUrl).Result
            using var reader = File.OpenRead(file);

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
                var currentDay = Converter.GetDateFromTimestamp(current.Timestamp);
                var isInFarFuture = true || DateOnly.FromDateTime(DateTime.Now).AddDays(2) < currentDay;
                bool firstPullOfTheDay;
                if (!dailyOccurrences.ContainsKey(currentDay))
                {
                    dailyOccurrences.Add(currentDay, new List<Tuple<Guid, Guid>>());
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
                    // This gets shown as a placeholder, before the different kinds of pizza are known
                    if (item.Title == "Heute ab 15.30 Uhr Pizza an unserer Cafebar")
                        continue;

                    var dishUuid = _databaseWrapper.ExecuteInsertDishCommand(item.Title) ??
                                   _databaseWrapper.ExecuteSelectDishByNameCommand(item.Title);

                    dailyDishes.Add(dishUuid.Value);

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
                        (Guid) _databaseWrapper.ExecuteInsertOccurrenceCommand(current, item, dishUuid.Value,
                            occurrenceStatus);

                    dailyOccurrences[currentDay].Add(new(dishUuid.Value, occurrenceUuid));

                    foreach (var tag in Converter.ExtractSingleTagsFromTitle(item.Title))
                    {
                        _databaseWrapper.AddInsertOccurrenceTagCommandToBatch(occurrenceUuid, tag);
                    }


                    foreach (var sideDish in Converter.GetSideDishes(item.Beilagen))
                    {
                        var sideDishUuid = _databaseWrapper.ExecuteSelectDishByNameCommand(sideDish) ??
                                           _databaseWrapper.ExecuteInsertDishCommand(sideDish);
                        _databaseWrapper.AddInsertOccurrenceSideDishCommandToBatch(occurrenceUuid, sideDishUuid.Value);
                    }
                }

                _databaseWrapper.ExecuteBatch();


                // Delete all dishes, that were removed on a day which is more than two days in the future
                if (isInFarFuture)
                {
                    foreach (var previousDish in dailyOccurrences[currentDay])
                    {
                        // If this dish does not exist in the current XML, delete it
                        if (!dailyDishes.Contains(previousDish.Item1))
                        {
                            _databaseWrapper.ExecuteDeleteOccurrenceByIdCommand(previousDish.Item1);
                        }
                    }
                }
            }


            Console.WriteLine(" --> this took " + timer.ElapsedMilliseconds + "ms");
            // Thread.Sleep(ScrapeDelayInSeconds * 1000);
        }

        _databaseWrapper.Dispose();
    }
}