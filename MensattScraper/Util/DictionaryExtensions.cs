namespace MensattScraper.Util;

public static class DictionaryExtensions
{
    public static void RemoveAllByKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
        Func<TKey, bool> predicate)
    {
        foreach (var (key, _) in dictionary.Where(tuple => predicate(tuple.Key)).ToList())
            dictionary.Remove(key);
    }
}