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
            Add(graphics, new OptionStepper("Display Mode", "Fullscreen, borderless window or windowed.",
                () => GameSettings.DisplayModeNames, () => GameSettings.DisplayMode,
                v => { GameSettings.DisplayMode = v; GameSettings.ApplyDisplay(); }));
            Add(graphics, new OptionStepper("Resolution", "Screen resolution.",
                () => GameSettings.ResolutionNames, () => GameSettings.Resolution,
                v => { GameSettings.Resolution = v; GameSettings.ApplyDisplay(); }));
            Add(graphics, new OptionStepper("Quality Preset", "Lower this if the game runs slowly.",
                () => GameSettings.QualityNames, () => GameSettings.Quality,
                v => { GameSettings.Quality = v; GameSettings.ApplyGraphics(); }));
            Add(graphics, new OptionStepper("V-Sync", "Stops screen tearing.",
                () => GameSettings.OnOffNames, () => GameSettings.VSync,
                v => { GameSettings.VSync = v; GameSettings.ApplyGraphics(); }));
            Add(graphics, new OptionStepper("Frame Rate Limit", "Maximum frame rate. Has no effect while V-Sync is on.",
                () => GameSettings.FrameLimitNames, () => GameSettings.FrameLimit,
                v => { GameSettings.FrameLimit = v; GameSettings.ApplyGraphics(); }));

            var audio = pages[1];
            Add(audio, new SettingSlider("Master Volume", "Overall volume.",
                () => GameSettings.MasterVolume, v => { GameSettings.MasterVolume = v; AudioManager.Instance.ApplyVolumes(); }));
            Add(audio, new SettingSlider("Music", "Music volume.",
                () => GameSettings.MusicVolume, v => GameSettings.MusicVolume = v));
            Add(audio, new SettingSlider("Effects", "Sound effects volume.",
                () => GameSettings.EffectsVolume, v => { GameSettings.EffectsVolume = v; AudioManager.Instance.ApplyVolumes(); }));
            Add(audio, new OptionStepper("Mute In Background", "Mute the game when you switch to another window.",
                () => GameSettings.OnOffNames, () => GameSettings.MuteInBackground, v => GameSettings.MuteInBackground = v));

            var gameplay = pages[2];
            Add(gameplay, new OptionStepper("Difficulty", "Game difficulty.",
                () => GameSettings.DifficultyNames, () => GameSettings.Difficulty, v => GameSettings.Difficulty = v));
            Add(gameplay, new SettingSlider("Look Sensitivity", "Mouse and right stick sensitivity.",
                () => GameSettings.Sensitivity, v => GameSettings.Sensitivity = v,
                v => $"{Mathf.Lerp(0.2f, 3f, v):0.0}x"));
            Add(gameplay, new OptionStepper("Invert Y Axis", "Invert vertical camera movement.",
                () => GameSettings.OnOffNames, () => GameSettings.InvertY, v => GameSettings.InvertY = v));
            Add(gameplay, new SettingSlider("Field Of View", "Camera field of view.",
                () => GameSettings.FieldOfView, v => GameSettings.FieldOfView = v,
                v => $"{Mathf.RoundToInt(Mathf.Lerp(60f, 110f, v))}°", step: 0.02f));
            Add(gameplay, new OptionStepper("Subtitles", "Show subtitles for dialogue.",
                () => GameSettings.OnOffNames, () => GameSettings.Subtitles, v => GameSettings.Subtitles = v));
        }

        void Add(VisualElement page, SettingRow row)
        {
            rows.Add(row);
            page.Add(row);
        }
    }
}
