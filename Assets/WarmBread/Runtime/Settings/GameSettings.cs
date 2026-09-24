using System;

namespace WarmBread
{
    [Serializable]
    public sealed class GameSettings
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float AmbienceVolume = 0.85f;
        public float EffectsVolume = 0.9f;
        public float RadioVolume = 0.75f;
        public float MouseSensitivity = 1f;
        public bool InvertY;
        public float FieldOfView = 75f;
        public float HeadBob = 0.45f;
        public int ShadowQuality = 2;
        public float RenderScale = 1f;
        public bool Bloom = true;
        public bool MotionBlur;
        public bool FilmGrain = true;
        public float TextScale = 1f;
        public bool HighContrast;
        public bool Subtitles = true;
        public float SubtitleSpeed = 1f;
        public bool ToggleInteraction;
        public string Language = "ru";

        public void Clamp()
        {
            MasterVolume = Clamp01(MasterVolume);
            MusicVolume = Clamp01(MusicVolume);
            AmbienceVolume = Clamp01(AmbienceVolume);
            EffectsVolume = Clamp01(EffectsVolume);
            RadioVolume = Clamp01(RadioVolume);
            MouseSensitivity = Math.Max(0.05f, Math.Min(5f, MouseSensitivity));
            FieldOfView = Math.Max(55f, Math.Min(105f, FieldOfView));
            HeadBob = Clamp01(HeadBob);
            ShadowQuality = Math.Max(0, Math.Min(3, ShadowQuality));
            RenderScale = Math.Max(0.6f, Math.Min(1.5f, RenderScale));
            TextScale = Math.Max(0.8f, Math.Min(1.8f, TextScale));
            SubtitleSpeed = Math.Max(0.5f, Math.Min(2f, SubtitleSpeed));
            if (Language != "ru" && Language != "en") Language = "ru";
        }

        private static float Clamp01(float value) { return Math.Max(0f, Math.Min(1f, value)); }
    }
}
