using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StevensAntiVertigo.Common.Configs;
using StevensAntiVertigo.Core;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Graphics.Capture;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace StevensAntiVertigo.Common.Brightness;

public sealed class ColorAdjustSystem : ModSystem
{
    private static readonly FieldInfo captureField = typeof(FilterManager).GetField("_captureThisFrame", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static RenderTarget2D? captureTarget;
    private static bool drawingCapture;
    private static bool forcingCapture;

    public override void Load()
    {
        On_FilterManager.CanCapture += OnCanCapture;
        On_FilterManager.EndCapture += OnEndCapture;
        On_Main.DrawCapture += SkipDuringCapture;
        On_ScreenObstruction.Draw += DrawRetroFallback;
    }

    public override void Unload()
    {
        if (Filters.Scene is not null && forcingCapture)
            Filters.Scene.OnPostDraw -= KeepCapture;

        forcingCapture = false;

        RenderTarget2D? targetToDispose = captureTarget;
        captureTarget = null;
        if (targetToDispose is not null)
            Main.QueueMainThreadAction(() => targetToDispose.Dispose());
    }

    private static void KeepCapture()
    {
    }

    private static void SyncCapture(FilterManager self, bool want)
    {
        if (want == forcingCapture)
            return;

        if (want)
            self.OnPostDraw += KeepCapture;
        else
            self.OnPostDraw -= KeepCapture;

        forcingCapture = want;
    }

    private static bool OnCanCapture(On_FilterManager.orig_CanCapture orig, FilterManager self)
    {
        var config = ModContent.GetInstance<AntiVertigoConfig>();
        SyncCapture(self, !drawingCapture && config is not null && config.IsColorAdjustActive);
        return orig(self);
    }

    private static RenderTarget2D EnsureCaptureTarget(GraphicsDevice device, int width, int height)
    {
        if (captureTarget is null || captureTarget.IsDisposed || captureTarget.Width != width || captureTarget.Height != height)
        {
            captureTarget?.Dispose();
            captureTarget = new RenderTarget2D(device, width, height, false, device.PresentationParameters.BackBufferFormat, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        return captureTarget;
    }

    private static void OnEndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
    {
        var config = ModContent.GetInstance<AntiVertigoConfig>();
        if (drawingCapture || config is null || !config.IsColorAdjustActive || !ReferenceEquals(screenTarget1, Main.screenTarget) || !(bool)captureField.GetValue(self)!)
        {
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
            return;
        }

        var device = Main.graphics.GraphicsDevice;
        RenderTarget2D captured = EnsureCaptureTarget(device, screenTarget1.Width, screenTarget1.Height);
        orig(self, captured, screenTarget1, screenTarget2, clearColor);

        Effect shader = AssetReferences.Assets.Shaders.ColorAdjust.Asset.Value;
        shader.Parameters["brightness"]?.SetValue(config.BrightnessFactor);
        shader.Parameters["contrast"]?.SetValue(config.ContrastFactor);
        shader.Parameters["gamma"]?.SetValue(config.GammaFactor);
        shader.Parameters["vibrance"]?.SetValue(config.VibranceFactor);

        device.SetRenderTarget(finalTexture);
        device.Clear(clearColor);

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        shader.CurrentTechnique.Passes[0].Apply();
        Main.spriteBatch.Draw(captured, Vector2.Zero, Color.White);
        Main.spriteBatch.End();
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
