using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Audio
{
    /// <summary>
    /// Central sound playback. Holds the procedurally-generated clips, plays one-shots
    /// at world positions, manages looping beds (city ambience, rain) and vehicle engine
    /// loops, and blips dialogue voice lines. Created by GameBootstrap; also reacts to
    /// notifications (UI click) and weather.
    ///
    /// To use real audio instead of the synth, assign clips to the public override
    /// fields in the inspector (or from a loader) — non-null overrides win.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Optional real-clip overrides (null = use synth)")]
        public AudioClip gunshotOverride, explosionOverride, engineOverride, footstepOverride,
                         uiOverride, cashOverride, sirenOverride, ambienceOverride, rainOverride;

        readonly Dictionary<string, AudioClip> clips = new();
        AudioSource ambienceSource;
        AudioSource rainSource;
        readonly List<AudioSource> pool = new();
        int poolIndex;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            clips["gunshot"] = gunshotOverride ? gunshotOverride : ProceduralAudio.Gunshot();
            clips["gunshot_supp"] = ProceduralAudio.SuppressedShot();
            clips["explosion"] = explosionOverride ? explosionOverride : ProceduralAudio.Explosion();
            clips["engine"] = engineOverride ? engineOverride : ProceduralAudio.EngineLoop();
            clips["footstep"] = footstepOverride ? footstepOverride : ProceduralAudio.Footstep();
            clips["ui"] = uiOverride ? uiOverride : ProceduralAudio.UIClick();
            clips["cash"] = cashOverride ? cashOverride : ProceduralAudio.Cash();
            clips["siren"] = sirenOverride ? sirenOverride : ProceduralAudio.Siren();
            clips["ambience"] = ambienceOverride ? ambienceOverride : ProceduralAudio.CityAmbience();
            clips["rain"] = rainOverride ? rainOverride : ProceduralAudio.Rain();
            clips["waves"] = ProceduralAudio.Waves();
            clips["wind"] = ProceduralAudio.Wind();
            clips["mission_start"] = ProceduralAudio.MissionStart();
            clips["mission_pass"] = ProceduralAudio.MissionPass();
            clips["mission_fail"] = ProceduralAudio.MissionFail();

            // one-shot voice pool + sfx pool
            for (int i = 0; i < 12; i++)
            {
                var src = new GameObject($"sfx_{i}").AddComponent<AudioSource>();
                src.transform.SetParent(transform);
                src.playOnAwake = false;
                src.spatialBlend = 0.85f;
                src.maxDistance = 120f;
                pool.Add(src);
            }

            ambienceSource = MakeLoop("ambience", 0.35f);
            rainSource = MakeLoop("rain", 0f);
        }

        void OnEnable() => GameEvents.Notify += OnNotify;
        void OnDisable() => GameEvents.Notify -= OnNotify;
        void OnNotify(string _, NotifyType type)
        {
            if (type == NotifyType.Money) Play("cash", Vector3.zero, spatial: false);
            else Play("ui", Vector3.zero, spatial: false);
        }

        AudioSource MakeLoop(string clip, float volume)
        {
            var src = new GameObject($"loop_{clip}").AddComponent<AudioSource>();
            src.transform.SetParent(transform);
            src.clip = clips[clip];
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = volume;
            src.Play();
            return src;
        }

        public void SetRain(bool on) { if (rainSource) rainSource.volume = on ? 0.4f : 0f; }

        /// <summary>Fire a one-shot from the pool at a world position (or 2D UI sound).</summary>
        public void Play(string key, Vector3 position, float volume = 1f, float pitch = 1f, bool spatial = true)
        {
            if (!clips.TryGetValue(key, out var clip) || clip == null) return;
            var src = pool[poolIndex];
            poolIndex = (poolIndex + 1) % pool.Count;
            src.transform.position = position;
            src.spatialBlend = spatial ? 0.85f : 0f;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        /// <summary>Play a non-positional UI/mission cue (mission_start / _pass / _fail).</summary>
        public void PlayCue(string key) => Play(key, Vector3.zero, 0.7f, 1f, spatial: false);

        public void PlayGunshot(Vector3 pos, bool suppressed) =>
            Play(suppressed ? "gunshot_supp" : "gunshot", pos, 0.8f, Random.Range(0.95f, 1.05f));
        public void PlayExplosion(Vector3 pos) => Play("explosion", pos, 1f);
        public void PlayFootstep(Vector3 pos) => Play("footstep", pos, 0.5f, Random.Range(0.9f, 1.1f));

        /// <summary>Attach a looping, RPM-pitched engine source to a vehicle.</summary>
        public AudioSource AttachEngine(Transform vehicle)
        {
            var src = vehicle.gameObject.AddComponent<AudioSource>();
            src.clip = clips["engine"];
            src.loop = true;
            src.spatialBlend = 0.9f;
            src.volume = 0.5f;
            src.maxDistance = 90f;
            src.Play();
            return src;
        }

        /// <summary>Place a looping positional (3D) ambient source in the world (city hum,
        /// waves, wind). Returns it so callers can tweak range/volume.</summary>
        public AudioSource PlaceAmbient(string key, Vector3 pos, float volume = 0.5f, float radius = 60f)
        {
            if (!clips.TryGetValue(key, out var clip) || clip == null) return null;
            var src = new GameObject($"amb_{key}").AddComponent<AudioSource>();
            src.transform.position = pos;
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 1f;             // fully 3D
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = radius * 0.25f;
            src.maxDistance = radius;
            src.volume = volume;
            src.Play();
            return src;
        }

        /// <summary>Play a dialogue voice-line placeholder blip sequence sized to the text.</summary>
        public void SpeakPlaceholder(string line, float pitch)
        {
            var clip = ProceduralAudio.VoiceBlip(pitch);
            var src = pool[poolIndex];
            poolIndex = (poolIndex + 1) % pool.Count;
            src.spatialBlend = 0f;
            src.pitch = 1f;
            src.PlayOneShot(clip, 0.6f);
        }
    }
}
