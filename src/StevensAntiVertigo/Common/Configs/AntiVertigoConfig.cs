using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace StevensAntiVertigo.Common.Configs;

public class AntiVertigoConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Slider]
    [Range(0, 100)]
    [Increment(1)]
    [DefaultValue(0)]
    public int ParallaxSync;

    // who is this json dude?
    [JsonIgnore]
    public float SyncFactor => ParallaxSync / 100f;
}
