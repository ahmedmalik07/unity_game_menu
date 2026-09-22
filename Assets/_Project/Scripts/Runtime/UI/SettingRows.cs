using System;
using Aetherfall.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Aetherfall.UI
{
    /// <summary>
    /// One focusable line in the settings list. Up/down moves between rows (default navigation),
    /// left/right changes the value, so the whole screen works with keys or a gamepad.
    /// </summary>
    public abstract class SettingRow : VisualElement
    {
        public string Description { get; }
        protected readonly VisualElement Control;

        protected SettingRow(string label, string description)
        {
            Description = description;
            focusable = true;
            AddToClassList("setting-row");

            var marker = new VisualElement { pickingMode = PickingMode.Ignore };
            marker.AddToClassList("setting-row__marker");
            Add(marker);

            var text = new Label(label.ToUpperInvariant()) { pickingMode = PickingMode.Ignore };
            text.AddToClassList("setting-row__label");
            Add(text);

            Control = new VisualElement();
            Control.AddToClassList("setting-row__control");
            Add(Control);

            UIUtil.EnableHoverFocus(this);
            RegisterCallback<NavigationMoveEvent>(OnNavigate);
        }

        void OnNavigate(NavigationMoveEvent evt)
        {
            int direction = evt.direction switch
            {
                NavigationMoveEvent.Direction.Left => -1,
                NavigationMoveEvent.Direction.Right => 1,
                _ => 0,
            };
            if (direction == 0) return;
            Step(direction);
            evt.StopPropagation();
            focusController?.IgnoreEvent(evt);
        }

        public abstract void Step(int direction);
        public abstract void Refresh();

        protected static VisualElement Chevron(bool left)
        {
            var arrow = new VisualElement();
            arrow.AddToClassList("stepper__arrow");
            var chevron = new VisualElement { pickingMode = PickingMode.Ignore };
            chevron.AddToClassList("chevron");
            chevron.AddToClassList(left ? "chevron--left" : "chevron--right");
            arrow.Add(chevron);
            return arrow;
        }
    }

    /// <summary>Cycles through a fixed list of options: ‹ VALUE ›, with pips showing the position.</summary>
    public class OptionStepper : SettingRow
    {
        readonly Func<string[]> options;
        readonly Func<int> getter;
        readonly Action<int> setter;
        readonly Label value;
        readonly VisualElement pips;

        public OptionStepper(string label, string description, Func<string[]> options, Func<int> getter, Action<int> setter)
            : base(label, description)
        {
            this.options = options;
            this.getter = getter;
            this.setter = setter;
            AddToClassList("setting-row--stepper");

            var left = Chevron(true);
            left.RegisterCallback<ClickEvent>(_ => Step(-1));
            Control.Add(left);

            var center = new VisualElement { pickingMode = PickingMode.Ignore };
            center.AddToClassList("stepper__center");
            value = new Label { pickingMode = PickingMode.Ignore };
            value.AddToClassList("stepper__value");
            center.Add(value);
            pips = new VisualElement { pickingMode = PickingMode.Ignore };
            pips.AddToClassList("stepper__pips");
            center.Add(pips);
            Control.Add(center);

            var right = Chevron(false);
            right.RegisterCallback<ClickEvent>(_ => Step(1));
            Control.Add(right);

            RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                Step(1);
                evt.StopPropagation();
            });
            Refresh();
        }

        public override void Step(int direction)
        {
            var names = options();
            if (names.Length <= 1)
            {
                AudioManager.Play(UISound.Error);
                return;
            }
            setter((getter() + direction + names.Length) % names.Length);
            AudioManager.Play(UISound.Tick);
            Refresh();

            // Nudge the value in the direction of travel; the USS transition eases it back.
            string nudge = direction < 0 ? "stepper__value--from-left" : "stepper__value--from-right";
            value.AddToClassList(nudge);
            value.schedule.Execute(() => value.RemoveFromClassList(nudge)).StartingIn(60);
        }

        public override void Refresh()
        {
            var names = options();
            int index = Mathf.Clamp(getter(), 0, Mathf.Max(0, names.Length - 1));
            value.text = names.Length > 0 ? names[index] : "-";

            pips.Clear();
            if (names.Length > 8) return;
            for (int i = 0; i < names.Length; i++)
            {
                var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                pip.AddToClassList("pip");
                if (i == index) pip.AddToClassList("pip--on");
                pips.Add(pip);
            }
        }
    }

    /// <summary>A 0..1 slider with a filled rail, draggable knob and formatted readout.</summary>
    public class SettingSlider : SettingRow
    {
        readonly Func<float> getter;
        readonly Action<float> setter;
        readonly Func<float, string> format;
        readonly float step;
        readonly VisualElement track;
        readonly VisualElement fill;
        readonly VisualElement knob;
        readonly Label readout;

        public SettingSlider(string label, string description, Func<float> getter, Action<float> setter,
            Func<float, string> format = null, float step = 0.05f)
            : base(label, description)
        {
            this.getter = getter;
            this.setter = setter;
            this.format = format ?? (v => $"{Mathf.RoundToInt(v * 100f)}");
            this.step = step;
            AddToClassList("setting-row--slider");

            track = new VisualElement();
            track.AddToClassList("slider__track");
            var rail = new VisualElement { pickingMode = PickingMode.Ignore };
            rail.AddToClassList("slider__rail");
            fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("slider__fill");
            rail.Add(fill);
            track.Add(rail);
            knob = new VisualElement { pickingMode = PickingMode.Ignore };
            knob.AddToClassList("slider__knob");
            track.Add(knob);
            Control.Add(track);

            readout = new Label { pickingMode = PickingMode.Ignore };
            readout.AddToClassList("slider__value");
            Control.Add(readout);

            track.RegisterCallback<PointerDownEvent>(evt =>
            {
                Focus();
                track.CapturePointer(evt.pointerId);
                AddToClassList("setting-row--dragging");
                SetFromPointer(evt.localPosition.x);
            });
            track.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (track.HasPointerCapture(evt.pointerId)) SetFromPointer(evt.localPosition.x);
            });
            track.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!track.HasPointerCapture(evt.pointerId)) return;
                track.ReleasePointer(evt.pointerId);
                RemoveFromClassList("setting-row--dragging");
            });
            Refresh();
        }

        void SetFromPointer(float x)
        {
            float width = track.layout.width;
            if (width <= 0f) return;
            Set(Mathf.Clamp01(x / width));
        }

        void Set(float v)
        {
            v = Mathf.Round(Mathf.Clamp01(v) / 0.01f) * 0.01f;
            if (Mathf.Approximately(v, getter())) return;
            setter(v);
            AudioManager.Play(UISound.Tick);
            Refresh();
        }

        public override void Step(int direction) => Set(getter() + direction * step);

        public override void Refresh()
        {
            float v = Mathf.Clamp01(getter());
            fill.style.width = Length.Percent(v * 100f);
            knob.style.left = Length.Percent(v * 100f);
            readout.text = format(v);
        }
    }
}
