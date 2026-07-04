using UnityEngine;

namespace ViceBayEmpire.Core
{
    /// <summary>
    /// Drives the in-game clock and (optionally) a directional Sun light for the
    /// day/night cycle. Weather is picked here and read by fog/particle controllers.
    /// One in-game day compresses into <see cref="dayLengthMinutes"/> real minutes.
    /// </summary>
    public class WorldClock : MonoBehaviour
    {
        [Header("Time")]
        [Range(0, 24)] public float hour = 8f;
        public int day = 1;
        public float dayLengthMinutes = 24f;      // real minutes per 24h

        [Header("Lighting")]
        public Light sun;
        public Gradient sunColorOverDay;
        public AnimationCurve sunIntensityOverDay = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Weather")]
        public Weather weather = Weather.Clear;
        public float weatherChangeMinSeconds = 60f;
        public float weatherChangeMaxSeconds = 180f;
        float weatherTimer;

        public enum Weather { Clear, Cloudy, Rain, Storm, Fog }

        void Start() => weatherTimer = Random.Range(weatherChangeMinSeconds, weatherChangeMaxSeconds);

        void Update()
        {
            float prevHour = hour;
            float hoursPerSecond = 24f / (dayLengthMinutes * 60f);
            hour += hoursPerSecond * Time.deltaTime;
            if (hour >= 24f)
            {
                hour -= 24f;
                day++;
                GameEvents.RaiseNewDay(day);
            }
            if (Mathf.FloorToInt(hour) != Mathf.FloorToInt(prevHour))
                GameEvents.RaiseHourChanged(hour);

            UpdateSun();

            weatherTimer -= Time.deltaTime;
            if (weatherTimer <= 0f)
            {
                weather = PickWeather();
                weatherTimer = Random.Range(weatherChangeMinSeconds, weatherChangeMaxSeconds);
                GameEvents.RaiseNotify($"Weather: {weather}", NotifyType.Info);
            }
        }

        void UpdateSun()
        {
            if (sun == null) return;
            float t = hour / 24f;
            // rotate: sunrise ~6h, noon ~12h, sunset ~18h
            sun.transform.rotation = Quaternion.Euler((hour - 6f) / 24f * 360f, 170f, 0f);
            float dayFactor = Mathf.Clamp01(Mathf.Sin((hour - 6f) / 12f * Mathf.PI));
            sun.intensity = sunIntensityOverDay.Evaluate(dayFactor) * (weather >= Weather.Rain ? 0.5f : 1f);
            if (sunColorOverDay != null) sun.color = sunColorOverDay.Evaluate(t);
            RenderSettings.ambientIntensity = Mathf.Lerp(0.25f, 1f, dayFactor);
        }

        Weather PickWeather()
        {
            float r = Random.value;
            if (r < 0.45f) return Weather.Clear;
            if (r < 0.65f) return Weather.Cloudy;
            if (r < 0.82f) return Weather.Rain;
            if (r < 0.93f) return Weather.Fog;
            return Weather.Storm;
        }

        public bool IsNight => hour < 6f || hour > 19f;
    }
}
