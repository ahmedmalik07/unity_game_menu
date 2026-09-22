using UnityEngine;
using UnityEngine.UIElements;

namespace Aetherfall.UI
{
    /// <summary>Procedural key art for the "What's new" card: a ringed world over a glowing horizon.</summary>
    public class CardArt : VisualElement
    {
        float time;

        public CardArt()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = style.top = style.right = style.bottom = 0;
            style.backgroundImage = UITextures.VerticalGradient(new Color(0.22f, 0.10f, 0.45f), new Color(0.02f, 0.03f, 0.10f));
            generateVisualContent += Draw;
            schedule.Execute(t =>
            {
                time += t.deltaTime / 1000f;
                MarkDirtyRepaint();
            }).Every(33);
        }

        void Draw(MeshGenerationContext context)
        {
            var r = contentRect;
            if (r.width <= 0f) return;
            var p = context.painter2D;
            var planet = new Vector2(r.width * 0.68f, r.height * 0.48f + Mathf.Sin(time * 0.6f) * 3f);
            float radius = r.height * 0.26f;

            // Glow
            for (int i = 5; i >= 1; i--)
            {
                p.fillColor = new Color(1f, 0.55f, 0.75f, 0.035f);
                p.BeginPath();
                p.Arc(planet, radius + i * 12f, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Fill();
            }

            p.fillColor = new Color(1f, 0.78f, 0.62f, 1f);
            p.BeginPath();
            p.Arc(planet, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Fill();

            p.fillColor = new Color(0.95f, 0.45f, 0.62f, 0.55f);
            p.BeginPath();
            p.Arc(planet + new Vector2(radius * 0.25f, radius * 0.2f), radius * 0.85f, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Fill();

            // Tilted ring: an ellipse approximated with a polyline.
            p.strokeColor = new Color(1f, 0.9f, 0.8f, 0.7f);
            p.lineWidth = 2f;
            p.BeginPath();
            const int segments = 48;
            float tilt = -0.3f;
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var local = new Vector2(Mathf.Cos(a) * radius * 1.8f, Mathf.Sin(a) * radius * 0.42f);
                var point = planet + new Vector2(local.x * Mathf.Cos(tilt) - local.y * Mathf.Sin(tilt),
                    local.x * Mathf.Sin(tilt) + local.y * Mathf.Cos(tilt));
                if (i == 0) p.MoveTo(point);
                else p.LineTo(point);
            }
            p.Stroke();

            // Mountain silhouette on the horizon.
            p.fillColor = new Color(0.03f, 0.03f, 0.1f, 1f);
            p.BeginPath();
            p.MoveTo(new Vector2(0f, r.height));
            float[] peaks = { 0.78f, 0.66f, 0.74f, 0.6f, 0.7f, 0.64f, 0.8f, 0.72f, 0.84f };
            for (int i = 0; i < peaks.Length; i++)
                p.LineTo(new Vector2(r.width * i / (peaks.Length - 1f), r.height * peaks[i]));
            p.LineTo(new Vector2(r.width, r.height));
            p.ClosePath();
            p.Fill();

            // A few stars.
            for (int i = 0; i < 18; i++)
            {
                float x = Mathf.Repeat(i * 0.618f, 1f) * r.width;
                float y = Mathf.Repeat(i * 0.377f, 0.55f) * r.height;
                float a = 0.4f + 0.4f * Mathf.Sin(time * (1f + i * 0.13f) + i);
                p.fillColor = new Color(1f, 1f, 1f, a);
                p.BeginPath();
                p.Arc(new Vector2(x, y), 1.1f, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Fill();
            }
        }
    }
}
