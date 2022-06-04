using System.Reflection;
using MensattScraper.Util;

// ReSharper disable PossibleMultipleEnumeration

namespace MensattScraper.Tests.Util;

public class ReflectionUtilUnitTest
{
    private readonly string _stringField1 = "0";
    private readonly string _stringField2 = "1";
    private readonly string _stringField3 = "2";

    public object ObjectField1 = new();
    public object ObjectField2 = new();

    private static readonly Exception _staticExceptionField1 = new();
    private static readonly Exception _staticExceptionField2 = new();
    private static readonly Exception _staticExceptionField3 = new();
    private static readonly Exception _staticExceptionField4 = new();

    [Fact]
    public void ZeroPrivateInstanceReflectionUtilUnitTestFields()
    {
        var fields =
            ReflectionUtil.GetFieldValuesWithType<ReflectionUtilUnitTest>(typeof(ReflectionUtilUnitTest), this);

        Assert.Empty(fields);
    }

    [Fact]
    public void ThreePrivateInstanceStringFields()
    {
        var fields = ReflectionUtil.GetFieldValuesWithType<string>(typeof(ReflectionUtilUnitTest), this);

        Assert.True(fields.SequenceEqual(new[] {_stringField1, _stringField2, _stringField3}));
    }


    [Fact]
    public void ZeroNonPrivateInstanceObjectFields()
    {
        var privateInstanceFields = ReflectionUtil.GetFieldValuesWithType<object>(typeof(ReflectionUtilUnitTest), this);
        var privateStaticFields = ReflectionUtil.GetFieldValuesWithType<object>(typeof(ReflectionUtilUnitTest), this,
            BindingFlags.NonPublic | BindingFlags.Static);
        var publicStaticFields =
            ReflectionUtil.GetFieldValuesWithType<object>(typeof(ReflectionUtilUnitTest), this,
                BindingFlags.Public | BindingFlags.Static);

        Assert.Empty(privateInstanceFields);
        Assert.Empty(privateStaticFields);
        Assert.Empty(publicStaticFields);
    }

    [Fact]
    public void TwoPublicInstanceObjectFields()
    {
        var fields = ReflectionUtil.GetFieldValuesWithType<object>(typeof(ReflectionUtilUnitTest), this,
            BindingFlags.Instance | BindingFlags.Public);

        Assert.True(fields.SequenceEqual(new[] {ObjectField1, ObjectField2}));
    }

    [Fact]
    public void FourPrivateStaticExceptionFields()
    {
        var fields = ReflectionUtil.GetFieldValuesWithType<Exception>(typeof(ReflectionUtilUnitTest), this,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.True(fields.SequenceEqual(new[]
            {_staticExceptionField1, _staticExceptionField2, _staticExceptionField3, _staticExceptionField4}));
    }
}