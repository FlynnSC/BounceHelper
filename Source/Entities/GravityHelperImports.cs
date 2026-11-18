using System;
using MonoMod.ModInterop;

namespace Celeste.Mod.BounceHelper;

[ModImportName("GravityHelper")]
public static class GravityHelperImports
{
    public static Action<string> RegisterModSupportBlacklist;

    public static Func<bool> IsPlayerInverted;

    public static void Initialize()
    {
        typeof(GravityHelperImports).ModInterop();
        RegisterModSupportBlacklist?.Invoke("BounceHelper");
    }
}