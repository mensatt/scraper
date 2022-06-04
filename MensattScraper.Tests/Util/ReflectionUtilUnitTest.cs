using System.Reflection;
using MensattScraper.Util;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable ConvertToConstant.Local

// ReSharper disable PossibleMultipleEnumeration

namespace MensattScraper.Tests.Util;

public class ReflectionUtilUnitTest
{
    private readonly string _stringField1 = "0";
    private readonly string _stringField2 = "1";
    private readonly string _stringField3 = "2";

    public readonly object ObjectField1 = new();
    public readonly object ObjectField2 = new();

    private static readonly Exception StaticExceptionField1 = new();
    private static readonly Exception StaticExceptionField2 = new();
    private static readonly Exception StaticExceptionField3 = new();
    private static readonly Exception StaticExceptionField4 = new();

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
            {StaticExceptionField1, StaticExceptionField2, StaticExceptionField3, StaticExceptionField4}));
    }
}