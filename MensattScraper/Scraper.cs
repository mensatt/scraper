﻿using System.Diagnostics;
using System.Xml.Serialization;
using MensattScraper.DatabaseSupport;
using MensattScraper.DataIngest;
using MensattScraper.DestinationCompat;
using MensattScraper.SourceCompat;
using Microsoft.Extensions.Logging;

namespace MensattScraper;

public class Scraper : IDisposable
{
    private readonly XmlSerializer _xmlSerializer;

    private readonly IDataProvider<Speiseplan> _primaryDataProvider;

    // Used to support another language
    private readonly IDataProvider<Speiseplan> _secondaryDataProvider;

    private readonly IDatabaseWrapper _databaseWrapper;

    private Dictionary<DateOnly, List<Tuple<Guid, Guid>>>? _dailyOccurrences;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly ILogger _ownedLogger;

    public Scraper(IDatabaseWrapper databaseWrapper, IDataProvider<Speiseplan> primaryDataProvider, string identifier)
    {
        _ownedLogger = CreateSimpleLogger($"Worker-{identifier}");
        _databaseWrapper = databaseWrapper;
        _primaryDataProvider = primaryDataProvider;
        _xmlSerializer = new(typeof(Speiseplan));
        _cancellationTokenSource = new();

        var cancellationToken = _cancellationTokenSource.Token;
        cancellationToken.Register(() => _ownedLogger.LogInformation("Cancelling sleep token"));

        _secondaryDataProvider = _primaryDataProvider switch
        {
            HttpDataProvider<Speiseplan> httpDataProvider => new HttpDataProvider<Speiseplan>(
                httpDataProvider.ApiUrl.Replace("xml/", "xml/en/"), _primaryDataProvider.GetDataDelayInSeconds),
            FileDataProvider<Speiseplan> fileDataProvider => throw new("FileDataProvider is not supported"),
            _ => throw new("Unknown data provider")
        };
    }

    public void Initialize()
    {
        _databaseWrapper.ConnectAndPrepare();
        _dailyOccurrences = _databaseWrapper.ExecuteSelectOccurrenceIdNameDateCommand();
    }

    public void Scrape()
    {
        if (_dailyOccurrences is null)
            throw new NullReferenceException("_dailyOccurrences must not be null");

        var timer = new Stopwatch();

        var zippedMenus = _primaryDataProvider.RetrieveUnderlying(_xmlSerializer)
            .Zip(_secondaryDataProvider.RetrieveUnderlying(_xmlSerializer));
        foreach (var (primaryMenu, secondaryMenu) in zippedMenus)
        {
            _ownedLogger.LogInformation("Processing new menus");
            timer.Restart();

            #region Menu checks

            if (primaryMenu is null || secondaryMenu is null)
            {
                _ownedLogger.LogError(
                    $"primaryMenu is null -> {primaryMenu == null}," +
                    $" secondaryMenu is null -> {secondaryMenu == null}");
                // We cannot safely continue here, as the two streams could be out of sync.
                // In lack of a better solution, we crash the whole process
                Environment.Exit(1);
            }

            // Happens on holidays, where the xml is provided but empty
            if (primaryMenu.Tags is null || secondaryMenu.Tags is null)
            {
                _ownedLogger.LogError(
                    $"Menu days were empty, is primaryMenu.Tags null -> {primaryMenu.Tags == null}," +
                    $" is secondaryMenu.Tags null -> {secondaryMenu.Tags == null}");
                continue;
            }

            if (primaryMenu.Tags.Length != secondaryMenu.Tags.Length)
            {
                _ownedLogger.LogError(
                    "Mismatch between primary and secondary menu, primary length is " +
                    $"{primaryMenu.Tags.Length} while secondary length is {secondaryMenu.Tags.Length}");
                continue;
            }

            #endregion


            var zippedDays = primaryMenu.Tags.Zip(secondaryMenu.Tags);
            foreach (var (primaryDay, secondaryDay) in zippedDays)
            {

                #region Day and item checks

                if (primaryDay.Items is null || secondaryDay.Items is null)
                {
                    _ownedLogger.LogError(
                        $"primaryDay.Items is null -> {primaryDay.Items is null}," +
                        $" secondary.Items is null -> {secondaryDay.Items is null}");
                    continue;
                }

                if (primaryDay.Items.Length != secondaryDay.Items.Length)
                {
                    _ownedLogger.LogError(
                        "Mismatch between primary and secondary menu items length, primary length is " +
                        $"{primaryDay.Items.Length} while secondary length is {secondaryDay.Items.Length}");
                    continue;
                }

                if (primaryDay.Timestamp != secondaryDay.Timestamp)
                {
                    _ownedLogger.LogError(
                        $"Timestamp mismatch, {nameof(primaryDay.Timestamp)}={primaryDay.Timestamp} " +
                        $"and {nameof(secondaryDay.Timestamp)}={secondaryDay.Timestamp}");
                    continue;
                }

                var currentDay = Converter.GetDateFromTimestamp(primaryDay.Timestamp);

                if (DateOnly.FromDateTime(DateTime.Now) > currentDay)
                {
                    _ownedLogger.LogWarning(
                        $"Noticed menu from the past, today is {DateOnly.FromDateTime(DateTime.Now)} menu was from {currentDay}"
                    );
                    continue;
                }

                #endregion

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

                _databaseWrapper.ResetBatch();

                var zippedItems = primaryDay.Items.Zip(secondaryDay.Items);
                foreach (var (primaryItem, secondaryItem) in zippedItems)
                {
                    if (primaryItem.Title is null)
                    {
                        _ownedLogger.LogError("Primary dish title is null");
                        continue;
                    }

                    if (primaryItem != secondaryItem)
                    {
                        _ownedLogger.LogWarning($"Noticed item consistency mismatch: {primaryItem.Title} vs {secondaryItem.Title}");
                    }

                    var dishUuid = InsertDishIfNotExists(primaryItem.Title, secondaryItem.Title);

                    var occurrenceStatus =
                        firstPullOfTheDay ? OccurrenceStatus.AWAITING_APPROVAL : OccurrenceStatus.UPDATED;

                    if (!firstPullOfTheDay)
                    {
                        var savedDishOccurrence = _dailyOccurrences[currentDay].Find(x => x.Item1 == dishUuid);

                        // If we got an occurrence with this dish already, do nothing
                        if (savedDishOccurrence is not null)
                        {
                            _ownedLogger.LogInformation($"Would update {primaryItem.Title}");
                            continue; // Update in the future
                        }

                        // If it is in the far future, the old one will be replaced by this
                        if (isInFarFuture)
                            occurrenceStatus = OccurrenceStatus.AWAITING_APPROVAL;
                    }

                    var occurrenceUuid =
                        (Guid) _databaseWrapper.ExecuteInsertOccurrenceCommand(
                            DatabaseMapping.GetLocationGuidByLocationId(primaryMenu.LocationId), primaryDay,
                            primaryItem,
                            dishUuid,
                            occurrenceStatus)!;

                    _dailyOccurrences[currentDay].Add(new(dishUuid, occurrenceUuid));

                    var titleTags = Converter.ExtractSingleTagsFromTitle(primaryItem.Title);
                    var pictogramTags = Converter.ExtractTagsFromPictogram(primaryItem.Piktogramme);

                    foreach (var tag in titleTags.Concat(pictogramTags).Distinct())
                    {
                        _databaseWrapper.AddInsertOccurrenceTagCommandToBatch(occurrenceUuid, tag);
                    }

                    #region Side dish checks

                    if (primaryItem.Beilagen is null)
                        continue;

                    if (secondaryItem.Beilagen is null)
                    {
                        _ownedLogger.LogWarning("Secondary item side dish is null, but primary wasn't");
                        continue;
                    }

                    if (primaryItem.Beilagen.Length != secondaryItem.Beilagen.Length)
                    {
                        _ownedLogger.LogWarning("Side dish count mismatch");
                        continue;
                    }

                    #endregion

                    var zippedSideDishes = Converter.GetSideDishes(primaryItem.Beilagen)
                        .Zip(Converter.GetSideDishes(secondaryItem.Beilagen));
                    foreach (var (primarySideDish, secondarySideDish) in zippedSideDishes)
                    {
                        var sideDishUuid = InsertDishIfNotExists(primarySideDish, secondarySideDish);
                        _databaseWrapper.AddInsertOccurrenceSideDishCommandToBatch(occurrenceUuid, sideDishUuid);
                    }
                }

                _databaseWrapper.ExecuteBatch();
            }

            _ownedLogger.LogInformation(
                $"Scraping took {timer.ElapsedMilliseconds}ms, going to sleep");
            Thread.Sleep(TimeSpan.FromSeconds(_primaryDataProvider.GetDataDelayInSeconds));
        }
    }

    private Guid InsertDishIfNotExists(string? primaryDishTitle, string? secondaryDishTitle)
    {
        var dishAlias = _databaseWrapper.ExecuteSelectDishNormalizedAliasByNameCommand(primaryDishTitle);
        if (dishAlias is not null)
            return (Guid) dishAlias;

        var dish =
            (Guid) (_databaseWrapper.ExecuteSelectDishByGermanNameCommand(primaryDishTitle) ??
                    _databaseWrapper.ExecuteInsertDishCommand(primaryDishTitle, secondaryDishTitle)!);
        // Same as dish
        return (Guid) _databaseWrapper.ExecuteInsertDishAliasCommand(primaryDishTitle, dish)!;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _ownedLogger.LogInformation("Disposing scraper and associated data providers");
        if (_primaryDataProvider is IDisposable disposableDataProvider)
            disposableDataProvider.Dispose();

        _databaseWrapper.Dispose();
    }
}
