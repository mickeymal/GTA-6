using UnityEngine;

namespace ViceBayEmpire.Audio
{
    /// <summary>
    /// Synthesizes all sound effects as AudioClips in code, so the game ships with no
    /// audio files and still has gunfire, engines, explosions, footsteps, UI blips,
    /// rain and city ambience. Replace any clip by dropping a real .wav/.ogg into the
    /// AudioManager's overrides — the synth is only the zero-asset default.
    /// </summary>
    public static class ProceduralAudio
    {
        const int SR = 44100;

        static AudioClip Make(string name, float seconds, System.Func<float, float> sample, bool loop = false)
        {
            int count = Mathf.Max(1, (int)(SR * seconds));
            var data = new float[count];
            for (int i = 0; i < count; i++)
                data[i] = Mathf.Clamp(sample(i / (float)SR), -1f, 1f);
            var clip = AudioClip.Create(name, count, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Noise() => Random.value * 2f - 1f;

        /// <summary>Sharp cracking gunshot: noise burst + fast exponential decay.</summary>
        public static AudioClip Gunshot()
            => Make("sfx_gunshot", 0.28f, t =>
            {
                float env = Mathf.Exp(-t * 34f);
                float body = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.5f;
                return (Noise() * 0.9f + body) * env;
            });

        public static AudioClip SuppressedShot()
            => Make("sfx_gunshot_supp", 0.16f, t =>
            {
                float env = Mathf.Exp(-t * 55f);
                return (Noise() * 0.5f) * env;
            });

        /// <summary>Boomy explosion: low rumble + noise, long decay.</summary>
        public static AudioClip Explosion()
            => Make("sfx_explosion", 1.4f, t =>
            {
                float env = Mathf.Exp(-t * 3.2f);
                float rumble = Mathf.Sin(2f * Mathf.PI * (40f + 20f * Mathf.Exp(-t * 4f)) * t);
                return (rumble * 0.6f + Noise() * 0.7f) * env;
            });

        /// <summary>Loopable engine drone; harmonic stack. Pitch is shifted per-RPM at play time.</summary>
        public static AudioClip EngineLoop()
            => Make("sfx_engine", 0.5f, t =>
            {
                float f = 60f;
                float s = 0f;
                s += Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f;
                s += Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.25f;
                s += Mathf.Sin(2f * Mathf.PI * f * 3f * t) * 0.15f;
                s += Noise() * 0.08f;
                return s * 0.5f;
            }, loop: true);

        public static AudioClip Footstep()
            => Make("sfx_footstep", 0.09f, t =>
            {
                float env = Mathf.Exp(-t * 60f);
                return Noise() * env * 0.5f;
            });

        public static AudioClip UIClick()
            => Make("sfx_ui", 0.05f, t => Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 40f) * 0.4f);

        public static AudioClip Cash()
            => Make("sfx_cash", 0.35f, t =>
            {
                float env = Mathf.Exp(-t * 8f);
                float shimmer = Mathf.Sin(2f * Mathf.PI * (1200f + 400f * Mathf.Sin(t * 60f)) * t);
                return shimmer * env * 0.3f;
            });

        public static AudioClip Siren()
            => Make("sfx_siren", 1.0f, t =>
            {
                float f = 700f + 300f * Mathf.Sin(2f * Mathf.PI * 1.5f * t);
                return Mathf.Sin(2f * Mathf.PI * f * t) * 0.3f;
            }, loop: true);

        /// <summary>City ambience: low hum bed for the world.</summary>
        public static AudioClip CityAmbience()
            => Make("amb_city", 2.0f, t =>
            {
                float hum = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.15f
                          + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.08f;
                return hum + Noise() * 0.04f;
            }, loop: true);

        public static AudioClip Rain()
            => Make("amb_rain", 1.5f, t => Noise() * 0.18f, loop: true);

        /// <summary>Ocean waves: slow-swelling filtered noise for the beach/bay.</summary>
        public static AudioClip Waves()
            => Make("amb_waves", 3.0f, t =>
            {
                float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.15f * t);
                return Noise() * 0.16f * swell + Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.04f;
            }, loop: true);

        /// <summary>Wind: airy band-limited noise for the airport / rooftops.</summary>
        public static AudioClip Wind()
            => Make("amb_wind", 3.0f, t =>
            {
                float gust = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 0.08f * t));
                return Noise() * 0.12f * gust;
            }, loop: true);

        /// <summary>Placeholder "voice" blip for dialogue lines (a short vowel-like tone).</summary>
        public static AudioClip VoiceBlip(float pitch = 1f)
            => Make("vo_blip", 0.12f, t =>
            {
                float f = 180f * pitch;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.12f));
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.25f) * env * 0.4f;
            });
    }
}
