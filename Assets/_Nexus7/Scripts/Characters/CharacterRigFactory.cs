using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus7.Characters
{
    /// <summary>Materiales de un modelo, por papel (cuerpo, detalle, traje, visor, ojo...).</summary>
    [System.Serializable]
    public class RigMaterials
    {
        public Material body, accent, suit, visor, eye, skin, metal, glow;

        public Material Get(RigMaterial m)
        {
            Material r;
            switch (m)
            {
                case RigMaterial.Accent: r = accent; break;
                case RigMaterial.Suit:   r = suit; break;
                case RigMaterial.Visor:  r = visor; break;
                case RigMaterial.Eye:    r = eye; break;
                case RigMaterial.Skin:   r = skin; break;
                case RigMaterial.Metal:  r = metal; break;
                case RigMaterial.Glow:   r = glow; break;
                default:                 r = body; break;
            }
            return r != null ? r : body;
        }
    }

    /// <summary>
    /// Construye los modelos redondeados (sin colliders) a partir de CharacterRigSpec.
    /// Lo usan los constructores de escena (editor) y SuitPickup (en juego, para cambiar al traje).
    /// La raíz devuelta se llama "Modelo" y cuelga de 'parent' con los pies en su origen.
    /// </summary>
    public static class CharacterRigFactory
    {
        public static Transform BuildPlayer(Transform parent, bool withSuit, RigMaterials mats) =>
            Build(parent, "Modelo", CharacterRigSpec.Player(withSuit), mats, 1f);

        public static Transform BuildRobot(Transform parent, RobotStyle style, RigMaterials mats) =>
            Build(parent, "Modelo", CharacterRigSpec.Robot(style), mats, CharacterRigSpec.RobotScale(style));

        public static Transform BuildWeapon(Transform hand, WeaponShape shape, RigMaterials mats)
        {
            var root = Build(hand, "Arma_" + shape, CharacterRigSpec.Weapon(shape), mats, 1f);
            root.localRotation = Quaternion.Euler(-80f, 0f, 0f);   // el arma apunta hacia delante con el brazo colgando
            return root;
        }

        public static Transform Build(Transform parent, string rootName, List<RigPart> parts, RigMaterials mats, float scale)
        {
            var root = new GameObject(rootName).transform;
            root.SetParent(parent, false);
            root.localScale = new Vector3(scale, scale, scale);

            var made = new Dictionary<string, Transform>();
            foreach (var p in parts)
            {
                GameObject go;
                if (p.shape == RigShape.Joint) go = new GameObject(p.name);
                else
                {
                    go = GameObject.CreatePrimitive(ToPrimitive(p.shape));
                    go.name = p.name;
                    var col = go.GetComponent<Collider>();
                    if (col != null) { col.enabled = false; DestroyNow(col); }   // en juego Destroy es diferido: que no choque ese frame
                    var r = go.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.sharedMaterial = mats != null ? mats.Get(p.material) : null;
                        r.shadowCastingMode = p.material == RigMaterial.Glow || p.material == RigMaterial.Eye ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    }
                }
                Transform parentT = root;
                Transform found;
                if (!string.IsNullOrEmpty(p.parent) && made.TryGetValue(p.parent, out found)) parentT = found;
                go.transform.SetParent(parentT, false);
                go.transform.localPosition = new Vector3(p.x, p.y, p.z);
                go.transform.localRotation = Quaternion.Euler(p.rx, p.ry, p.rz);
                if (p.shape != RigShape.Joint) go.transform.localScale = new Vector3(p.sx, p.sy, p.sz);
                made[p.name] = go.transform;
            }
            return root;
        }

        /// <summary>Partes visibles en primera persona (antebrazos y manos) de un modelo construido.</summary>
        public static Renderer[] FirstPersonRenderers(Transform modelRoot, List<RigPart> spec)
        {
            var list = new List<Renderer>();
            foreach (var p in spec)
            {
                if (!p.firstPerson) continue;
                var t = FindDeep(modelRoot, p.name);
                var r = t != null ? t.GetComponent<Renderer>() : null;
                if (r != null) list.Add(r);
            }
            return list.ToArray();
        }

        public static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        private static PrimitiveType ToPrimitive(RigShape s) =>
            s == RigShape.Sphere ? PrimitiveType.Sphere : s == RigShape.Cylinder ? PrimitiveType.Cylinder
          : s == RigShape.Box ? PrimitiveType.Cube : PrimitiveType.Capsule;

        private static void DestroyNow(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }
}
