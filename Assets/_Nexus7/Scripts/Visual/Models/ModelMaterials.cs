using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Visual.Models
{
    /// <summary>
    /// Materiales compartidos de los modelos hechos por código: uno por (papel, color), creados una sola vez a partir
    /// del material por defecto de las primitivas (Lit de URP). Nunca se destruyen: los usan muchos objetos a la vez.
    /// </summary>
    public static class ModelMaterials
    {
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        private static Material baseMaterial;

        /// <summary>Material de partida (el de las primitivas). ModelFactory lo pasa la primera vez.</summary>
        public static void SetBase(Material m) { if (baseMaterial == null && m != null) baseMaterial = m; }

        public static Material For(MaterialRole role, MColor color)
        {
            string key = role + ":" + color.GetHashCode();
            if (cache.TryGetValue(key, out var mat) && mat != null) return mat;
            mat = Create();
            if (mat == null) return null;
            var c = new Color(color.r, color.g, color.b);
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            float metallic, smoothness, emission;
            switch (role)
            {
                case MaterialRole.Metal:  metallic = 0.9f; smoothness = 0.75f; emission = 0f; break;
                case MaterialRole.Grip:   metallic = 0f;   smoothness = 0.15f; emission = 0f; break;
                case MaterialRole.Accent: metallic = 0.2f; smoothness = 0.5f;  emission = 0.6f; break;
                case MaterialRole.Glow:   metallic = 0f;   smoothness = 0.8f;  emission = 2.5f; break;
                default:                  metallic = 0.5f; smoothness = 0.45f; emission = 0f; break;
            }
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(color.r * emission, color.g * emission, color.b * emission));
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            mat.name = "Modelo_" + role + "_" + ColorUtility.ToHtmlStringRGB(c);
            cache[key] = mat;
            return mat;
        }

        private static Material Create()
        {
            if (baseMaterial != null) return new Material(baseMaterial);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            return shader != null ? new Material(shader) : null;
        }
    }
}
