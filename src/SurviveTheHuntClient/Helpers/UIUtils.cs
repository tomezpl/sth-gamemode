using CitizenFX.Core;
using SurviveTheHuntClient.Models.UI;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntClient.Helpers.UIUtils;

namespace SurviveTheHuntClient.Helpers
{
    internal static class UIUtils
    {
        internal struct Rect
        {
            internal float X;
            internal float Y;
            internal float Width;
            internal float Height;
        }

        internal const float DefaultAlphaBarVerticalOrigin = 0.855f;
        internal const float DefaultAlphaBarHorizOrigin = 0.94f;
        internal const float DefaultAlphaBarSpritePadding = -0.006f;
        internal const float DefaultAlphaBarTitleOffset = -0.01f;
        internal const float DefaultAlphaBarValueOffset = -0.02f;
        internal const float DefaultAlphaBarHeight = 0.06f * 0.5f * 1.4f;
        internal const float DefaultAlphaBarGap = 0.012f;

        private static class TextWidthCache
        {
            internal readonly static Dictionary<string, float> Counters = new Dictionary<string, float>();
            internal readonly static Dictionary<string, float> ProgressBars = new Dictionary<string, float>();
        }

        internal static float GetWidthAdjustedForTimerText(in float initialWidth, in string text)
        {
            // Treat negative width as auto-width
            if (initialWidth < 0f)
            {
                if(TextWidthCache.Counters.TryGetValue(text, out float cachedWidth))
                {
                    return cachedWidth;
                }

                // Get rect width to fit the text.
                SetTextScale(0f, 0.55f);
                BeginTextCommandWidth("STRING");
                AddTextComponentString($"{text}  00:00");
                cachedWidth = EndTextCommandGetWidth(true);
                TextWidthCache.Counters.Add(text, cachedWidth);

                return cachedWidth;
            }

            return initialWidth;
        }

        internal static float GetWidthAdjustedForProgressBar(in float initialWidth, in string label)
        {
            // Treat negative width as auto-width
            if (initialWidth < 0f)
            {
                if (TextWidthCache.ProgressBars.TryGetValue(label, out float cachedWidth))
                {
                    return cachedWidth;
                }

                // Get rect width to fit the text.
                SetTextScale(0f, 0.55f);
                BeginTextCommandWidth("STRING");
                AddTextComponentString($"{label}  PROGRESS");
                cachedWidth = EndTextCommandGetWidth(true);
                TextWidthCache.ProgressBars.Add(label, cachedWidth);

                return cachedWidth;
            }

            return initialWidth;
        }

        internal static void DrawTimer(in string label, in string value, in float x, in float y, in float width, in float height, in uint colour = uint.MaxValue, in float contentXOffset = 0f, in float spritePadding = DefaultAlphaBarSpritePadding)
        {
            float adjustedWidth = GetWidthAdjustedForTimerText(in width, in label);

            float safeX = GetSafeX(in x, in adjustedWidth);

            DrawAlphaBarBackground(in safeX, in y, in adjustedWidth, in height, in spritePadding);

            safeX += 0.02f;

            SurviveTheHuntShared.Utils.EncodingHelper.UnpackRgba(colour, out byte r, out byte g, out byte b, out byte a);
            SetTextColour(r, g, b, a);

            SetTextScale(0f, 0.55f);
            BeginTextCommandDisplayText("STRING");
            AddTextComponentString(value);
            safeX += contentXOffset;
            EndTextCommandDisplayText(safeX, y + DefaultAlphaBarValueOffset);

            DrawTitle(in label, in safeX, in y, in adjustedWidth, in colour);
        }

        internal static void DrawTitle(in string title, in float x, in float y, in float width, in uint colour = uint.MaxValue, in float alphaBarTitleOffset = DefaultAlphaBarTitleOffset)
        {
            SurviveTheHuntShared.Utils.EncodingHelper.UnpackRgba(colour, out byte r, out byte g, out byte b, out byte a);
            SetTextColour(r, g, b, a);

            SetTextScale(0, 0.35f);
            BeginTextCommandDisplayText("STRING");
            AddTextComponentString(title);
            EndTextCommandDisplayText(x - width / 2.35f, y + alphaBarTitleOffset);
        }

        internal static void DrawAlphaBarBackground(in float x, in float y, in float width, in float height, in float spritePadding = DefaultAlphaBarSpritePadding)
        {
            DrawSprite("timerbars", "all_black_bg", x + spritePadding * 0.5f, y, width, height, 0f, 255, 255, 255, 128);
        }

        internal static void DrawTimer(in string label, in string value, in Rect rect, in uint colour = uint.MaxValue, in float contentXOffset = 0f, in float spritePadding = DefaultAlphaBarSpritePadding)
        {
            DrawTimer(label, value, in rect.X, in rect.Y, in rect.Width, in rect.Height, in colour, in contentXOffset, in spritePadding);
        }

        private static float GetSafeX(in float x, in float width)
        {
            return x - ((x + width * 0.5f) - 1f);
        }

        internal static void DrawProgress(in string label, float progress, in uint colour, in Rect rect, in byte dividers = 0, in float contentXOffset = 0f, in float spritePadding = DefaultAlphaBarSpritePadding)
        {
            progress = Math.Min(1f, Math.Max(0f, progress));

            float adjustedWidth = GetWidthAdjustedForProgressBar(in rect.Width, in label);

            float x = GetSafeX(in rect.X, in adjustedWidth);

            DrawAlphaBarBackground(x, in rect.Y, in adjustedWidth, in rect.Height, in spritePadding);

            DrawTitle(in label, in rect.X, in rect.Y, in adjustedWidth, in colour);

            SurviveTheHuntShared.Utils.EncodingHelper.UnpackRgba(in colour, out byte r, out byte g, out byte b, out byte a);

            const float progressBarHeight = 0.0115f;
            const float topOffset = 0.003f;
            const float padding = 0.011f;
            float progressBarWidth = 0.06f;

            x += (adjustedWidth * 0.5f) - (progressBarWidth * 0.5f) - padding;
            x += contentXOffset;

            if (dividers == 0)
            {
                DrawRect(x, rect.Y + topOffset, progressBarWidth, progressBarHeight, r, g, b, (int)(a * 0.45f));
                DrawRect(x - (progressBarWidth * (1f - progress) * 0.5f), rect.Y + topOffset, progressBarWidth * progress, progressBarHeight, r, g, b, byte.MaxValue);
            }
            else
            {
                const float dividerWidth = 0.001f;
                float dividerGap = dividers == 0 ? 0 : progressBarWidth / dividers;

                x -= progressBarWidth / dividers;

                progressBarWidth = dividerGap;
                float progressPerStep = 1f / dividers;

                for (byte i = 0; i < dividers; i++)
                {
                    DrawRect(x, rect.Y + topOffset, progressBarWidth, progressBarHeight, r, g, b, (int)(a * 0.45f));
                    float progressThisStep = Math.Min(1f, progress / progressPerStep);
                    DrawRect(x - (progressBarWidth * (1f - progressThisStep) * 0.5f), rect.Y + topOffset, progressBarWidth * progressThisStep, progressBarHeight, r, g, b, byte.MaxValue);

                    x += progressBarWidth + dividerWidth;
                    progress = Math.Max(0f, progress - progressPerStep);
                }
            }
        }

        internal static void Draw(in LabelledItem item, in Rect rect, in float spritePadding = DefaultAlphaBarSpritePadding)
        {
            switch(item.Type)
            {
                case LabelledItemType.Text:
                    DrawTimer(in item.Label, in item.Value, in rect, in item.Colour, in item.XOffset, in spritePadding);
                    break;
                case LabelledItemType.Progress:
                    DrawProgress(in item.Label, SurviveTheHuntShared.Utils.EncodingHelper.NormalFloatFromUtf16(item.Value), in item.Colour, in rect, in item.ProgressBarDividers, in item.XOffset, in spritePadding);
                    break;
            }
        }
    }
}
