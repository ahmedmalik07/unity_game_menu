using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Aetherfall.Core
{
    /// <summary>
    /// Player-facing settings, persisted in PlayerPrefs. Every value is applied the moment it changes,
    /// so the settings screen never needs an "Apply" button.
    /// </summary>
    public static class GameSettings
    {
        public static readonly string[] DisplayModeNames = { "FULLSCREEN", "BORDERLESS", "WINDOWED" };
        static readonly FullScreenMode[] DisplayModes =
            { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };

        public static readonly string[] OnOffNames = { "OFF", "ON" };
        public static readonly string[] FrameLimitNames = { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "UNLIMITED" };
        static readonly int[] FrameLimits = { 30, 60, 120, 144, -1 };
        public static readonly string[] DifficultyNames = { "EASY", "NORMAL", "HARD" };

        // Graphics
        public static int DisplayMode;
        public static int Resolution;
        public static int Quality;
        public static int VSync;
        public static int FrameLimit;
        // Audio
        public static float MasterVolume;
        public static float MusicVolume;
        public static float EffectsVolume;
        public static int MuteInBackground;
        // Gameplay
        public static int Difficulty;
        public static float Sensitivity;
        public static int InvertY;
        public static float FieldOfView;
        public static int Subtitles;

        static List<Vector2Int> resolutions;

        public static IReadOnlyList<Vector2Int> Resolutions
        {
            get
            {
                if (resolutions == null)
                {
                    resolutions = Screen.resolutions
                        .Select(r => new Vector2Int(r.width, r.height))
                        .Distinct()
                        .OrderByDescending(r => r.x * r.y)
                        .ToList();
                    var current = new Vector2Int(Screen.width, Screen.height);
                    if (!resolutions.Contains(current)) resolutions.Insert(0, current);
                }
                return resolutions;
            }
        }

        public static string[] ResolutionNames => Resolutions.Select(r => $"{r.x} x {r.y}").ToArray();
        public static string[] QualityNames => QualitySettings.names.Select(n => n.ToUpperInvariant()).ToArray();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Load();
            ApplyGraphics();
        }

        public static void Load()
        {
            int currentMode = System.Array.IndexOf(DisplayModes, Screen.fullScreenMode);
            DisplayMode = PlayerPrefs.GetInt("settings.displayMode", currentMode < 0 ? 1 : currentMode);
            Resolution = Mathf.Clamp(PlayerPrefs.GetInt("settings.resolution", CurrentResolutionIndex()), 0, Resolutions.Count - 1);
            Quality = Mathf.Clamp(PlayerPrefs.GetInt("settings.quality", QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            VSync = PlayerPrefs.GetInt("settings.vsync", 1);
            FrameLimit = PlayerPrefs.GetInt("settings.frameLimit", 1);

            MasterVolume = PlayerPrefs.GetFloat("settings.master", 0.8f);
            MusicVolume = PlayerPrefs.GetFloat("settings.music", 0.6f);
            EffectsVolume = PlayerPrefs.GetFloat("settings.effects", 0.8f);
            MuteInBackground = PlayerPrefs.GetInt("settings.muteInBackground", 1);

            Difficulty = PlayerPrefs.GetInt("settings.difficulty", 1);
            Sensitivity = PlayerPrefs.GetFloat("settings.sensitivity", 0.3f);
            InvertY = PlayerPrefs.GetInt("settings.invertY", 0);
            FieldOfView = PlayerPrefs.GetFloat("settings.fov", 0.4f);
            Subtitles = PlayerPrefs.GetInt("settings.subtitles", 1);
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("settings.displayMode", DisplayMode);
            PlayerPrefs.SetInt("settings.resolution", Resolution);
            PlayerPrefs.SetInt("settings.quality", Quality);
            PlayerPrefs.SetInt("settings.vsync", VSync);
            PlayerPrefs.SetInt("settings.frameLimit", FrameLimit);

            PlayerPrefs.SetFloat("settings.master", MasterVolume);
            PlayerPrefs.SetFloat("settings.music", MusicVolume);
            PlayerPrefs.SetFloat("settings.effects", EffectsVolume);
            PlayerPrefs.SetInt("settings.muteInBackground", MuteInBackground);

            PlayerPrefs.SetInt("settings.difficulty", Difficulty);
            PlayerPrefs.SetFloat("settings.sensitivity", Sensitivity);
            PlayerPrefs.SetInt("settings.invertY", InvertY);
            PlayerPrefs.SetFloat("settings.fov", FieldOfView);
            PlayerPrefs.SetInt("settings.subtitles", Subtitles);
            PlayerPrefs.Save();
        }

        public static void ResetToDefaults()
        {
            foreach (var key in new[]
                     {
                         "displayMode", "resolution", "quality", "vsync", "frameLimit", "master", "music", "effects",
                         "muteInBackground", "difficulty", "sensitivity", "invertY", "fov", "subtitles"
                     })
                PlayerPrefs.DeleteKey("settings." + key);

            Load();
            ApplyGraphics();
            ApplyDisplay();
            Save();
        }

        /// <summary>Quality, vsync and frame cap. Safe to call at startup.</summary>
        public static void ApplyGraphics()
        {
            QualitySettings.SetQualityLevel(Quality, true);
            QualitySettings.vSyncCount = VSync;
            Application.targetFrameRate = FrameLimits[Mathf.Clamp(FrameLimit, 0, FrameLimits.Length - 1)];
        }

        /// <summary>Window mode and resolution. Only called when the player changes them.</summary>
        public static void ApplyDisplay()
        {
            var res = Resolutions[Mathf.Clamp(Resolution, 0, Resolutions.Count - 1)];
            Screen.SetResolution(res.x, res.y, DisplayModes[Mathf.Clamp(DisplayMode, 0, DisplayModes.Length - 1)]);
        }

        public static float FieldOfViewDegrees => Mathf.Lerp(60f, 110f, FieldOfView);
        public static float SensitivityMultiplier => Mathf.Lerp(0.2f, 3f, Sensitivity);

        static int CurrentResolutionIndex()
        {
            for (int i = 0; i < Resolutions.Count; i++)
                if (Resolutions[i].x == Screen.width && Resolutions[i].y == Screen.height)
                    return i;
            return 0;
        }
    }
}
