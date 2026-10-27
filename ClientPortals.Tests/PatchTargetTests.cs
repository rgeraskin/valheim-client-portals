using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ClientPortals;
using Xunit;

/// <summary>Every Harmony patch must find its method in the game build the plugin compiles against; one miss stops the whole plugin from loading.</summary>
public class PatchTargetTests
{
    public static IEnumerable<object[]> Patches()
    {
        foreach (var type in typeof(Plugin).Assembly.GetTypes())
        {
            var classInfo = type.GetCustomAttributes<HarmonyPatch>().Select(a => a.info).ToList();
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HarmonyPatch>().Any() || m.Name is "Prefix" or "Postfix")
                .ToList();
            foreach (var m in methods)
            {
                var info = new HarmonyMethod();
                foreach (var c in classInfo) Merge(info, c);
                foreach (var a in m.GetCustomAttributes<HarmonyPatch>()) Merge(info, a.info);
                yield return new object[] { $"{type.Name}.{m.Name}", info.declaringType, info.methodName, info.argumentTypes };
            }
        }
    }

    static void Merge(HarmonyMethod into, HarmonyMethod from)
    {
        into.declaringType = from.declaringType ?? into.declaringType;
        into.methodName = from.methodName ?? into.methodName;
        into.argumentTypes = from.argumentTypes ?? into.argumentTypes;
    }

    [Theory]
    [MemberData(nameof(Patches))]
    public void TargetExists(string patch, Type type, string method, Type[] args)
    {
        Assert.NotNull(type);
        Assert.NotNull(method);
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        if (args != null)
            Assert.True(type.GetMethod(method, all, null, args, null) != null, $"{patch}: {type.Name}.{method}({string.Join(", ", args.Select(a => a.Name))}) not found");
        else
            Assert.True(type.GetMethods(all).Count(m => m.Name == method) == 1, $"{patch}: {type.Name}.{method} not found or overloaded (give the argument types)");
    }

    [Fact]
    public void AllPatchClassesAreFound() => Assert.True(Patches().Count() >= 10);
}
