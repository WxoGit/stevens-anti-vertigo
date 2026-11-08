using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StevensAntiVertigo.Common.Configs;
using StevensAntiVertigo.Core;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Graphics.Capture;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace StevensAntiVertigo.Common.Brightness;

public sealed class ColorAdjustSystem : ModSystem
{
    private const string FilterKey = "StevensAntiVertigo:ColorAdjust";
    private static bool drawingCapture;

    public override void Load()
    {
        Filters.Scene[FilterKey] = new Filter(new ScreenShaderData(AssetReferences.Assets.Shaders.ColorAdjust.Asset, "p0"), EffectPriority.VeryHigh);
        On_Main.DrawCapture += SkipDuringCapture;
        On_ScreenObstruction.Draw += DrawRetroFallback;
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;

        Filter? filter = Filters.Scene[FilterKey];
        if (filter is null)
            return;

        var config = ModContent.GetInstance<AntiVertigoConfig>();
        if (drawingCapture || !config.IsColorAdjustActive)
        {
            if (filter.IsActive())
                filter.Deactivate();

            return;
        }

        if (!filter.IsActive())
            Filters.Scene.Activate(FilterKey);

        filter.Opacity = 1f;
        filter.IsHidden = false;

        ScreenShaderData shader = filter.GetShader();
        shader.UseOpacity(1f);
        shader.Shader.Parameters["brightness"]?.SetValue(config.BrightnessFactor);
        shader.Shader.Parameters["contrast"]?.SetValue(config.ContrastFactor);
        shader.Shader.Parameters["gamma"]?.SetValue(config.GammaFactor);
        shader.Shader.Parameters["vibrance"]?.SetValue(config.VibranceFactor);
    }

    private static void SkipDuringCapture(On_Main.orig_DrawCapture orig, Main self, Rectangle area, CaptureSettings settings)
    {
        drawingCapture = true;

        try
        {
            orig(self, area, settings);
        }
        finally
        {
            drawingCapture = false;
        }
    }

    private static void DrawRetroFallback(On_ScreenObstruction.orig_Draw orig, SpriteBatch spriteBatch)
    {
        orig(spriteBatch);

        if (drawingCapture || Lighting.NotRetro)
            return;

        var config = ModContent.GetInstance<AntiVertigoConfig>();
        if (config.Brightness >= 50)
            return;

        float dimFactor = config.Brightness / 50f;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4), new Rectangle(0, 0, 1, 1), Color.Black * (1f - dimFactor));
    }
}
