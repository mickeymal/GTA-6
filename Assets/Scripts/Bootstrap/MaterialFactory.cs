using System.Collections.Generic;
using UnityEngine;

namespace ViceBayEmpire.Bootstrap
{
    /// <summary>
    /// Creates PBR materials at runtime that work under the Built-in pipeline (Standard)
    /// or URP (Universal Render Pipeline/Lit). Supports metallic + smoothness so cars can
    /// look like glossy painted metal, glass can look like glass, and roads stay matte.
    /// Because the whole game is generated in code, we never rely on a material asset.
    /// </summary>
    public static class MaterialFactory
    {
        static Shader _shader;
        static readonly Dictionary<string, Material> _cache = new();

        static Shader LitShader()
        {
            if (_shader != null) return _shader;
            _shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard")
                      ?? Shader.Find("Legacy Shaders/Diffuse")
                      ?? Shader.Find("Sprites/Default");
            return _shader;
        }

        public static Material Solid(Color color, float smoothness = 0.2f, bool emissive = false, float metallic = 0f)
        {
            string key = $"{color}|{smoothness}|{emissive}|{metallic}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var mat = new Material(LitShader());
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                Color e = color * 1.8f;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", e);
            }
            _cache[key] = mat;
            return mat;
        }

        /// <summary>Applies a material to every renderer on a GameObject.</summary>
        public static GameObject Paint(GameObject go, Color color, float smoothness = 0.2f,
                                       bool emissive = false, float metallic = 0f)
        {
            var mat = Solid(color, smoothness, emissive, metallic);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;
            return go;
        }

        /// <summary>Reads the tint currently on a GameObject's material (either pipeline).</summary>
        public static Color GetColor(GameObject go, Color fallback)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null || r.sharedMaterial == null) return fallback;
            var m = r.sharedMaterial;
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            return fallback;
        }

        /// <summary>Glossy painted-metal look for vehicles.</summary>
        public static Material CarPaint(Color color) => Solid(color, 0.75f, false, 0.6f);

        /// <summary>Dark tinted glass for windshields and windows.</summary>
        public static Material Glass() => Solid(new Color(0.08f, 0.11f, 0.16f), 0.92f, false, 0.35f);
    }
}
