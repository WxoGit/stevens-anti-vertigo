using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace StevensAntiVertigo.Common.Configs;

public class AntiVertigoConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Parallax")]
    [Slider]
    [Range(0, 100)]
    [Increment(1)]
    [DefaultValue(0)]
    public int ParallaxSync;

    [JsonIgnore]
    public float SyncFactor => ParallaxSync / 100f;

    [Header("ColorAdjust")]
    [Slider]
    [Range(0, 100)]
    [Increment(1)]
    [DefaultValue(50)]
    public int Brightness;

    [Slider]
    [Range(0, 100)]
    [Increment(1)]
    [DefaultValue(50)]
    public int Contrast;

    [Slider]
    [Range(40, 250)]
    [Increment(1)]
    [DefaultValue(100)]
    public int Gamma;

    [Slider]
    [Range(0, 100)]
    [Increment(1)]
    [DefaultValue(50)]
    public int DigitalVibrance;

    [JsonIgnore]
    public bool IsColorAdjustActive => Brightness != 50 || Contrast != 50 || Gamma != 100 || DigitalVibrance != 50;

    [JsonIgnore]
    public float BrightnessFactor => Brightness / 100f;

    [JsonIgnore]
    public float ContrastFactor => Contrast / 100f;

    [JsonIgnore]
    public float GammaFactor => Gamma / 100f;

    [JsonIgnore]
    public float VibranceFactor => DigitalVibrance / 100f;
}
