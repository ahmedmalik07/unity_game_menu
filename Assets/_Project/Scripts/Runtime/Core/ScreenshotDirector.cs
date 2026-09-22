using System.Collections;
using System.IO;
using Aetherfall.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aetherfall.Core
{
    /// <summary>
    /// Walks through every screen and saves a PNG of each, then quits. Only active when the player is
    /// launched with <c>-capture &lt;folder&gt;</c>; used to produce the README screenshots.
    /// </summary>
    public class ScreenshotDirector : MonoBehaviour
    {
        static string outputFolder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            int index = System.Array.IndexOf(args, "-capture");
            if (index < 0 || index + 1 >= args.Length) return;

            outputFolder = args[index + 1];
            Directory.CreateDirectory(outputFolder);
            // Show the "Continue" state in the shots.
            SaveSystem.StartNewJourney();

            var go = new GameObject("[ScreenshotDirector]");
            DontDestroyOnLoad(go);
            go.AddComponent<ScreenshotDirector>();
        }

        IEnumerator Start()
        {
            yield return Wait(4.5f);
            var menu = FindAnyObjectByType<MenuController>();

            yield return Shot("01-main-menu");

            menu.OpenSettings();
            yield return Wait(1f);
            yield return Shot("02-settings-graphics");
            menu.Settings.CycleTab(1);
            yield return Wait(0.8f);
            yield return Shot("03-settings-audio");
            menu.Settings.CycleTab(1);
            yield return Wait(0.8f);
            yield return Shot("04-settings-gameplay");

            menu.ShowMain();
            yield return Wait(0.6f);
            menu.OpenCredits();
            yield return Wait(1f);
            yield return Shot("05-credits");

            menu.ShowMain();
            yield return Wait(0.6f);
            menu.RequestNewGame();
            yield return Wait(0.8f);
            yield return Shot("06-confirm-dialog");
            menu.CancelDialog();
            yield return Wait(0.5f);

            menu.BeginJourney(newGame: false);
            yield return Wait(2.6f);
            yield return Shot("07-loading");

            while (SceneManager.GetActiveScene().name != Scenes.Game) yield return null;
            yield return Wait(2.2f);
            yield return Shot("08-in-game-hud");

            var pause = FindAnyObjectByType<PauseController>();
            pause.Pause();
            yield return Wait(1f);
            yield return Shot("09-pause-menu");

            Application.Quit();
        }

        static WaitForSecondsRealtime Wait(float seconds) => new(seconds);

        static IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(outputFolder, name + ".png"));
            yield return Wait(0.3f);
        }
    }
}
