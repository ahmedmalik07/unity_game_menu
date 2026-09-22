using UnityEngine;
using UnityEngine.UIElements;

namespace Aetherfall.UI
{
    /// <summary>A thin ring with a bright arc, drawn once and rotated.</summary>
    public class Spinner : VisualElement
    {
        float angle;

        public Spinner()
        {
            pickingMode = PickingMode.Ignore;
            style.flexGrow = 1;
            generateVisualContent += Draw;
            schedule.Execute(t =>
            {
                angle = (angle + t.deltaTime * 0.36f) % 360f;
                style.rotate = new Rotate(angle);
            }).Every(16);
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - 2f;
            if (radius <= 0f) return;
            var center = rect.center;
            var painter = context.painter2D;
            painter.lineWidth = 2.5f;
            painter.lineCap = LineCap.Round;

            painter.strokeColor = new Color(1f, 1f, 1f, 0.12f);
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            painter.strokeColor = new Color(0.49f, 0.95f, 1f, 1f);
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(20f));
            painter.Stroke();
        }
    }
}
