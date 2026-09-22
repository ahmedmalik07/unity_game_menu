using System;
using System.Collections.Generic;
using Aetherfall.Audio;
using Aetherfall.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Aetherfall.UI
{
    /// <summary>
    /// Drives the SettingsPanel.uxml template: tabs, rows and the contextual description line.
    /// Shared by the main menu and the in-game pause menu.
    /// </summary>
    public class SettingsView
    {
        public VisualElement Root { get; }

        readonly Button[] tabs;
        readonly VisualElement[] pages;
        readonly Label description;
        readonly List<SettingRow> rows = new();
        int currentTab;

        public SettingsView(VisualElement root, Action onClose)
        {
            Root = root;
            tabs = new[] { root.Q<Button>("tab-graphics"), root.Q<Button>("tab-audio"), root.Q<Button>("tab-gameplay") };
            pages = new[] { root.Q("page-graphics"), root.Q("page-audio"), root.Q("page-gameplay") };
            description = root.Q<Label>("setting-description");

            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                UIUtil.Bind(tabs[i], () => SelectTab(index, focusFirstRow: false), UISound.Tab);
            }

            UIUtil.Bind(root.Q<Button>("btn-settings-reset"), ResetDefaults, UISound.Back);
            UIUtil.Bind(root.Q<Button>("btn-settings-back"), () =>
            {
                GameSettings.Save();
                onClose();
            }, UISound.Back);

            BuildRows();
            root.RegisterCallback<FocusInEvent>(evt =>
                description.text = evt.target is SettingRow row ? row.Description : "");
        }

        public void Open()
        {
            foreach (var row in rows) row.Refresh();
            SelectTab(0, focusFirstRow: true, playSound: false);
        }

        public void CycleTab(int direction)
        {
            AudioManager.Play(UISound.Tab);
            SelectTab((currentTab + direction + tabs.Length) % tabs.Length, focusFirstRow: true, playSound: false);
        }

        public void SelectTab(int index, bool focusFirstRow = true, bool playSound = false)
        {
            if (playSound) AudioManager.Play(UISound.Tab);
            currentTab = index;
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].EnableInClassList("tab--active", i == index);
                pages[i].style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Re-trigger the page entrance animation.
            pages[index].AddToClassList(UIUtil.PreClass);
            UIUtil.Reveal(pages[index], 20);

            if (focusFirstRow && pages[index].childCount > 0) UIUtil.FocusLater(pages[index][0]);
        }

        void ResetDefaults()
        {
            GameSettings.ResetToDefaults();
            AudioManager.Instance.ApplyVolumes();
            foreach (var row in rows) row.Refresh();
        }

        void BuildRows()
        {
            var graphics = pages[0];
            Add(graphics, new OptionStepper("Display Mode", "Borderless keeps alt-tab instant; Fullscreen gives the GPU exclusive control.",
                () => GameSettings.DisplayModeNames, () => GameSettings.DisplayMode,
                v => { GameSettings.DisplayMode = v; GameSettings.ApplyDisplay(); }));
            Add(graphics, new OptionStepper("Resolution", "Rendering resolution of the game window.",
                () => GameSettings.ResolutionNames, () => GameSettings.Resolution,
                v => { GameSettings.Resolution = v; GameSettings.ApplyDisplay(); }));
            Add(graphics, new OptionStepper("Quality Preset", "Overall visual fidelity. Lower presets run faster on older hardware.",
                () => GameSettings.QualityNames, () => GameSettings.Quality,
                v => { GameSettings.Quality = v; GameSettings.ApplyGraphics(); }));
            Add(graphics, new OptionStepper("V-Sync", "Synchronise frames to your display to remove tearing.",
                () => GameSettings.OnOffNames, () => GameSettings.VSync,
                v => { GameSettings.VSync = v; GameSettings.ApplyGraphics(); }));
            Add(graphics, new OptionStepper("Frame Rate Limit", "Cap the frame rate to save power and keep fans quiet. Ignored while V-Sync is on.",
                () => GameSettings.FrameLimitNames, () => GameSettings.FrameLimit,
                v => { GameSettings.FrameLimit = v; GameSettings.ApplyGraphics(); }));

            var audio = pages[1];
            Add(audio, new SettingSlider("Master Volume", "Overall loudness of the game.",
                () => GameSettings.MasterVolume, v => { GameSettings.MasterVolume = v; AudioManager.Instance.ApplyVolumes(); }));
            Add(audio, new SettingSlider("Music", "Volume of the soundtrack.",
                () => GameSettings.MusicVolume, v => GameSettings.MusicVolume = v));
            Add(audio, new SettingSlider("Effects", "Volume of interface and world sound effects.",
                () => GameSettings.EffectsVolume, v => { GameSettings.EffectsVolume = v; AudioManager.Instance.ApplyVolumes(); }));
            Add(audio, new OptionStepper("Mute In Background", "Silence the game while its window is not focused.",
                () => GameSettings.OnOffNames, () => GameSettings.MuteInBackground, v => GameSettings.MuteInBackground = v));

            var gameplay = pages[2];
            Add(gameplay, new OptionStepper("Difficulty", "Story: focus on exploration. Balanced: the intended challenge. Veteran: every mistake matters.",
                () => GameSettings.DifficultyNames, () => GameSettings.Difficulty, v => GameSettings.Difficulty = v));
            Add(gameplay, new SettingSlider("Look Sensitivity", "How fast the camera turns with the mouse or right stick.",
                () => GameSettings.Sensitivity, v => GameSettings.Sensitivity = v,
                v => $"{Mathf.Lerp(0.2f, 3f, v):0.0}x"));
            Add(gameplay, new OptionStepper("Invert Y Axis", "Push forward to look down, like a flight stick.",
                () => GameSettings.OnOffNames, () => GameSettings.InvertY, v => GameSettings.InvertY = v));
            Add(gameplay, new SettingSlider("Field Of View", "Wider angles show more of the world; narrower ones feel more cinematic.",
                () => GameSettings.FieldOfView, v => GameSettings.FieldOfView = v,
                v => $"{Mathf.RoundToInt(Mathf.Lerp(60f, 110f, v))}°", step: 0.02f));
            Add(gameplay, new OptionStepper("Subtitles", "Show dialogue and important sounds as text.",
                () => GameSettings.OnOffNames, () => GameSettings.Subtitles, v => GameSettings.Subtitles = v));
        }

        void Add(VisualElement page, SettingRow row)
        {
            rows.Add(row);
            page.Add(row);
        }
    }
}
