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
            UIUtil.HoverFocus = false;

            var go = new GameObject("[ScreenshotDirector]");
            DontDestroyOnLoad(go);
            go.AddComponent<ScreenshotDirector>();
        }

        IEnumerator Start()
        {
            yield return Wait(4.5f);
            var menu = FindAnyObjectByType<MenuController>();
            yield return Shot("main-menu");

            menu.OpenSettings();
            yield return Wait(1f);
            yield return Shot("settings");

            menu.ShowMain();
            yield return Wait(0.6f);
            menu.BeginJourney(newGame: false);
            yield return Wait(2.6f);
            yield return Shot("loading");

            while (SceneManager.GetActiveScene().name != Scenes.Game) yield return null;
            yield return Wait(2.2f);
            FindAnyObjectByType<PauseController>().Pause();
            yield return Wait(1f);
            yield return Shot("pause-menu");

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
