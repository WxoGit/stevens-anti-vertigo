using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using StevensAntiVertigo.Common.Configs;

namespace StevensAntiVertigo.Common.Parallax;

public sealed class ParallaxSyncSystem : ModSystem
{
    private static readonly List<ILHook> hooks = [];

    public override void Load()
    {
        FieldInfo? bgParallaxField = typeof(Main).GetField("bgParallax", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (bgParallaxField is null)
        {
            ModContent.GetInstance<StevensAntiVertigo.ModImpl>().Logger.Info("Failed to find field 'bgParallax' in Main class. Parallax sync will not be applied! how did this happen!?");
            return;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in typeof(Main).GetMethods(flags))
        {
            if (method.Name is "DrawBackground" or "OldDrawBackground" || method.Name.StartsWith("DrawSurfaceBG"))
                hooks.Add(new ILHook(method, il => PatchParallax(il, bgParallaxField)));
        }
    }

    public override void Unload()
    {
        // does tml unload these? idk maybe not maybe yes?
        hooks.ForEach(hook => hook?.Dispose());
        hooks.Clear();
    }

    private static void PatchParallax(ILContext il, FieldInfo bgParallaxField)
    {
        var c = new ILCursor(il);

        // pretty self explanatory
        while (c.TryGotoNext(MoveType.After, i => i.MatchLdfld(bgParallaxField)))
        {
            if (c.Next?.OpCode != OpCodes.Mul)
                continue;

            c.EmitDelegate(RemapParallax);
        }
    }

    private static double RemapParallax(double vanillaParallax)
    {
        var sync = ModContent.GetInstance<AntiVertigoConfig>().SyncFactor;

        if (sync <= 0f)
            return vanillaParallax;

        return vanillaParallax + (1.0 - vanillaParallax) * sync;
    }
}
