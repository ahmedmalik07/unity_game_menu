using UnityEngine;
using UnityEngine.UIElements;

namespace Aetherfall.UI
{
    /// <summary>
    /// Animated backdrop: a deep-space gradient, slowly drifting nebula glows, a twinkling starfield
    /// with pointer parallax, the occasional shooting star, and a planet rim on the horizon.
    /// Everything is drawn by UI Toolkit, so it scales with the panel and needs no camera.
    /// </summary>
    public class MenuBackground : VisualElement
    {
        struct Star
        {
            public Vector2 Position;
            public float Size, Depth, Twinkle, Phase, Brightness;
        }

        struct Nebula
        {
            public VisualElement Element;
            public Vector2 Anchor;
            public float Size, Speed, Phase, Depth;
        }

        readonly VisualElement starLayer;
        readonly VisualElement planetLayer;
        readonly Nebula[] nebulae;
        readonly Star[] stars;
        readonly System.Random random = new(42);

        Vector2 parallax, parallaxTarget;
        float time;
        float nextShootingStar = 3f;
        float shootingStarAge = -1f;
        Vector2 shootingStart, shootingDirection;

        public MenuBackground()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = style.top = style.right = style.bottom = 0;
            style.backgroundImage = UITextures.VerticalGradient(new Color(0.05f, 0.06f, 0.17f), new Color(0.012f, 0.014f, 0.045f));

            (Color color, Vector2 anchor, float size)[] glows =
            {
                (new Color(0.42f, 0.24f, 1.00f, 0.34f), new Vector2(0.18f, 0.28f), 1250f),
                (new Color(0.12f, 0.80f, 0.98f, 0.20f), new Vector2(0.78f, 0.18f), 1000f),
                (new Color(1.00f, 0.24f, 0.56f, 0.16f), new Vector2(0.62f, 0.86f), 1100f),
                (new Color(0.16f, 0.30f, 0.95f, 0.30f), new Vector2(0.10f, 0.95f), 1300f),
            };
            nebulae = new Nebula[glows.Length];
            for (int i = 0; i < glows.Length; i++)
            {
                var element = new VisualElement { pickingMode = PickingMode.Ignore };
                element.style.position = Position.Absolute;
                element.style.width = element.style.height = glows[i].size;
                element.style.backgroundImage = UITextures.SoftCircle;
                element.style.unityBackgroundImageTintColor = glows[i].color;
                Add(element);
                nebulae[i] = new Nebula
                {
                    Element = element, Anchor = glows[i].anchor, Size = glows[i].size,
                    Speed = 0.05f + 0.03f * i, Phase = i * 1.7f, Depth = 0.4f + 0.2f * i,
                };
            }

            stars = new Star[190];
            for (int i = 0; i < stars.Length; i++)
            {
                float depth = Next(0.2f, 1f);
                stars[i] = new Star
                {
                    Position = new Vector2(Next(0f, 1f), Next(0f, 1f)),
                    Depth = depth,
                    Size = Mathf.Lerp(0.6f, 2.2f, depth * depth),
                    Twinkle = Next(0.6f, 2.4f),
                    Phase = Next(0f, 6.28f),
                    Brightness = Mathf.Lerp(0.35f, 1f, depth),
                };
            }

            starLayer = Layer();
            starLayer.generateVisualContent += DrawStars;
            planetLayer = Layer();
            planetLayer.generateVisualContent += DrawPlanet;

            RegisterCallback<GeometryChangedEvent>(_ => planetLayer.MarkDirtyRepaint());
            schedule.Execute(Tick).Every(16);
        }

        /// <summary>Pointer position in panel space; stars and nebulae shift gently against it.</summary>
        public void SetPointer(Vector2 panelPosition)
        {
            var size = layout.size;
            if (size.x <= 0f || size.y <= 0f) return;
            parallaxTarget = new Vector2(panelPosition.x / size.x - 0.5f, panelPosition.y / size.y - 0.5f);
        }

        VisualElement Layer()
        {
            var layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.left = layer.style.top = layer.style.right = layer.style.bottom = 0;
            Add(layer);
            return layer;
        }

        float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);

        void Tick(TimerState timer)
        {
            float dt = Mathf.Min(timer.deltaTime / 1000f, 0.1f);
            time += dt;
            parallax = Vector2.Lerp(parallax, parallaxTarget, 1f - Mathf.Exp(-dt * 3f));

            var size = layout.size;
            foreach (var n in nebulae)
            {
                float x = n.Anchor.x * size.x - n.Size * 0.5f + Mathf.Sin(time * n.Speed + n.Phase) * 90f - parallax.x * 60f * n.Depth;
                float y = n.Anchor.y * size.y - n.Size * 0.5f + Mathf.Cos(time * n.Speed * 0.8f + n.Phase) * 60f - parallax.y * 40f * n.Depth;
                float scale = 1f + Mathf.Sin(time * n.Speed * 1.3f + n.Phase) * 0.08f;
                n.Element.style.translate = new Translate(x, y);
                n.Element.style.scale = new Scale(new Vector2(scale, scale));
            }

            UpdateShootingStar(dt, size);
            starLayer.MarkDirtyRepaint();
        }

        void UpdateShootingStar(float dt, Vector2 size)
        {
            if (shootingStarAge >= 0f)
            {
                shootingStarAge += dt;
                if (shootingStarAge > 1.1f) shootingStarAge = -1f;
            }
            else if (time > nextShootingStar)
            {
                shootingStarAge = 0f;
                shootingStart = new Vector2(Next(0.3f, 0.95f) * size.x, Next(0.02f, 0.35f) * size.y);
                shootingDirection = new Vector2(-Next(0.8f, 1f), Next(0.25f, 0.45f)).normalized;
                nextShootingStar = time + Next(5f, 11f);
            }
        }

        void DrawStars(MeshGenerationContext context)
        {
            var rect = starLayer.contentRect;
            if (rect.width <= 0f) return;
            var painter = context.painter2D;

            foreach (var star in stars)
            {
                float x = Mathf.Repeat(star.Position.x + time * 0.0025f * star.Depth, 1f) * rect.width - parallax.x * 36f * star.Depth;
                float y = star.Position.y * rect.height - parallax.y * 24f * star.Depth;
                float alpha = star.Brightness * (0.55f + 0.45f * Mathf.Sin(time * star.Twinkle + star.Phase));
                var center = new Vector2(x, y);

                if (star.Size > 1.7f)
                {
                    painter.fillColor = new Color(0.6f, 0.85f, 1f, alpha * 0.1f);
                    painter.BeginPath();
                    painter.Arc(center, star.Size * 3.5f, Angle.Degrees(0f), Angle.Degrees(360f));
                    painter.Fill();
                }

                painter.fillColor = new Color(0.86f, 0.92f, 1f, alpha);
                painter.BeginPath();
                painter.Arc(center, star.Size, Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Fill();
            }

            if (shootingStarAge >= 0f)
            {
                float p = shootingStarAge / 1.1f;
                float fade = Mathf.Sin(Mathf.PI * p);
                var head = shootingStart + shootingDirection * (p * 700f);
                painter.lineCap = LineCap.Round;
                for (int i = 0; i < 8; i++)
                {
                    var a = head - shootingDirection * (i * 22f);
                    var b = head - shootingDirection * ((i + 1) * 22f);
                    painter.strokeColor = new Color(0.8f, 0.95f, 1f, fade * (1f - i / 8f) * 0.9f);
                    painter.lineWidth = Mathf.Lerp(2.4f, 0.4f, i / 8f);
                    painter.BeginPath();
                    painter.MoveTo(a);
                    painter.LineTo(b);
                    painter.Stroke();
                }
            }
        }

        void DrawPlanet(MeshGenerationContext context)
        {
            var rect = planetLayer.contentRect;
            if (rect.width <= 0f) return;
            var painter = context.painter2D;
            var center = new Vector2(rect.width * 0.84f, rect.height * 1.55f);
            float radius = rect.height * 0.86f;

            // Atmosphere glow: wide, faint strokes stacked under a crisp rim.
            (float width, float alpha)[] rims = { (70f, 0.025f), (38f, 0.05f), (18f, 0.09f), (7f, 0.2f), (2f, 0.85f) };
            foreach (var (width, alpha) in rims)
            {
                painter.strokeColor = new Color(0.45f, 0.9f, 1f, alpha);
                painter.lineWidth = width;
                painter.BeginPath();
                painter.Arc(center, radius, Angle.Degrees(180f), Angle.Degrees(360f));
                painter.Stroke();
            }

            painter.fillColor = new Color(0.015f, 0.02f, 0.06f, 1f);
            painter.BeginPath();
            painter.Arc(center, radius - 1f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();

            // Faint latitude lines give the planet some form.
            for (int i = 1; i <= 3; i++)
            {
                painter.strokeColor = new Color(0.45f, 0.8f, 1f, 0.05f / i);
                painter.lineWidth = 1.5f;
                painter.BeginPath();
                painter.Arc(center, radius - i * 28f, Angle.Degrees(180f), Angle.Degrees(360f));
                painter.Stroke();
            }
        }
    }
}
