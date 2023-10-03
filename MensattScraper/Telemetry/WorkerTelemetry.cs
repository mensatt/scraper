namespace MensattScraper.Telemetry;

internal class WorkerTelemetry
{
    internal uint TotalFetches { get; set; }
    internal uint TotalDays { get; set; }
    internal uint TotalItems { get; set; }
    internal uint PotentialUpdates { get; set; }
    internal uint NeedsNoUpdate { get; set; }
    internal uint TotalSideDishCount { get; set; }
    internal uint TotalExistingDishAliasCount { get; set; }
    internal uint TotalFoundDishCount { get; set; }
    internal uint TotalNewDishCount { get; set; }
    internal uint TotalNewOccurrenceCount { get; set; }
    internal uint TotalOccurrenceTagCount { get; set; }
    internal uint AccumulatedScrapeTimeMs { get; set; }

    internal uint TotalOccurrenceAlreadyUnavailableCount { get; set; }
    internal uint TotalOccurrenceNewUnavailableCount { get; set; }

    public override string ToString()
    {
        return
            $"{nameof(TotalFetches)}: {TotalFetches}, {nameof(TotalDays)}: {TotalDays}, {nameof(TotalItems)}: {TotalItems}, {nameof(PotentialUpdates)}: {PotentialUpdates}, {nameof(NeedsNoUpdate)}: {NeedsNoUpdate}, {nameof(TotalSideDishCount)}: {TotalSideDishCount}, {nameof(TotalExistingDishAliasCount)}: {TotalExistingDishAliasCount}, {nameof(TotalFoundDishCount)}: {TotalFoundDishCount}, {nameof(TotalNewDishCount)}: {TotalNewDishCount}, {nameof(TotalNewOccurrenceCount)}: {TotalNewOccurrenceCount}, {nameof(TotalOccurrenceTagCount)}: {TotalOccurrenceTagCount}, {nameof(AccumulatedScrapeTimeMs)}: {AccumulatedScrapeTimeMs}, {nameof(TotalOccurrenceAlreadyUnavailableCount)}: {TotalOccurrenceAlreadyUnavailableCount}, {nameof(TotalOccurrenceNewUnavailableCount)}: {TotalOccurrenceNewUnavailableCount}";
    }
}
