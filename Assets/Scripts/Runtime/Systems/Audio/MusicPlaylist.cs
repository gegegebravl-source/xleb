using System;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Audio
{
    /// <summary>
    /// Плейлист ларька: какие треки из <c>Resources/Music</c> включены в ротацию.
    /// Выбор игрока сохраняется между запусками.
    /// </summary>
    /// <remarks>
    /// Хранится отдельно от игрового прогресса: это пользовательская настройка, а не состояние
    /// смены, поэтому обычный PlayerPrefs здесь уместен.
    /// </remarks>
    internal static class MusicPlaylist
    {
        private const string ResourcesPath = "Music";
        private const string EnabledKey = "WarmBread.MusicPlaylist";

        private static AudioClip[] tracks;
        private static bool[] enabled;
        private static bool isLoaded;

        /// <summary>Все доступные треки.</summary>
        public static AudioClip[] Tracks
        {
            get
            {
                EnsureLoaded();

                return tracks;
            }
        }

        /// <summary>Есть ли хотя бы один включённый трек.</summary>
        public static bool HasAnyEnabled
        {
            get
            {
                EnsureLoaded();

                for (var index = 0; index < enabled.Length; index++)
                {
                    if (enabled[index])
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Событие изменения выбора: радио и панель обновляются по нему.</summary>
        public static event Action Changed;

        public static bool IsEnabled(int index)
        {
            EnsureLoaded();

            return index >= 0 && index < enabled.Length && enabled[index];
        }

        public static void SetEnabled(int index, bool value)
        {
            EnsureLoaded();

            if (index < 0 || index >= enabled.Length || enabled[index] == value)
            {
                return;
            }

            enabled[index] = value;

            Save();
            Changed?.Invoke();
        }

        /// <summary>Включить или выключить трек целиком.</summary>
        public static void Toggle(int index)
        {
            SetEnabled(index, IsEnabled(index) == false);
        }

        /// <summary>Имя трека без пути.</summary>
        public static string GetTrackName(int index)
        {
            EnsureLoaded();

            return index >= 0 && index < tracks.Length && tracks[index] != false
                ? tracks[index].name
                : "Трек " + (index + 1);
        }

        private static void EnsureLoaded()
        {
            if (isLoaded)
            {
                return;
            }

            isLoaded = true;

            tracks = Resources.LoadAll<AudioClip>(ResourcesPath);

            enabled = new bool[tracks.Length];

            // По умолчанию включено всё: игрок выключает лишнее.
            for (var index = 0; index < enabled.Length; index++)
            {
                enabled[index] = true;
            }

            Load();
        }

        private static void Load()
        {
            if (PlayerPrefs.HasKey(EnabledKey) == false)
            {
                return;
            }

            var raw = PlayerPrefs.GetString(EnabledKey);

            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var flags = raw.Split(',');

            for (var index = 0; index < enabled.Length && index < flags.Length; index++)
            {
                enabled[index] = flags[index] == "1";
            }
        }

        private static void Save()
        {
            var flags = new string[enabled.Length];

            for (var index = 0; index < enabled.Length; index++)
            {
                flags[index] = enabled[index] ? "1" : "0";
            }

            PlayerPrefs.SetString(EnabledKey, string.Join(",", flags));
            PlayerPrefs.Save();
        }
    }
}
