using System;
using UnityEngine;

namespace Aetherfall.Core
{
    /// <summary>
    /// Minimal save slot. The menu only needs to know whether a journey exists, which chapter it is at
    /// and when it was last played.
    /// </summary>
    public static class SaveSystem
    {
        const string ExistsKey = "save.exists";
        const string ChapterKey = "save.chapter";
        const string TimeKey = "save.time";

        public static bool HasSave => PlayerPrefs.GetInt(ExistsKey, 0) == 1;
        public static int Chapter => PlayerPrefs.GetInt(ChapterKey, 1);

        public static DateTime LastPlayed =>
            long.TryParse(PlayerPrefs.GetString(TimeKey, ""), out var binary) ? DateTime.FromBinary(binary) : DateTime.Now;

        public static void StartNewJourney()
        {
            PlayerPrefs.SetInt(ExistsKey, 1);
            PlayerPrefs.SetInt(ChapterKey, 1);
            Touch();
        }

        public static void Touch()
        {
            PlayerPrefs.SetString(TimeKey, DateTime.Now.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        public static string Describe() => $"CHAPTER {Chapter}  ·  {Relative(LastPlayed)}";

        static string Relative(DateTime time)
        {
            var span = DateTime.Now - time;
            if (span.TotalMinutes < 1) return "JUST NOW";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} MIN AGO";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} H AGO";
            return span.TotalDays < 2 ? "YESTERDAY" : $"{(int)span.TotalDays} DAYS AGO";
        }
    }
}
