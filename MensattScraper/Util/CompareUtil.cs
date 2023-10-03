using System.Diagnostics.Metrics;
using MensattScraper.DestinationCompat;
using MensattScraper.SourceCompat;
using Microsoft.Extensions.Logging;

namespace MensattScraper.Util;

public static class CompareUtil
{
    public static bool ContentEquals(Item i, Guid itemDish, Occurrence o, ILogger? logger = null)
    {
        if (itemDish != o.Dish)
        {
            logger?.LogTrace("{ItemDishName} != {ODishName}: {ItemDish} != {ODish}", nameof(itemDish), nameof(o.Dish),
                itemDish, o.Dish);
            return false;
        }

        if (Converter.FloatStringToInt(i.Preis1) != o.PriceStudent)
        {
            logger?.LogTrace("{ItemPriceStudentName} != {OPriceStudentName}: {ItemPriceStudent} != {OPriceStudent}",
                nameof(i.Preis1), nameof(o.PriceStudent), Converter.FloatStringToInt(i.Preis1), o.PriceStudent);
            return false;
        }

        if (Converter.FloatStringToInt(i.Preis2) != o.PriceStaff)
        {
            logger?.LogTrace("{ItemPriceStaffName} != {OPriceStaffName}: {ItemPriceStaff} != {OPriceStaff}",
                nameof(i.Preis2), nameof(o.PriceStaff), Converter.FloatStringToInt(i.Preis2), o.PriceStaff);
            return false;
        }

        if (Converter.FloatStringToInt(i.Preis3) != o.PriceGuest)
        {
            logger?.LogTrace("{ItemPriceGuestName} != {OPriceGuestName}: {ItemPriceGuest} != {OPriceGuest}",
                nameof(i.Preis3), nameof(o.PriceGuest), Converter.FloatStringToInt(i.Preis3), o.PriceGuest);
            return false;
        }

        if (Converter.BigFloatStringToInt(i.Kj) != o.Kj)
        {
            logger?.LogTrace("{ItemKjName} != {OKjName}: {ItemKj} != {OKj}", nameof(i.Kj), nameof(o.Kj),
                Converter.BigFloatStringToInt(i.Kj), o.Kj);
            return false;
        }

        if (Converter.BigFloatStringToInt(i.Kcal) != o.Kcal)
        {
            logger?.LogTrace("{ItemKcalName} != {OKcalName}: {ItemKcal} != {OKcal}", nameof(i.Kcal), nameof(o.Kcal),
                Converter.BigFloatStringToInt(i.Kcal), o.Kcal);
            return false;
        }

        if (Converter.FloatStringToInt(i.Fett) != o.Fat)
        {
            logger?.LogTrace("{ItemFatName} != {OFatName}: {ItemFat} != {OFat}", nameof(i.Fett), nameof(o.Fat),
                Converter.FloatStringToInt(i.Fett), o.Fat);
            return false;
        }

        if (Converter.FloatStringToInt(i.Gesfett) != o.SaturatedFat)
        {
            logger?.LogTrace("{ItemSaturatedFatName} != {OSaturatedFatName}: {ItemSaturatedFat} != {OSaturatedFat}",
                nameof(i.Gesfett), nameof(o.SaturatedFat), Converter.FloatStringToInt(i.Gesfett), o.SaturatedFat);
            return false;
        }

        if (Converter.FloatStringToInt(i.Kh) != o.Carbohydrates)
        {
            logger?.LogTrace("{ItemCarbohydratesName} != {OCarbohydratesName}: {ItemCarbohydrates} != {OCarbohydrates}",
                nameof(i.Kh), nameof(o.Carbohydrates), Converter.FloatStringToInt(i.Kh), o.Carbohydrates);
            return false;
        }

        if (Converter.FloatStringToInt(i.Zucker) != o.Sugar)
        {
            logger?.LogTrace("{ItemSugarName} != {OSugarName}: {ItemSugar} != {OSugar}", nameof(i.Zucker),
                nameof(o.Sugar), Converter.FloatStringToInt(i.Zucker), o.Sugar);
            return false;
        }

        if (Converter.FloatStringToInt(i.Ballaststoffe) != o.Fiber)
        {
            logger?.LogTrace("{ItemFiberName} != {OFiberName}: {ItemFiber} != {OFiber}", nameof(i.Ballaststoffe),
                nameof(o.Fiber), Converter.FloatStringToInt(i.Ballaststoffe), o.Fiber);
            return false;
        }

        if (Converter.FloatStringToInt(i.Eiweiss) != o.Protein)
        {
            logger?.LogTrace("{ItemProteinName} != {OProteinName}: {ItemProtein} != {OProtein}", nameof(i.Eiweiss),
                nameof(o.Protein), Converter.FloatStringToInt(i.Eiweiss), o.Protein);
            return false;
        }

        if (Converter.FloatStringToInt(i.Salz) != o.Salt)
        {
            logger?.LogTrace("{ItemSaltName} != {OSaltName}: {ItemSalt} != {OSalt}", nameof(i.Salz), nameof(o.Salt),
                Converter.FloatStringToInt(i.Salz), o.Salt);
            return false;
        }

        return true;
    }

    public static (List<string> toAdd, List<string> toRemove) TagEquals(Item i, Occurrence o, ILogger? logger = null)
    {
        var itemTags = Converter.ExtractCombinedTags(i.Title, i.Piktogramme).ToList();
        var occurrenceTags = o.Tags ?? new List<string>();
        occurrenceTags = occurrenceTags.Select(Converter.NormalizeTag).ToList();

        var toAdd = itemTags.Except(occurrenceTags).ToList();
        var toRemove = occurrenceTags.Except(itemTags).ToList();

        if (toAdd.Count != 0)
            logger?.LogTrace("toAdd: {ToAdd}", string.Join(", ", toAdd));
        if (toRemove.Count != 0)
            logger?.LogTrace("toRemove: {ToRemove}", string.Join(", ", toRemove));

        return (toAdd, toRemove);
    }
}
