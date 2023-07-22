// ReSharper disable InconsistentNaming
// Required for proper mapping with the values in the database.

namespace MensattScraper.DestinationCompat;

/// <summary>
/// As the new database schema created by ent go does not provide
/// the occurrence status as a postgres enum type (but a varchar instead)
/// this enum is no longer used for destination compatibility."
/// </summary>
public enum OccurrenceStatus
{
    APPROVED,
    AWAITING_APPROVAL,
    UPDATED,
    PENDING_DELETION
}
