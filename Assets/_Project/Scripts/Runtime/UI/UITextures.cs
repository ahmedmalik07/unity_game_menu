using System.Collections.Generic;
using UnityEngine;

namespace Aetherfall.UI
{
    /// <summary>
    /// Small textures generated at runtime (soft glows, gradients, vignette). USS has no gradients,
    /// so these give the UI depth without shipping any image files.
    /// </summary>
    public static class UITextures
    {
        static Texture2D softCircle;
        static Texture2D vignette;
        static readonly Dictionary<(Color, Color), Texture2D> verticals = new();
        static readonly Dictionary<Color, Texture2D> fades = new();

        public static Texture2D SoftCircle
        {
            get
            {
                if (!softCircle)
                    softCircle = Build(128, 128, (x, y) =>
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f)) * 2f;
                        float a = Mathf.Clamp01(1f - d);
                        return new Color(1f, 1f, 1f, Mathf.Pow(a * a * (3f - 2f * a), 1.6f));
                    });
                return softCircle;
            }
        }

        public static Texture2D Vignette
        {
            get
            {
                if (!vignette)
                    vignette = Build(128, 128, (x, y) =>
                    {
                        float dx = (x - 0.5f) * 1.6f, dy = (y - 0.5f) * 2f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        return new Color(0.01f, 0.01f, 0.04f, Mathf.SmoothStep(0f, 0.85f, Mathf.Clamp01((d - 0.45f) / 0.9f)));
                    });
                return vignette;
            }
        }

        public static Texture2D VerticalGradient(Color top, Color bottom)
        {
            if (!verticals.TryGetValue((top, bottom), out var tex) || !tex)
                verticals[(top, bottom)] = tex = Build(2, 256, (_, y) => Color.Lerp(bottom, top, y));
            return tex;
        }

        /// <summary>Colour on the left fading to transparent on the right.</summary>
        public static Texture2D HorizontalFade(Color color)
        {
            if (!fades.TryGetValue(color, out var tex) || !tex)
                fades[color] = tex = Build(256, 2, (x, _) =>
                    new Color(color.r, color.g, color.b, color.a * Mathf.Pow(1f - x, 1.8f)));
            return tex;
        }

        static Texture2D Build(int width, int height, System.Func<float, float, Color> pixel)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = pixel((x + 0.5f) / width, (y + 0.5f) / height);
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
