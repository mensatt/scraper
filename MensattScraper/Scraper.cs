using System.Diagnostics;
using System.Xml.Serialization;
using FuzzySharp;
using MensattScraper.DatabaseSupport;
using MensattScraper.DataIngest;
using MensattScraper.DestinationCompat;
using MensattScraper.Discord;
using MensattScraper.SourceCompat;
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

    private Dictionary<DateOnly, List<Tuple<Guid, Guid>>>? _dailyOccurrences;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CancellationToken _cancellationToken;

    private readonly DiscordIntegration _discordIntegration;

    public Scraper(IDatabaseWrapper databaseWrapper, IDataProvider<Speiseplan> primaryDataProvider)
    {
        _databaseWrapper = databaseWrapper;
        _primaryDataProvider = primaryDataProvider;
        _xmlSerializer = new(typeof(Speiseplan));
        _cancellationTokenSource = new();
        _cancellationToken = _cancellationTokenSource.Token;
        _cancellationToken.Register(() => SharedLogger.LogInformation("Cancelling sleep token"));

        _secondaryDataProvider = _primaryDataProvider switch
        {
            HttpDataProvider<Speiseplan> httpDataProvider => new HttpDataProvider<Speiseplan>(
                httpDataProvider.ApiUrl.Replace("xml/", "xml/en/")),
            // TODO: Document this
            FileDataProvider<Speiseplan> fileDataProvider => new FileDataProvider<Speiseplan>(fileDataProvider.Path +
                "_en"),
            _ => throw new("Unknown data provider")
        };

        _discordIntegration = new();
        _discordIntegration.StartIntegration();
        _discordIntegration.MessageInteractionResponseEvent += DiscordIntegrationOnMessageInteractionResponseEvent;
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
            SharedLogger.LogInformation("Processing new menus");
            timer.Restart();

            // Free up entries that are more than 3 days old
            _dailyOccurrences.RemoveAllByKey(key => key < DateOnly.FromDateTime(DateTime.Today).AddDays(-3));

            // TODO: Evaluate the error handling should be extracted into it's own method
            if (primaryMenu is null || secondaryMenu is null)
            {
                SharedLogger.LogError(
                    $"primaryMenu is null -> {primaryMenu == null}," +
                    $" secondaryMenu is null -> {secondaryMenu == null}");
                continue;
            }

            // Happens on holidays, where the xml is provided but empty
            if (primaryMenu.Tags is null || secondaryMenu.Tags is null)
            {
                SharedLogger.LogError(
                    $"Menu days were empty, is primaryMenu.Tags null -> {primaryMenu.Tags == null}," +
                    $" is secondaryMenu.Tags null -> {secondaryMenu.Tags == null}");
                continue;
            }

            if (primaryMenu.Tags.Length != secondaryMenu.Tags.Length)
            {
                SharedLogger.LogError(
                    "Mismatch between primary and secondary menu, primary length is " +
                    $"{primaryMenu.Tags.Length} while secondary length is {secondaryMenu.Tags.Length}");
                continue;
            }

            var zippedDays = primaryMenu.Tags.Zip(secondaryMenu.Tags);
            foreach (var (primaryDay, secondaryDay) in zippedDays)
            {
                if (primaryDay.Items is null || secondaryDay.Items is null)
                {
                    SharedLogger.LogError(
                        $"primaryDay.Items is null -> {primaryDay.Items is null}," +
                        $" secondary.Items is null -> {secondaryDay.Items is null}");
                    continue;
                }

                if (primaryDay.Items.Length != secondaryDay.Items.Length)
                {
                    SharedLogger.LogError(
                        "Mismatch between primary and secondary menu items length, primary length is " +
                        $"{primaryDay.Items.Length} while secondary length is {secondaryDay.Items.Length}");
                    continue;
                }

                if (primaryDay.Timestamp != secondaryDay.Timestamp)
                {
                    SharedLogger.LogError(
                        $"Timestamp mismatch, {nameof(primaryDay.Timestamp)}={primaryDay.Timestamp} " +
                        $"and {nameof(secondaryDay.Timestamp)}={secondaryDay.Timestamp}");
                    continue;
                }

                var currentDay = Converter.GetDateFromTimestamp(primaryDay.Timestamp);
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

                var zippedItems = primaryDay.Items.Zip(secondaryDay.Items);
                foreach (var (primaryItem, secondaryItem) in zippedItems)
                {
                    if (primaryItem.Title is null)
                    {
                        SharedLogger.LogError("Primary dish title is null");
                        continue;
                    }

                    // TODO: Check item consistency, override ==

                    // This gets shown as a placeholder, before the different kinds of pizza are known
                    if (primaryItem.Title == "Heute ab 15.30 Uhr Pizza an unserer Cafebar")
                    {
                        SharedLogger.LogWarning($"Noticed placeholder item, skipping {primaryItem.Title}");
                        continue;
                    }

                    var dishUuid = InsertDishIfNotExists(primaryItem.Title, secondaryItem.Title,
                        out var fuzzyCheckRequired, out var transferData);
                    dailyDishes.Add(dishUuid);

                    var occurrenceStatus =
                        firstPullOfTheDay ? OccurrenceStatus.AWAITING_APPROVAL : OccurrenceStatus.UPDATED;

                    if (!firstPullOfTheDay)
                    {
                        var savedDishOccurrence = _dailyOccurrences[currentDay].Find(x => x.Item1 == dishUuid);

                        // If we got an occurrence with this dish already, do nothing
                        if (savedDishOccurrence is not null)
                            continue; // Update in the future

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

                    // If this is true, this dish was not encountered before.
                    // Thus we fuzzy search for matches and start the manual review process.
                    if (fuzzyCheckRequired)
                    {
                        var extractedTitle =
                            Converter.ExtractElementFromTitle(primaryItem.Title, Converter.TitleElement.Name);

                        transferData!.Occurrence = occurrenceUuid;
                        transferData.FullDishTitle = extractedTitle;
                        transferData.SanitizedDishTitle = Converter.SanitizeString(extractedTitle);
                        _discordIntegration.SendNewDish(transferData);
                    }

                    _dailyOccurrences[currentDay].Add(new(dishUuid, occurrenceUuid));

                    foreach (var tag in Converter.ExtractSingleTagsFromTitle(primaryItem.Title))
                    {
                        _databaseWrapper.AddInsertOccurrenceTagCommandToBatch(occurrenceUuid, tag);
                    }

                    if (primaryItem.Beilagen is null)
                        continue;

                    if (secondaryItem.Beilagen is null)
                    {
                        SharedLogger.LogWarning("Secondary item side dish is null, but primary wasn't");
                    }

                    if (primaryItem.Beilagen.Length != secondaryItem.Beilagen.Length)
                    {
                        SharedLogger.LogWarning("Side dish count mismatch");
                    }

                    var zippedSideDishes = Converter.GetSideDishes(primaryItem.Beilagen)
                        .Zip(Converter.GetSideDishes(secondaryItem.Beilagen));
                    foreach (var (primarySideDish, secondarySideDish) in zippedSideDishes)
                    {
                        var sideDishUuid =
                            InsertDishIfNotExists(primarySideDish, secondarySideDish, out _, out _);
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
                        SharedLogger.LogInformation(
                            $"Noticed dish removal of {dishId}, isFarInTheFuture={isInFarFuture}");
                        if (isInFarFuture)
                            _databaseWrapper.ExecuteDeleteOccurrenceByIdCommand(occurrenceId);
                        else
                            _databaseWrapper.ExecuteUpdateOccurrenceReviewStatusByIdCommand(
                                OccurrenceStatus.PENDING_DELETION, occurrenceId);
                    }
                }
            }

            SharedLogger.LogInformation($"Scraping took {timer.ElapsedMilliseconds}ms, going to sleep");

            _cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(_primaryDataProvider.GetDataDelayInSeconds));
        }
    }


    // TODO: Log all relevant data to prevent deleted dish_alias occurrence combinations
    private Guid InsertDishIfNotExists(string? primaryDishTitle, string? secondaryDishTitle, out bool fuzzyCheck,
        out TransferData? transferData)
    {
        transferData = null;
        fuzzyCheck = false;
        List<FuzzyResult> results = new();

        var extracted = Converter.ExtractElementFromTitle(primaryDishTitle, Converter.TitleElement.Name);

        var guid = _databaseWrapper.ExecuteSelectDishAliasByNameCommand(primaryDishTitle);
        if (guid == null)
        {
            var executeSelectDishByGermanNameCommand =
                _databaseWrapper.ExecuteSelectDishByGermanNameCommand(primaryDishTitle);
            if (executeSelectDishByGermanNameCommand == null)
            {
                // If we reach this part, that means that this dish was not encountered before
                var aliases = _databaseWrapper.ExecuteSelectDishAliasIdNameDeAllCommand();
                results = new();
                foreach (var (normalizedDishName, dish) in aliases)
                {
                    results.Add(new(Fuzz.WeightedRatio(Converter.SanitizeString(extracted), normalizedDishName),
                        dish,
                        normalizedDishName));
                }

                results.Sort();

                if (results.Count != 0 && results.First().Score >= 50)
                    fuzzyCheck = true;

                executeSelectDishByGermanNameCommand =
                    _databaseWrapper.ExecuteInsertDishCommand(primaryDishTitle, secondaryDishTitle)!;
            }

            guid = _databaseWrapper.ExecuteInsertDishAliasCommand(primaryDishTitle,
                (Guid) executeSelectDishByGermanNameCommand);

            if (fuzzyCheck)
                transferData = new(executeSelectDishByGermanNameCommand.Value,
                    extracted, results.Take(3).ToList());
        }

        return (Guid) guid!;
    }

    private void DiscordIntegrationOnMessageInteractionResponseEvent(object? sender,
        MessageInteractionResponseEventArgs e)
    {
        switch (e.Type)
        {
            case MessageInteractionResponseType.AcceptFirst or MessageInteractionResponseType.AcceptSecond
                or MessageInteractionResponseType.AcceptThird:
                _databaseWrapper.ExecuteUpdateOccurrenceDishByIdCommand(e.TransferData.Results[(int) e.Type].Dish,
                    e.TransferData.Occurrence);
                _databaseWrapper.ExecuteUpdateDishAliasDishByAliasNameCommand(e.TransferData.Results[(int) e.Type].Dish,
                    e.TransferData.DishAlias);
                _databaseWrapper.ExecuteDeleteDishByIdCommand(e.TransferData.CreatedDishId);
                break;
            case MessageInteractionResponseType.InsertNew:
                return;
            case MessageInteractionResponseType.DiscardAll:
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        SharedLogger.LogInformation("Disposing scraper and associated data providers");
        if (_primaryDataProvider is IDisposable disposableDataProvider)
            disposableDataProvider.Dispose();

        _databaseWrapper.Dispose();
    }
}