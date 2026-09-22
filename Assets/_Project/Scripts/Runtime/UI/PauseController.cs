using System.Collections;
using Aetherfall.Audio;
using Aetherfall.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Aetherfall.UI
{
    /// <summary>In-game HUD and pause menu. Pausing freezes Time.timeScale; the UI keeps animating.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseController : MonoBehaviour
    {
        VisualElement root, pauseScreen, settingsScreen, fader, autosave;
        SettingsView settings;
        ConfirmDialog dialog;
        VisualElement current;
        VisualElement lastFocused;
        bool leaving;

        public bool IsPaused { get; private set; }

        void Start()
        {
            Time.timeScale = 1f;
            root = GetComponent<UIDocument>().rootVisualElement;
            UIUtil.Prepare(root);
            root.RegisterCallback<FocusInEvent>(evt => lastFocused = evt.target as VisualElement);

            pauseScreen = root.Q("screen-pause");
            settingsScreen = root.Q("screen-settings");
            fader = root.Q("fader");
            autosave = root.Q("autosave");
            root.Q("autosave-spinner").Add(new Spinner());
            root.Q("vignette").style.backgroundImage = UITextures.Vignette;
            root.Query(className: "menu-button__glow").ForEach(glow =>
                glow.style.backgroundImage = UITextures.HorizontalFade(new Color(0.49f, 0.95f, 1f, 0.22f)));

            settings = new SettingsView(settingsScreen, () => Show(pauseScreen, root.Q<Button>("btn-pause-settings")));
            dialog = new ConfirmDialog(root.Q("dialog"));

            UIUtil.Bind(root.Q<Button>("btn-resume"), Resume, UISound.Back);
            UIUtil.Bind(root.Q<Button>("btn-pause-settings"), () =>
            {
                Show(settingsScreen, null);
                settings.Open();
            });
            UIUtil.Bind(root.Q<Button>("btn-main-menu"), () => dialog.Show("MAIN MENU", "Back to the main menu?",
                "Your progress is saved.", "MAIN MENU", ReturnToMenu, pauseScreen));
            UIUtil.Bind(root.Q<Button>("btn-pause-quit"), () => dialog.Show("QUIT", "Quit the game?",
                "Your progress is saved.", "QUIT", Quit, pauseScreen, destructive: true));

            UIUtil.SetVisible(pauseScreen, false, instant: true);
            UIUtil.SetVisible(settingsScreen, false, instant: true);

            fader.schedule.Execute(() => fader.RemoveFromClassList("fader--on")).StartingIn(100);
            UIUtil.Reveal(root.Q("objective"), 700);
            UIUtil.Reveal(root.Q("hud-hints"), 1000);
            SaveSystem.Touch();
            UIUtil.SetVisible(autosave, true);
            autosave.schedule.Execute(() => UIUtil.SetVisible(autosave, false)).StartingIn(3200);
        }

        void Update()
        {
            if (leaving || root == null) return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7) ||
                (IsPaused && Input.GetKeyDown(KeyCode.JoystickButton1)))
            {
                if (!IsPaused) Pause();
                else Back();
            }

            if (current == settingsScreen)
            {
                if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton4)) settings.CycleTab(-1);
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton5)) settings.CycleTab(1);
            }

            if (IsPaused && root.focusController.focusedElement == null && lastFocused != null &&
                UIUtil.IsShown(lastFocused) && lastFocused.enabledInHierarchy)
                lastFocused.Focus();
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            AudioManager.Play(UISound.Confirm);
            AudioManager.DuckMusic(0.45f);
            root.Q("hud").AddToClassList("hud--dimmed");
            Show(pauseScreen, root.Q<Button>("btn-resume"));
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            AudioManager.DuckMusic(0.7f);
            root.Q("hud").RemoveFromClassList("hud--dimmed");
            UIUtil.SetVisible(current, false);
            current = null;
            root.focusController.focusedElement?.Blur();
            lastFocused = null;
        }

        void Back()
        {
            if (dialog.IsOpen)
            {
                AudioManager.Play(UISound.Back);
                dialog.Cancel();
            }
            else if (current == settingsScreen)
            {
                AudioManager.Play(UISound.Back);
                GameSettings.Save();
                Show(pauseScreen, root.Q<Button>("btn-pause-settings"));
            }
            else
            {
                AudioManager.Play(UISound.Back);
                Resume();
            }
        }

        void Show(VisualElement screen, VisualElement focus)
        {
            if (current != null && current != screen) UIUtil.SetVisible(current, false);
            UIUtil.SetVisible(screen, true);
            current = screen;
            if (screen == pauseScreen)
            {
                int i = 0;
                screen.Query(className: "menu-button").ForEach(b =>
                {
                    b.AddToClassList(UIUtil.PreClass);
                    UIUtil.Reveal(b, 60 + 50 * i++);
                });
            }
            if (focus != null) UIUtil.FocusLater(focus, 120);
        }

        void ReturnToMenu() => StartCoroutine(Leave(() => SceneManager.LoadScene(Scenes.MainMenu)));

        static void Quit()
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator Leave(System.Action then)
        {
            leaving = true;
            SaveSystem.Touch();
            fader.AddToClassList("fader--on");
            yield return new WaitForSecondsRealtime(0.7f);
            Time.timeScale = 1f;
            then();
        }
    }
}
