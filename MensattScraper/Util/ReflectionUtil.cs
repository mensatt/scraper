using System.Reflection;

namespace MensattScraper.Util;

public static class ReflectionUtil
{
    public static IEnumerable<T> GetFieldValuesWithType<T>(Type type, object callee,
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance) where T : class
    {
        foreach (var fieldInfo in type.GetFields(flags))
        {
            if (fieldInfo.FieldType != typeof(T)) continue;

            yield return fieldInfo.GetValue(callee) as T ?? throw new NullReferenceException("Field value was null");
        }
    }
}