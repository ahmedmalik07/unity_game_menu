using System;
using Aetherfall.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Aetherfall.UI
{
    public static class UIUtil
    {
        public const string HiddenClass = "is-hidden";
        public const string PreClass = "is-pre";
        const int TransitionMs = 380;

        static float silentUntil;

        /// <summary>
        /// Shows or hides an element using the USS "is-hidden" transition, then removes it from layout
        /// once the fade has finished.
        /// </summary>
        public static void SetVisible(VisualElement element, bool visible, bool instant = false)
        {
            if (visible)
            {
                element.style.display = DisplayStyle.Flex;
                if (instant) element.RemoveFromClassList(HiddenClass);
                else element.schedule.Execute(() => element.RemoveFromClassList(HiddenClass)).StartingIn(20);
            }
            else
            {
                element.AddToClassList(HiddenClass);
                if (instant) element.style.display = DisplayStyle.None;
                else
                    element.schedule.Execute(() =>
                    {
                        if (element.ClassListContains(HiddenClass)) element.style.display = DisplayStyle.None;
                    }).StartingIn(TransitionMs);
            }
        }

        /// <summary>Removes "is-pre" after a delay, so the element animates into its resting state.</summary>
        public static void Reveal(VisualElement element, long delayMs) =>
            element.schedule.Execute(() => element.RemoveFromClassList(PreClass)).StartingIn(delayMs);

        /// <summary>
        /// Strips Unity's default button skin from every button under <paramref name="root"/> and makes
        /// hovering a button focus it, so mouse and keyboard/gamepad share a single highlight.
        /// Focus changes anywhere under the root play the hover sound.
        /// </summary>
        public static void Prepare(VisualElement root)
        {
            root.Query<Button>().ForEach(button =>
            {
                button.RemoveFromClassList(Button.ussClassName);
                EnableHoverFocus(button);
            });
            root.RegisterCallback<FocusInEvent>(_ =>
            {
                if (Time.unscaledTime >= silentUntil) AudioManager.Play(UISound.Hover);
            });
        }

        public static void EnableHoverFocus(VisualElement element) =>
            element.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (element.enabledInHierarchy && element.focusController?.focusedElement != element) element.Focus();
            });

        public static void Bind(Button button, Action action, UISound sound = UISound.Confirm)
        {
            button.clicked += () =>
            {
                AudioManager.Play(sound);
                action();
            };
        }

        /// <summary>Focuses an element once it has been laid out, without the hover blip.</summary>
        public static void FocusLater(VisualElement element, long delayMs = 60)
        {
            if (element == null) return;
            element.schedule.Execute(() =>
            {
                silentUntil = Time.unscaledTime + 0.05f;
                element.Focus();
            }).StartingIn(delayMs);
        }

        public static bool IsShown(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
                if (e.resolvedStyle.display == DisplayStyle.None || e.ClassListContains(HiddenClass))
                    return false;
            return element.panel != null;
        }
    }
}
