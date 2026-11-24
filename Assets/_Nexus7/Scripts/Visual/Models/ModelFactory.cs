using UnityEngine;

namespace Nexus7.Visual.Models
{
    /// <summary>
    /// Monta un ModelBlueprint en la escena: una primitiva por pieza, sin colisionadores, con materiales compartidos.
    /// Devuelve la raíz (hija de 'parent'); quien lo pide decide colisionador, luces y comportamiento.
    /// </summary>
    public static class ModelFactory
    {
        public static Transform Build(ModelBlueprint bp, Transform parent, bool castShadows)
        {
            var root = new GameObject(bp != null ? bp.Key : "Modelo").transform;
            root.SetParent(parent, false);
            if (bp == null) return root;
            foreach (var p in bp.Parts)
            {
                var go = GameObject.CreatePrimitive(ToUnity(p.Shape));
                if (go == null) continue;
                go.name = p.Role.ToString();
                var col = go.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;   // Destroy es diferido: que no choque en este fotograma
                    if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
                }
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(p.Position.x, p.Position.y, p.Position.z);
                go.transform.localRotation = Quaternion.Euler(p.Euler.x, p.Euler.y, p.Euler.z);
                var s = ModelScale.ForUnity(p.Shape, p.Size);
                go.transform.localScale = new Vector3(s.x, s.y, s.z);
                var r = go.GetComponent<Renderer>();
                if (r == null) continue;
                ModelMaterials.SetBase(r.sharedMaterial);
                var mat = ModelMaterials.For(p.Role, p.Color);
                if (mat != null) r.sharedMaterial = mat;
                r.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return root;
        }

        private static PrimitiveType ToUnity(ModelShape s)
        {
            switch (s)
            {
                case ModelShape.Cylinder: return PrimitiveType.Cylinder;
                case ModelShape.Sphere:   return PrimitiveType.Sphere;
                case ModelShape.Capsule:  return PrimitiveType.Capsule;
                default:                  return PrimitiveType.Cube;
            }
        }

        /// <summary>Color de Unity → color de los planos.</summary>
        public static MColor ToModel(Color c) => new MColor(c.r, c.g, c.b);
    }
}
