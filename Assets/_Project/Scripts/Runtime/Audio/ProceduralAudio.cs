using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aetherfall.Audio
{
    /// <summary>
    /// Synthesises every sound the menu uses, so the project ships without audio assets.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 44100;
        const float TwoPi = Mathf.PI * 2f;

        public static Dictionary<UISound, AudioClip> CreateUiClips() => new()
        {
            [UISound.Hover] = Clip("ui_hover", 0.09f, t =>
                (Sin(1480f, t) * 0.6f + Sin(2960f, t) * 0.12f) * Decay(t, 55f) * 0.22f),

            [UISound.Confirm] = Clip("ui_confirm", 0.32f, t =>
                (Note(880f, t, 0f, 14f) + Note(1318.5f, t, 0.07f, 12f) * 0.8f + Sin(440f, t) * Decay(t, 18f) * 0.3f) * 0.2f),

            [UISound.Back] = Clip("ui_back", 0.26f, t =>
                (Note(987.8f, t, 0f, 16f) + Note(740f, t, 0.06f, 14f) * 0.8f) * 0.18f),

            [UISound.Tab] = Clip("ui_tab", 0.07f, t =>
                (Sin(2200f, t) + Sin(3300f, t) * 0.3f) * Decay(t, 80f) * 0.14f),

            [UISound.Tick] = Clip("ui_tick", 0.04f, t => Sin(3000f, t) * Decay(t, 140f) * 0.12f),

            [UISound.Error] = Clip("ui_error", 0.2f, t =>
                (Sin(160f, t) + Sin(320f, t) * 0.4f + Sin(482f, t) * 0.2f) * Decay(t, 18f) * 0.25f),

            [UISound.Start] = StartClip(),
        };

        static float Sin(float freq, float t) => Mathf.Sin(TwoPi * freq * t);
        static float Decay(float t, float rate) => Mathf.Exp(-t * rate) * (1f - Mathf.Exp(-t * 3000f));

        static float Note(float freq, float t, float start, float rate)
        {
            if (t < start) return 0f;
            float local = t - start;
            return (Sin(freq, local) + Sin(freq * 2f, local) * 0.2f) * Decay(local, rate);
        }

        static AudioClip Clip(string name, float duration, Func<float, float> sample)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float fadeOut = Mathf.Clamp01((length - i) / (SampleRate * 0.005f));
                data[i] = sample(i / (float)SampleRate) * fadeOut;
            }
            var clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>A rising "whoosh" + shimmer used when a journey begins.</summary>
        static AudioClip StartClip()
        {
            const float duration = 1.6f;
            var random = new System.Random(11);
            float low = 0f, phase = 0f;
            return Clip("ui_start", duration, t =>
            {
                float p = t / duration;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float cutoff = Mathf.Lerp(0.01f, 0.2f, p);
                low += (noise - low) * cutoff;
                phase += TwoPi * Mathf.Lerp(220f, 880f, p * p) / SampleRate;
                float swell = Mathf.Sin(Mathf.PI * Mathf.Pow(p, 0.7f));
                float tone = Mathf.Sin(phase) * 0.35f + Mathf.Sin(phase * 1.5f) * 0.2f;
                return (low * 1.4f + tone) * swell * 0.3f;
            });
        }

        /// <summary>
        /// Renders a seamless 24 second ambient loop (interleaved stereo). Pure math with no Unity API
        /// calls, so it can run on a worker thread while the menu fades in.
        /// </summary>
        public static float[] RenderAmbientLoop()
        {
            const float chordLength = 6f;
            float[][] chords =
            {
                new[] { 110.00f, 164.81f, 220.00f, 261.63f, 493.88f }, // Am(add9)
                new[] { 87.31f, 130.81f, 220.00f, 329.63f, 392.00f },  // Fmaj7
                new[] { 130.81f, 196.00f, 293.66f, 329.63f, 493.88f }, // Cmaj9
                new[] { 98.00f, 146.83f, 246.94f, 329.63f, 440.00f },  // G6/9
            };

            int n = (int)(chordLength * chords.Length * SampleRate);
            var left = new float[n];
            var right = new float[n];
            var random = new System.Random(7);

            // Each chord swells in and out with a sin^2 window twice its length. Neighbouring windows
            // overlap by half, which sums to constant loudness and wraps cleanly at the loop point.
            int window = (int)(chordLength * 2f * SampleRate);
            for (int c = 0; c < chords.Length; c++)
            {
                int start = (int)((c - 0.5f) * chordLength * SampleRate);
                for (int k = 0; k < chords[c].Length; k++)
                {
                    float freq = chords[c][k];
                    float pan = (k / (chords[c].Length - 1f) * 2f - 1f) * 0.6f;
                    float gainL = Mathf.Sqrt(0.5f * (1f - pan)), gainR = Mathf.Sqrt(0.5f * (1f + pan));
                    float amp = freq < 150f ? 0.11f : 0.075f;
                    double w = 2.0 * Math.PI * freq / SampleRate;
                    double offset = random.NextDouble() * Math.PI * 2.0;
                    for (int i = 0; i < window; i++)
                    {
                        double env = Math.Sin(Math.PI * i / window);
                        env *= env;
                        double ph = w * i + offset;
                        double shimmer = 1.0 + 0.2 * Math.Sin(2.0 * Math.PI * 0.13 * i / SampleRate + k);
                        float v = (float)((Math.Sin(ph) + 0.3 * Math.Sin(2.0015 * ph) + 0.08 * Math.Sin(3.0 * ph)) * env * shimmer * amp);
                        int idx = ((start + i) % n + n) % n;
                        left[idx] += v * gainL;
                        right[idx] += v * gainR;
                    }
                }
            }

            // Sparse pentatonic bells drifting above the pad.
            float[] bellNotes = { 880f, 987.77f, 1174.66f, 1318.51f, 1567.98f, 1760f };
            int bellLength = (int)(3.5f * SampleRate);
            for (int b = 0; b < 16; b++)
            {
                int start = random.Next(n);
                float freq = bellNotes[random.Next(bellNotes.Length)];
                float pan = (float)(random.NextDouble() * 1.6 - 0.8);
                float gainL = Mathf.Sqrt(0.5f * (1f - pan)), gainR = Mathf.Sqrt(0.5f * (1f + pan));
                double w = 2.0 * Math.PI * freq / SampleRate;
                for (int i = 0; i < bellLength; i++)
                {
                    double t = (double)i / SampleRate;
                    double env = Math.Exp(-t * 1.4) * (1.0 - Math.Exp(-t * 400.0));
                    float v = (float)((Math.Sin(w * i) + 0.25 * Math.Sin(w * 2.76 * i)) * env * 0.035);
                    int idx = (start + i) % n;
                    left[idx] += v * gainL;
                    right[idx] += v * gainR;
                }
            }

            // Ping-pong echo, run over the buffer twice so the tail wraps into the loop start.
            int delayL = (int)(0.41f * SampleRate), delayR = (int)(0.53f * SampleRate);
            var wetL = new float[n];
            var wetR = new float[n];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    wetL[i] = left[i] + wetR[(i - delayR + n) % n] * 0.45f;
                    wetR[i] = right[i] + wetL[(i - delayL + n) % n] * 0.45f;
                }
            }

            var output = new float[n * 2];
            float peak = 0.0001f;
            for (int i = 0; i < n; i++)
            {
                output[i * 2] = left[i] + (wetL[i] - left[i]) * 0.4f;
                output[i * 2 + 1] = right[i] + (wetR[i] - right[i]) * 0.4f;
                peak = Mathf.Max(peak, Mathf.Abs(output[i * 2]), Mathf.Abs(output[i * 2 + 1]));
            }

            float gain = 0.7f / peak;
            for (int i = 0; i < output.Length; i++) output[i] *= gain;
            return output;
        }
    }
}
