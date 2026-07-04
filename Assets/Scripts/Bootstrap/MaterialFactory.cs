using System.Collections.Generic;
using UnityEngine;

namespace ViceBayEmpire.Bootstrap
{
    /// <summary>
    /// Creates colored materials at runtime that work whether the project uses the
    /// Built-in pipeline (Standard) or URP (Universal Render Pipeline/Lit). Because the
    /// whole game is generated in code, we never rely on a material asset existing.
    /// </summary>
    public static class MaterialFactory
    {
        static Shader _shader;
        static readonly Dictionary<Color, Material> _cache = new();

        static Shader LitShader()
        {
            if (_shader != null) return _shader;
            _shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard")
                      ?? Shader.Find("Legacy Shaders/Diffuse")
                      ?? Shader.Find("Sprites/Default");
            return _shader;
        }

        public static Material Solid(Color color, float smoothness = 0.2f, bool emissive = false)
        {
            if (!emissive && _cache.TryGetValue(color, out var cached)) return cached;

            var mat = new Material(LitShader());
            // set both property names so it looks right under either pipeline
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                Color e = color * 1.6f;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", e);
            }
            else _cache[color] = mat;

            return mat;
        }

        /// <summary>Applies a solid color to every renderer on a GameObject.</summary>
        public static GameObject Paint(GameObject go, Color color, float smoothness = 0.2f, bool emissive = false)
        {
            var mat = Solid(color, smoothness, emissive);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;
            return go;
        }
    }
}
