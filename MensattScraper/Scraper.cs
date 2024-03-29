﻿using System.Diagnostics;
using System.Xml.Serialization;
using MensattScraper.DatabaseSupport;
using MensattScraper.DataIngest;
using MensattScraper.DestinationCompat;
using MensattScraper.SourceCompat;
using MensattScraper.Telemetry;
using MensattScraper.Util;
using Microsoft.Extensions.Logging;

namespace MensattScraper;

public class Scraper : IDisposable
{
    private readonly XmlSerializer _xmlSerializer;

    private readonly IDataProvider<Speiseplan> _primaryDataProvider;

    // Used to support another language
    private readonly IDataProvider<Speiseplan> _secondaryDataProvider;

    private readonly IDatabaseWrapper _databaseWrapper;

    private Dictionary<DateOnly, List<Occurrence>>? _dailyOccurrences;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly ILogger _ownedLogger;

    private readonly WorkerTelemetry _telemetry;

    private readonly string _identifier;

    public Scraper(IDatabaseWrapper databaseWrapper, IDataProvider<Speiseplan> primaryDataProvider, string identifier)
    {
        _ownedLogger = CreateSimpleLogger($"Worker-{identifier}");
        _databaseWrapper = databaseWrapper;
        _primaryDataProvider = primaryDataProvider;
        _xmlSerializer = new(typeof(Speiseplan));
        _cancellationTokenSource = new();

        _identifier = identifier;

        _telemetry = new();

        var cancellationToken = _cancellationTokenSource.Token;
        cancellationToken.Register(() => _ownedLogger.LogInformation("Cancelling sleep token"));

        _secondaryDataProvider = _primaryDataProvider switch
        {
            HttpDataProvider<Speiseplan> httpDataProvider => new HttpDataProvider<Speiseplan>(
                httpDataProvider.ApiUrl.Replace("xml/", "xml/en/"), _primaryDataProvider.GetDataDelayInSeconds,
                Path.Combine(ContentDirectory, $"content_en_{identifier}")),
            FileDataProvider<Speiseplan> => throw new("FileDataProvider is not supported"),
            _ => throw new("Unknown data provider")
        };
    }

    public void Initialize()
    {
        _databaseWrapper.ConnectAndPrepare();
    }

    public void PrintTelemetry()
    {
        _ownedLogger.LogInformation("Telemetry for {Identifier}:", _identifier);

        _ownedLogger.LogInformation("{}", _telemetry.ToString());
    }

    public void Scrape()
    {
        var timer = new Stopwatch();

        var zippedMenus = _primaryDataProvider.RetrieveUnderlying(_xmlSerializer)
            .Zip(_secondaryDataProvider.RetrieveUnderlying(_xmlSerializer));
        foreach (var (primaryMenu, secondaryMenu) in zippedMenus)
        {
            _ownedLogger.LogInformation("Processing new menus");
            timer.Restart();

            _telemetry.TotalFetches++;

            #region Menu checks

            if (primaryMenu is null || secondaryMenu is null)
            {
                _ownedLogger.LogError(
                    "primaryMenu is null -> {PrimaryMenuIsNull}," +
                    " secondaryMenu is null -> {SecondaryMenuIsNull}", primaryMenu == null, secondaryMenu == null);
                // We cannot safely continue here, as the two streams could be out of sync.
                // In lack of a better solution, we crash the whole process
                Environment.Exit(1);
            }

            // Happens on holidays, where the xml is provided but empty
            if (primaryMenu.Tags is null || secondaryMenu.Tags is null)
            {
                _ownedLogger.LogError(
                    "Menu days were empty, is primaryMenu.Tags null -> {PrimaryMenuTagsIsNull}," +
                    " is secondaryMenu.Tags null -> {SecondaryMenuTagsIsNull}", primaryMenu.Tags == null,
                    secondaryMenu.Tags == null);
                continue;
            }

            if (primaryMenu.Tags.Length != secondaryMenu.Tags.Length)
            {
                _ownedLogger.LogError(
                    "Mismatch between primary and secondary menu, primary length is " +
                    "{PrimaryMenuTagsLength} while secondary length is {SecondaryMenuTagsLength}",
                    primaryMenu.Tags.Length, secondaryMenu.Tags.Length);
                continue;
            }

            #endregion


            // We fetch the first daily occurrences here, because we have access to this worker's location
            _dailyOccurrences ??= _databaseWrapper.RetrieveFullOccurrencesWithTags(
                DatabaseMapping.GetLocationGuidByLocationId(primaryMenu.LocationId),
                DateOnly.FromDateTime(DateTime.Now.AddDays(-14)));

            // Remove all occurrences that are longer than a week old
            var lastWeek = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));
            _dailyOccurrences.RemoveAllByKey(date => date < lastWeek);

            var zippedDays = primaryMenu.Tags.Zip(secondaryMenu.Tags);
            foreach (var (primaryDay, secondaryDay) in zippedDays)
            {
                _telemetry.TotalDays++;

                #region Day and item checks

                if (primaryDay.Items is null || secondaryDay.Items is null)
                {
                    _ownedLogger.LogError(
                        "primaryDay.Items is null -> {PrimaryDayItemsIsNull}," +
                        " secondary.Items is null -> {SecondaryDayItemsIsNull}", primaryDay.Items is null,
                        secondaryDay.Items is null);
                    continue;
                }

                if (primaryDay.Items.Length != secondaryDay.Items.Length)
                {
                    _ownedLogger.LogError(
                        "Mismatch between primary and secondary menu items length, primary length is " +
                        "{PrimaryDayItemsLength} while secondary length is {SecondaryDayItemsLength}",
                        primaryDay.Items.Length, secondaryDay.Items.Length);
                    continue;
                }

                if (primaryDay.Timestamp != secondaryDay.Timestamp)
                {
                    _ownedLogger.LogError(
                        "Timestamp mismatch, {NameOfPrimaryDayTimestamp}={PrimaryDayTimestamp} " +
                        "and {NameOfSecondaryDayTimestamp}={SecondaryDayTimestamp}", nameof(primaryDay.Timestamp),
                        primaryDay.Timestamp, nameof(secondaryDay.Timestamp), secondaryDay.Timestamp);
                    continue;
                }

                var currentDay = Converter.GetDateFromTimestamp(primaryDay.Timestamp);

                if (DateOnly.FromDateTime(DateTime.Now) > currentDay)
                {
                    _ownedLogger.LogWarning(
                        "Noticed menu from the past, today is {FromDateTime} menu was from {CurrentDay}",
                        DateOnly.FromDateTime(DateTime.Now), currentDay);
                    continue;
                }

                #endregion

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


                // This list saves all *dish* ids, which were handled for a given day and ignores them in the update
                // step below.
                // The underlying problem is the following:
                // When there are two entries in the xml, where the item's name is different but they get mapped to the
                // same dish for mensatt (so two aliases mapping to one dish), we fetch the same saved occurrence.
                // However, the item's stats (such as kcal, kj, ...) can and *do* differ, so we end in an update loop.
                // TODO: Fix this properly
                var handledDishIds = new HashSet<Guid>();

                // We save a list of the dishes here and remove every dish we encounter in the current pull below
                // Afterwards we only have the dishes left, which are no longer part of the current line up -
                // ergo we set their lastAvailableAfter to now
                var dishesOfPreviousPull = _dailyOccurrences[currentDay].Select(x => x.Dish).ToList();

                var zippedItems = primaryDay.Items.Zip(secondaryDay.Items);
                foreach (var (primaryItem, secondaryItem) in zippedItems)
                {
                    _telemetry.TotalItems++;

                    if (primaryItem.Title is null)
                    {
                        _ownedLogger.LogError("Primary dish title is null");
                        continue;
                    }

                    if (primaryItem != secondaryItem)
                    {
                        _ownedLogger.LogWarning(
                            "Noticed item consistency mismatch: {PrimaryItemTitle} vs {SecondaryItemTitle}",
                            primaryItem.Title, secondaryItem.Title);
                    }

                    var dishUuid = InsertDishIfNotExists(primaryItem.Title, secondaryItem.Title);

                    dishesOfPreviousPull.Remove(dishUuid);
                    var knownDish = handledDishIds.Add(dishUuid);

                    if (!firstPullOfTheDay)
                    {
                        var savedDishOccurrence = _dailyOccurrences[currentDay].Find(x => x.Dish == dishUuid);

                        // If we got an occurrence with this dish already, do nothing
                        if (savedDishOccurrence is not null)
                        {
                            if (!knownDish)
                            {
                                _telemetry.SkipHandledDishIdsUpdateCount++;
                                _ownedLogger.LogTrace("Skipping known dish update for {DishUuid} {DishName}", dishUuid,
                                    primaryItem.Title);
                                continue;
                            }

                            if (CompareUtil.ContentEquals(primaryItem, dishUuid, savedDishOccurrence, _ownedLogger))
                            {
                                if (!HandleTagUpdate(primaryItem, savedDishOccurrence))
                                    _telemetry.NeedsNoUpdate++;
                            }
                            else
                            {
                                _ownedLogger.LogInformation("Content update {PrimaryItemTitle} {O}", primaryItem.Title,
                                    savedDishOccurrence.Id);

                                _databaseWrapper.ExecuteUpdateOccurrenceContentsByIdCommand(savedDishOccurrence.Id,
                                    primaryItem);
                                savedDishOccurrence.ContentUpdate(primaryItem);

                                HandleTagUpdate(primaryItem, savedDishOccurrence);

                                _telemetry.ConfirmedUpdates++;
                            }

                            continue;
                        }
                    }

                    _telemetry.TotalNewOccurrenceCount++;

                    var occurrenceUuid =
                        (Guid) _databaseWrapper.ExecuteInsertOccurrenceCommand(
                            DatabaseMapping.GetLocationGuidByLocationId(primaryMenu.LocationId), primaryDay,
                            primaryItem,
                            dishUuid)!;

                    _ownedLogger.LogDebug("ADD: {OccurrenceUuid}", occurrenceUuid);

                    _dailyOccurrences[currentDay].Add(new(occurrenceUuid, dishUuid));

                    foreach (var tag in Converter.ExtractCombinedTags(primaryItem.Title, primaryItem.Piktogramme))
                    {
                        _telemetry.TotalOccurrenceTagCount++;
                        _databaseWrapper.ExecuteInsertOccurrenceTagCommand(occurrenceUuid, tag);
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
                        _telemetry.TotalSideDishCount++;
                        var sideDishUuid = InsertDishIfNotExists(primarySideDish, secondarySideDish);
                        _databaseWrapper.ExecuteInsertOccurrenceSideDishCommand(occurrenceUuid, sideDishUuid);
                    }


                }

                foreach (var dishId in dishesOfPreviousPull)
                {
                    var occ = _dailyOccurrences[currentDay].Find(x => x.Dish == dishId)!;
                    if (occ.NotAvailableAfter is not null)
                    {
                        _telemetry.TotalOccurrenceAlreadyUnavailableCount++;
                        continue;
                    }

                    _ownedLogger.LogDebug("REMOVE: {OccId}", occ.Id);
                    _telemetry.TotalOccurrenceNewUnavailableCount++;
                    occ.NotAvailableAfter = DateTime.UtcNow;
                    _databaseWrapper.ExecuteUpdateOccurrenceNotAvailableAfterByIdCommand(occ.Id, DateTime.UtcNow);
                }

            }

            _ownedLogger.LogInformation("Scraping took {TimerElapsedMilliseconds}ms, going to sleep",
                timer.ElapsedMilliseconds);
            _telemetry.AccumulatedScrapeTimeMs += (uint) timer.ElapsedMilliseconds;
            _cancellationTokenSource.Token.WaitHandle.WaitOne(
                TimeSpan.FromSeconds(_primaryDataProvider.GetDataDelayInSeconds));
        }
    }

    private bool HandleTagUpdate(Item primaryItem, Occurrence savedDishOccurrence)
    {
        var (toAdd, toRemove) = CompareUtil.TagEquals(primaryItem, savedDishOccurrence,
            _ownedLogger);
        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            return false;
        }

        // Tag update
        _ownedLogger.LogInformation("Tag update {PrimaryItemTitle}", primaryItem.Title);
        _telemetry.ConfirmedUpdates++;
        toAdd.ForEach(tag => _ownedLogger.LogTrace("Adding tag: {Tag}", tag));
        toRemove.ForEach(tag => _ownedLogger.LogTrace("Removing tag: {Tag}", tag));

        toAdd.ForEach(tag =>
            _databaseWrapper.ExecuteInsertOccurrenceTagCommand(savedDishOccurrence.Id,
                tag));
        toRemove.ForEach(tag =>
            _databaseWrapper.ExecuteDeleteOccurrenceTagByIdTagCommand(
                savedDishOccurrence.Id, tag));
        savedDishOccurrence.TagUpdate(toAdd, toRemove);
        return true;
    }

    private Guid InsertDishIfNotExists(string? primaryDishTitle, string? secondaryDishTitle)
    {
        var dishAlias = _databaseWrapper.ExecuteSelectDishNormalizedAliasByNameCommand(primaryDishTitle);
        if (dishAlias is not null)
        {
            _telemetry.TotalExistingDishAliasCount++;
            return (Guid) dishAlias;
        }

        var guid = _databaseWrapper.ExecuteSelectDishByGermanNameCommand(primaryDishTitle);
        if (guid == null)
        {
            _telemetry.TotalNewDishCount++;
            guid = _databaseWrapper.ExecuteInsertDishCommand(primaryDishTitle, secondaryDishTitle)!;
        }
        else
        {
            _telemetry.TotalFoundDishCount++;
        }

        var dish =
            (Guid) guid;
        // Same as dish
        return (Guid) _databaseWrapper.ExecuteInsertDishAliasCommand(primaryDishTitle, dish)!;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        _ownedLogger.LogInformation("Disposing scraper and associated data providers");
        if (_primaryDataProvider is IDisposable disposableDataProvider)
            disposableDataProvider.Dispose();

        _databaseWrapper.Dispose();
    }
}
