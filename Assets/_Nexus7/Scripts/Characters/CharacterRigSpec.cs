using System.Collections.Generic;

namespace Nexus7.Characters
{
    /// <summary>Una pieza del modelo: articulación vacía o forma redondeada, relativa a su padre.</summary>
    public class RigPart
    {
        public string name, parent;
        public RigShape shape;
        public RigMaterial material;
        public float x, y, z;          // posición local
        public float sx = 1, sy = 1, sz = 1; // escala local (solo formas)
        public float rx, ry, rz;       // rotación local en grados
        public bool firstPerson;       // visible también en primera persona

        public RigPart(string name, string parent, RigShape shape, RigMaterial material, float x, float y, float z,
                       float sx = 1, float sy = 1, float sz = 1)
        {
            this.name = name; this.parent = parent; this.shape = shape; this.material = material;
            this.x = x; this.y = y; this.z = z; this.sx = sx; this.sy = sy; this.sz = sz;
        }
    }

    /// <summary>
    /// Descripción (lógica pura, probada con Mono) de los modelos redondeados: jugador (con o sin traje),
    /// los cuatro tipos de robot y sus armas. CharacterRigFactory la convierte en GameObjects.
    /// Alturas del jugador: caderas 0,95 m · cabeza 1,62 m · total ≈ 1,8 m.
    /// </summary>
    public static class CharacterRigSpec
    {
        private static RigPart J(string n, string p, float x, float y, float z) => new RigPart(n, p, RigShape.Joint, RigMaterial.Body, x, y, z);
        private static RigPart S(string n, string p, RigShape s, RigMaterial m, float x, float y, float z, float sx, float sy, float sz)
            => new RigPart(n, p, s, m, x, y, z, sx, sy, sz);

        // ---------------- Jugador ----------------
        public static List<RigPart> Player(bool withSuit)
        {
            var body = withSuit ? RigMaterial.Suit : RigMaterial.Body;
            var l = new List<RigPart>
            {
                J(RigJoints.Hips, null, 0f, 0.95f, 0f),
                S("Pelvis", RigJoints.Hips, RigShape.Capsule, body, 0f, 0f, 0f, 0.34f, 0.12f, 0.22f),
                J(RigJoints.Spine, RigJoints.Hips, 0f, 0.08f, 0f),
                S("Torso", RigJoints.Spine, RigShape.Capsule, body, 0f, 0.27f, 0f, 0.40f, 0.30f, 0.25f),
                S("Cinturon", RigJoints.Spine, RigShape.Cylinder, RigMaterial.Accent, 0f, 0.02f, 0f, 0.36f, 0.03f, 0.24f),
                J(RigJoints.Head, RigJoints.Spine, 0f, 0.59f, 0f),
                S("Cuello", RigJoints.Head, RigShape.Cylinder, withSuit ? RigMaterial.Accent : RigMaterial.Skin, 0f, -0.08f, 0f, 0.1f, 0.05f, 0.1f),
            };
            if (withSuit)
            {
                l.Add(S("Casco", RigJoints.Head, RigShape.Sphere, RigMaterial.Suit, 0f, 0.03f, 0f, 0.34f, 0.34f, 0.34f));
                l.Add(S("Visor", RigJoints.Head, RigShape.Sphere, RigMaterial.Visor, 0f, 0.04f, 0.08f, 0.26f, 0.18f, 0.2f));
                l.Add(S("Mochila", RigJoints.Spine, RigShape.Capsule, RigMaterial.Accent, 0f, 0.30f, -0.18f, 0.30f, 0.22f, 0.14f));
                l.Add(S("LuzMochila", RigJoints.Spine, RigShape.Sphere, RigMaterial.Glow, 0.09f, 0.42f, -0.25f, 0.05f, 0.05f, 0.05f));
            }
            else
            {
                l.Add(S("Cabeza", RigJoints.Head, RigShape.Sphere, RigMaterial.Skin, 0f, 0.02f, 0f, 0.28f, 0.28f, 0.28f));
                l.Add(S("Pelo", RigJoints.Head, RigShape.Sphere, RigMaterial.Accent, 0f, 0.07f, -0.02f, 0.29f, 0.2f, 0.29f));
            }
            AddArm(l, "L", -1f, body, RigMaterial.Accent, 0.26f, 0.48f, 1f);
            AddArm(l, "R", 1f, body, RigMaterial.Accent, 0.26f, 0.48f, 1f);
            AddLeg(l, "L", -1f, body, RigMaterial.Accent, 1f);
            AddLeg(l, "R", 1f, body, RigMaterial.Accent, 1f);
            return l;
        }

        // ---------------- Robots ----------------
        public static float RobotScale(RobotStyle s) =>
            s == RobotStyle.Guard ? 1.15f : s == RobotStyle.Elite ? 1.1f : s == RobotStyle.Research ? 0.95f : 1f;

        public static List<RigPart> Robot(RobotStyle s)
        {
            float wide = s == RobotStyle.Guard ? 1.25f : 1f;
            var l = new List<RigPart>
            {
                J(RigJoints.Hips, null, 0f, 0.95f, 0f),
                S("Pelvis", RigJoints.Hips, RigShape.Capsule, RigMaterial.Metal, 0f, 0f, 0f, 0.36f * wide, 0.12f, 0.24f),
                J(RigJoints.Spine, RigJoints.Hips, 0f, 0.08f, 0f),
                S("Torso", RigJoints.Spine, RigShape.Capsule, RigMaterial.Body, 0f, 0.28f, 0f, 0.46f * wide, 0.32f, 0.30f),
                S("Nucleo", RigJoints.Spine, RigShape.Sphere, RigMaterial.Glow, 0f, 0.32f, 0.14f, 0.1f, 0.1f, 0.05f),
                J(RigJoints.Head, RigJoints.Spine, 0f, 0.60f, 0f),
                S("Cuello", RigJoints.Head, RigShape.Cylinder, RigMaterial.Metal, 0f, -0.08f, 0f, 0.09f, 0.05f, 0.09f),
                S("Cabeza", RigJoints.Head, RigShape.Sphere, RigMaterial.Body, 0f, 0.04f, 0f, 0.30f, 0.26f, 0.30f),
                S("Eye", RigJoints.Head, RigShape.Sphere, RigMaterial.Eye, 0f, 0.05f, 0.13f, 0.14f, 0.07f, 0.06f),
                S("Antena", RigJoints.Head, RigShape.Cylinder, RigMaterial.Metal, 0.08f, 0.2f, -0.02f, 0.015f, 0.08f, 0.015f),
            };
            if (s == RobotStyle.Research)
                l.Add(S("VisorMorado", RigJoints.Head, RigShape.Sphere, RigMaterial.Accent, 0f, 0.05f, 0.1f, 0.26f, 0.1f, 0.12f));
            if (s == RobotStyle.Elite)
            {
                l.Add(S("HombreraL", RigJoints.Spine, RigShape.Sphere, RigMaterial.Accent, -0.3f, 0.48f, 0f, 0.22f, 0.16f, 0.24f));
                l.Add(S("HombreraR", RigJoints.Spine, RigShape.Sphere, RigMaterial.Accent, 0.3f, 0.48f, 0f, 0.22f, 0.16f, 0.24f));
                l.Add(S("Cresta", RigJoints.Head, RigShape.Capsule, RigMaterial.Accent, 0f, 0.17f, -0.02f, 0.05f, 0.08f, 0.22f));
            }
            if (s == RobotStyle.Guard)
                l.Add(S("Placa", RigJoints.Spine, RigShape.Capsule, RigMaterial.Accent, 0f, 0.3f, 0.13f, 0.34f, 0.18f, 0.06f));
            AddArm(l, "L", -1f, RigMaterial.Body, RigMaterial.Metal, 0.29f * wide, 0.48f, 1.05f);
            AddArm(l, "R", 1f, RigMaterial.Body, RigMaterial.Metal, 0.29f * wide, 0.48f, 1.05f);
            AddLeg(l, "L", -1f, RigMaterial.Body, RigMaterial.Metal, 1.05f);
            AddLeg(l, "R", 1f, RigMaterial.Body, RigMaterial.Metal, 1.05f);
            return l;
        }

        // ---------------- Armas (relativas a la mano; el mango sale hacia delante) ----------------
        public static List<RigPart> Weapon(WeaponShape w)
        {
            var l = new List<RigPart>();
            switch (w)
            {
                case WeaponShape.Bat:
                    l.Add(Rot(S("Mango", null, RigShape.Cylinder, RigMaterial.Accent, 0f, 0f, 0.12f, 0.04f, 0.14f, 0.04f), 90f));
                    l.Add(Rot(S("Barra", null, RigShape.Capsule, RigMaterial.Metal, 0f, 0f, 0.5f, 0.08f, 0.3f, 0.08f), 90f));
                    break;
                case WeaponShape.Wrench:
                    l.Add(Rot(S("Mango", null, RigShape.Cylinder, RigMaterial.Metal, 0f, 0f, 0.3f, 0.05f, 0.3f, 0.05f), 90f));
                    l.Add(S("Boca", null, RigShape.Cylinder, RigMaterial.Metal, 0f, 0f, 0.66f, 0.2f, 0.04f, 0.2f));
                    l.Add(S("Hueco", null, RigShape.Cylinder, RigMaterial.Accent, 0f, 0.001f, 0.72f, 0.09f, 0.045f, 0.09f));
                    break;
                case WeaponShape.Baton:
                    l.Add(Rot(S("Mango", null, RigShape.Cylinder, RigMaterial.Accent, 0f, 0f, 0.1f, 0.04f, 0.1f, 0.04f), 90f));
                    l.Add(Rot(S("Varilla", null, RigShape.Cylinder, RigMaterial.Metal, 0f, 0f, 0.38f, 0.03f, 0.2f, 0.03f), 90f));
                    l.Add(S("Punta", null, RigShape.Sphere, RigMaterial.Glow, 0f, 0f, 0.6f, 0.07f, 0.07f, 0.07f));
                    break;
                default: // Axe
                    l.Add(Rot(S("Mango", null, RigShape.Cylinder, RigMaterial.Accent, 0f, 0f, 0.35f, 0.04f, 0.38f, 0.04f), 90f));
                    l.Add(S("Hoja", null, RigShape.Capsule, RigMaterial.Metal, 0f, 0.14f, 0.68f, 0.04f, 0.16f, 0.2f));
                    l.Add(S("Filo", null, RigShape.Capsule, RigMaterial.Glow, 0f, 0.28f, 0.68f, 0.02f, 0.05f, 0.18f));
                    break;
            }
            return l;
        }

        // ---------------- Extremidades ----------------
        private static void AddArm(List<RigPart> l, string side, float sign, RigMaterial upper, RigMaterial hand, float shoulderX, float shoulderY, float k)
        {
            string up = side == "L" ? RigJoints.LUpperArm : RigJoints.RUpperArm;
            string fore = side == "L" ? RigJoints.LForearm : RigJoints.RForearm;
            string hn = side == "L" ? RigJoints.LHand : RigJoints.RHand;
            l.Add(J(up, RigJoints.Spine, sign * shoulderX, shoulderY, 0f));
            l.Add(S("Hombro" + side, up, RigShape.Sphere, upper, 0f, 0f, 0f, 0.13f * k, 0.13f * k, 0.13f * k));
            l.Add(S("Brazo" + side, up, RigShape.Capsule, upper, 0f, -0.15f * k, 0f, 0.11f * k, 0.16f * k, 0.11f * k));
            l.Add(J(fore, up, 0f, -0.30f * k, 0f));
            var fa = S("Antebrazo" + side, fore, RigShape.Capsule, upper, 0f, -0.14f * k, 0f, 0.10f * k, 0.15f * k, 0.10f * k);
            fa.firstPerson = true; l.Add(fa);
            l.Add(J(hn, fore, 0f, -0.29f * k, 0f));
            var h = S("Mano" + side, hn, RigShape.Sphere, hand, 0f, 0f, 0f, 0.1f * k, 0.1f * k, 0.1f * k);
            h.firstPerson = true; l.Add(h);
        }

        private static void AddLeg(List<RigPart> l, string side, float sign, RigMaterial leg, RigMaterial foot, float k)
        {
            string th = side == "L" ? RigJoints.LThigh : RigJoints.RThigh;
            string sh = side == "L" ? RigJoints.LShin : RigJoints.RShin;
            l.Add(J(th, RigJoints.Hips, sign * 0.11f, -0.05f, 0f));
            l.Add(S("Muslo" + side, th, RigShape.Capsule, leg, 0f, -0.22f, 0f, 0.14f * k, 0.24f, 0.14f * k));
            l.Add(J(sh, th, 0f, -0.44f, 0f));
            l.Add(S("Rodilla" + side, sh, RigShape.Sphere, foot, 0f, 0f, 0.02f, 0.12f * k, 0.12f * k, 0.12f * k));
            l.Add(S("Pierna" + side, sh, RigShape.Capsule, leg, 0f, -0.21f, 0f, 0.12f * k, 0.23f, 0.12f * k));
            l.Add(S("Pie" + side, sh, RigShape.Capsule, foot, 0f, -0.44f, 0.05f, 0.13f * k, 0.07f, 0.22f));
        }

        private static RigPart Rot(RigPart p, float rx) { p.rx = rx; return p; }
    }
}
