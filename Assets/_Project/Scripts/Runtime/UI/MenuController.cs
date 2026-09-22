using System;
using System.Collections;
using Aetherfall.Audio;
using Aetherfall.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Aetherfall.UI
{
    /// <summary>
    /// Main menu flow: intro, screen navigation (main / settings / credits), confirm dialogs and the
    /// loading screen into the game scene.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MenuController : MonoBehaviour
    {
        [SerializeField] float minimumLoadingTime = 3f;

        static readonly string[] Tips =
        {
            "Aether shards glow brighter when a rift is near. Follow the light.",
            "Spirits you bind remember how you treated them.",
            "Resting at a Spire restores your strength and saves your journey.",
            "The sky cracked long before the war. Some say it was opened on purpose.",
            "Hold your ground: a perfectly timed parry staggers even the largest foes.",
        };

        static readonly string[] LoadingStates = { "WEAVING THE AETHER", "CHARTING THE RIFT", "WAKING THE SPIRE", "READY" };

        VisualElement root;
        VisualElement mainScreen, settingsScreen, creditsScreen, loadingScreen;
        VisualElement fader;
        MenuBackground background;
        SettingsView settings;
        ConfirmDialog dialog;
        Button continueButton;
        Label continueMeta, clock;
        VisualElement current;
        VisualElement lastMainFocus;
        VisualElement lastFocused;
        bool busy;

        public SettingsView Settings => settings;

        void Start()
        {
            AudioManager.DuckMusic(1f);
            Bind();
            PlayIntro();
        }

        void Bind()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            UIUtil.Prepare(root);

            background = new MenuBackground();
            root.Q("background").Add(background);
            root.Q("vignette").style.backgroundImage = UITextures.Vignette;
            root.RegisterCallback<PointerMoveEvent>(evt => background.SetPointer(evt.position));
            root.RegisterCallback<FocusInEvent>(evt => lastFocused = evt.target as VisualElement);

            mainScreen = root.Q("screen-main");
            settingsScreen = root.Q("screen-settings");
            creditsScreen = root.Q("screen-credits");
            loadingScreen = root.Q("screen-loading");
            fader = root.Q("fader");

            settings = new SettingsView(settingsScreen, ShowMain);
            dialog = new ConfirmDialog(root.Q("dialog"));

            // Decorative gradients that USS cannot express.
            root.Q("brand-rule").style.backgroundImage = UITextures.HorizontalFade(new Color(0.49f, 0.95f, 1f, 0.9f));
            root.Query(className: "menu-button__glow").ForEach(glow =>
                glow.style.backgroundImage = UITextures.HorizontalFade(new Color(0.49f, 0.95f, 1f, 0.22f)));
            root.Q("news-art").Insert(0, new CardArt());
            root.Q("loading-spinner").Add(new Spinner());

            continueButton = root.Q<Button>("btn-continue");
            continueMeta = root.Q<Label>("continue-meta");
            UIUtil.Bind(continueButton, () => BeginJourney(newGame: false), UISound.Start);
            UIUtil.Bind(root.Q<Button>("btn-new-game"), RequestNewGame, SaveSystem.HasSave ? UISound.Confirm : UISound.Start);
            UIUtil.Bind(root.Q<Button>("btn-settings"), OpenSettings);
            UIUtil.Bind(root.Q<Button>("btn-credits"), OpenCredits);
            UIUtil.Bind(root.Q<Button>("btn-quit"), RequestQuit);
            UIUtil.Bind(root.Q<Button>("btn-credits-back"), ShowMain, UISound.Back);

            mainScreen.Query<Button>().ForEach(b => b.RegisterCallback<FocusInEvent>(_ => lastMainFocus = b));

            clock = root.Q<Label>("clock");
            root.schedule.Execute(() => clock.text = DateTime.Now.ToString("HH:mm")).Every(1000);
            root.Q<Label>("version").text = $"v{Application.version}  ·  UNITY {Application.unityVersion}";

            RefreshContinue();
        }

        void PlayIntro()
        {
            foreach (var screen in new[] { settingsScreen, creditsScreen, loadingScreen })
                UIUtil.SetVisible(screen, false, instant: true);
            current = mainScreen;

            fader.schedule.Execute(() => fader.RemoveFromClassList("fader--on")).StartingIn(50);
            UIUtil.Reveal(root.Q("brand"), 250);
            int i = 0;
            mainScreen.Query(className: "menu-button").ForEach(button => UIUtil.Reveal(button, 520 + 70 * i++));
            UIUtil.Reveal(root.Q("news"), 900);
            UIUtil.Reveal(root.Q("footer"), 1000);
            UIUtil.FocusLater(FirstMainButton(), 700);
        }

        void RefreshContinue()
        {
            bool hasSave = SaveSystem.HasSave;
            continueButton.SetEnabled(hasSave);
            continueMeta.text = hasSave ? SaveSystem.Describe() : "NO JOURNEY YET";
        }

        VisualElement FirstMainButton() => continueButton.enabledSelf ? continueButton : root.Q<Button>("btn-new-game");

        void Update()
        {
            if (busy || root == null) return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)) Back();

            if (current == settingsScreen && !dialog.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton4)) settings.CycleTab(-1);
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton5)) settings.CycleTab(1);
            }

            // Clicking empty space clears focus; restore it so keys and gamepad keep working.
            if (root.focusController.focusedElement == null && lastFocused != null && UIUtil.IsShown(lastFocused) &&
                lastFocused.enabledInHierarchy)
                lastFocused.Focus();
        }

        // ---------------------------------------------------------------- Navigation

        void Show(VisualElement screen)
        {
            if (current == screen) return;
            if (current != null) UIUtil.SetVisible(current, false);
            UIUtil.SetVisible(screen, true);
            current = screen;
        }

        public void ShowMain()
        {
            Show(mainScreen);
            UIUtil.FocusLater(lastMainFocus ?? FirstMainButton());
        }

        public void OpenSettings()
        {
            Show(settingsScreen);
            settings.Open();
        }

        public void OpenCredits()
        {
            Show(creditsScreen);
            UIUtil.FocusLater(root.Q<Button>("btn-credits-back"));
        }

        public void Back()
        {
            if (busy) return;
            if (dialog.IsOpen)
            {
                AudioManager.Play(UISound.Back);
                dialog.Cancel();
            }
            else if (current != mainScreen)
            {
                AudioManager.Play(UISound.Back);
                GameSettings.Save();
                ShowMain();
            }
            else
            {
                RequestQuit();
            }
        }

        public void CancelDialog() => dialog.Cancel();

        public void RequestNewGame()
        {
            if (!SaveSystem.HasSave)
            {
                BeginJourney(newGame: true);
                return;
            }
            dialog.Show("NEW JOURNEY", "Start over?",
                "Beginning a new journey will overwrite your current progress. This cannot be undone.",
                "START OVER", () => BeginJourney(newGame: true), mainScreen, destructive: true);
        }

        public void RequestQuit()
        {
            dialog.Show("LEAVING SO SOON?", "Quit Aetherfall?",
                "Your journey was saved at the last Spire you rested at.",
                "QUIT GAME", Quit, current, destructive: true, cancelText: "STAY");
        }

        static void Quit()
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- Loading

        public void BeginJourney(bool newGame)
        {
            if (busy) return;
            if (newGame) SaveSystem.StartNewJourney();
            StartCoroutine(LoadGame());
        }

        IEnumerator LoadGame()
        {
            busy = true;
            root.focusController.focusedElement?.Blur();
            AudioManager.DuckMusic(0.45f);

            fader.AddToClassList("fader--on");
            yield return new WaitForSecondsRealtime(0.6f);

            root.Q<Label>("loading-chapter").text = $"CHAPTER {SaveSystem.ToRoman(SaveSystem.Chapter)}";
            UIUtil.SetVisible(current, false, instant: true);
            UIUtil.SetVisible(loadingScreen, true);
            current = loadingScreen;
            fader.RemoveFromClassList("fader--on");

            var tip = root.Q<Label>("loading-tip");
            int tipIndex = UnityEngine.Random.Range(0, Tips.Length);
            tip.text = Tips[tipIndex];
            var tipCycle = tip.schedule.Execute(() =>
            {
                tip.AddToClassList("loading__tip--out");
                tip.schedule.Execute(() =>
                {
                    tipIndex = (tipIndex + 1) % Tips.Length;
                    tip.text = Tips[tipIndex];
                    tip.RemoveFromClassList("loading__tip--out");
                }).StartingIn(400);
            }).Every(3500).StartingIn(3500);

            var fill = root.Q("loading-fill");
            var percent = root.Q<Label>("loading-percent");
            var status = root.Q<Label>("loading-status");

            var operation = SceneManager.LoadSceneAsync(Scenes.Game);
            operation.allowSceneActivation = false;

            float startTime = Time.unscaledTime;
            float shown = 0f;
            while (shown < 1f)
            {
                float real = Mathf.Clamp01(operation.progress / 0.9f);
                float paced = (Time.unscaledTime - startTime) / minimumLoadingTime;
                shown = Mathf.MoveTowards(shown, Mathf.Min(real, paced), Time.unscaledDeltaTime * 1.2f);
                fill.style.width = Length.Percent(shown * 100f);
                percent.text = $"{Mathf.RoundToInt(shown * 100f)}%";
                status.text = LoadingStates[Mathf.Min(LoadingStates.Length - 1, (int)(shown * (LoadingStates.Length - 1)))];
                yield return null;
            }

            tipCycle.Pause();
            yield return new WaitForSecondsRealtime(0.4f);
            fader.AddToClassList("fader--on");
            yield return new WaitForSecondsRealtime(0.7f);
            AudioManager.DuckMusic(0.7f);
            operation.allowSceneActivation = true;
        }
    }
}
