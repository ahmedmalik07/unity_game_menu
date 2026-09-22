using System.Collections.Generic;
using System.Threading.Tasks;
using Aetherfall.Core;
using UnityEngine;

namespace Aetherfall.Audio
{
    public enum UISound { Hover, Confirm, Back, Tab, Tick, Error, Start }

    /// <summary>
    /// Persistent audio service. Created on first use and kept across scene loads so music never cuts.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager instance;

        AudioSource effects;
        AudioSource music;
        Dictionary<UISound, AudioClip> clips;
        readonly Dictionary<UISound, float> lastPlayed = new();
        Task<float[]> musicRender;
        float musicFade;
        float musicDuck = 1f;
        float musicDuckTarget = 1f;

        public static AudioManager Instance
        {
            get
            {
                if (!instance)
                {
                    var go = new GameObject("[Audio]");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<AudioManager>();
                }
                return instance;
            }
        }

        public static void Play(UISound sound) => Instance.PlayEffect(sound);

        /// <summary>Lowers the music (0..1) e.g. while a scene loads.</summary>
        public static void DuckMusic(float level) => Instance.musicDuckTarget = level;

        void Awake()
        {
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0f;

            clips = ProceduralAudio.CreateUiClips();
            musicRender = Task.Run(ProceduralAudio.RenderAmbientLoop);
            ApplyVolumes();
        }

        void Update()
        {
            if (musicRender is { IsCompleted: true })
            {
                if (musicRender.IsFaulted)
                {
                    Debug.LogException(musicRender.Exception);
                }
                else
                {
                    var data = musicRender.Result;
                    var clip = AudioClip.Create("ambient_loop", data.Length / 2, 2, ProceduralAudio.SampleRate, false);
                    clip.SetData(data, 0);
                    music.clip = clip;
                    music.Play();
                }
                musicRender = null;
            }

            musicFade = Mathf.MoveTowards(musicFade, 1f, Time.unscaledDeltaTime / 4f);
            musicDuck = Mathf.MoveTowards(musicDuck, musicDuckTarget, Time.unscaledDeltaTime * 1.5f);
            music.volume = GameSettings.MusicVolume * 0.55f * musicFade * musicDuck;
        }

        public void ApplyVolumes()
        {
            AudioListener.volume = GameSettings.MasterVolume;
            effects.volume = GameSettings.EffectsVolume;
        }

        void PlayEffect(UISound sound)
        {
            // Throttle rapid repeats (slider drags, fast scrolling) so they never turn into buzzing.
            float now = Time.unscaledTime;
            float minGap = sound is UISound.Tick or UISound.Hover ? 0.045f : 0.02f;
            if (lastPlayed.TryGetValue(sound, out var last) && now - last < minGap) return;
            lastPlayed[sound] = now;

            if (clips.TryGetValue(sound, out var clip)) effects.PlayOneShot(clip);
        }

        void OnApplicationFocus(bool focused)
        {
            AudioListener.pause = !focused && GameSettings.MuteInBackground == 1 && !Application.isEditor;
        }
    }
}
