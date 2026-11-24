using System;
using System.Collections.Generic;

namespace Nexus7.Visual.Models
{
    /// <summary>Vector propio de los planos (lógica pura: se prueba fuera de Unity).</summary>
    public struct MVec
    {
        public float x, y, z;
        public MVec(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static MVec operator +(MVec a, MVec b) => new MVec(a.x + b.x, a.y + b.y, a.z + b.z);
        public static MVec operator -(MVec a, MVec b) => new MVec(a.x - b.x, a.y - b.y, a.z - b.z);
        public static MVec operator *(MVec a, float f) => new MVec(a.x * f, a.y * f, a.z * f);
        public static float Dot(MVec a, MVec b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public float Length => (float)Math.Sqrt(x * x + y * y + z * z);
        public MVec Normalized { get { float l = Length; return l > 1e-6f ? this * (1f / l) : this; } }
        public static MVec Min(MVec a, MVec b) => new MVec(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z));
        public static MVec Max(MVec a, MVec b) => new MVec(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z));

        /// <summary>Gira como Unity con ángulos de Euler en grados: primero Z, luego X y por último Y.</summary>
        public static MVec Rotate(MVec v, MVec euler)
        {
            double rz = euler.z * Math.PI / 180.0, rx = euler.x * Math.PI / 180.0, ry = euler.y * Math.PI / 180.0;
            double x = v.x * Math.Cos(rz) - v.y * Math.Sin(rz), y = v.x * Math.Sin(rz) + v.y * Math.Cos(rz), z = v.z;
            double y2 = y * Math.Cos(rx) - z * Math.Sin(rx), z2 = y * Math.Sin(rx) + z * Math.Cos(rx);
            double x3 = x * Math.Cos(ry) + z2 * Math.Sin(ry), z3 = -x * Math.Sin(ry) + z2 * Math.Cos(ry);
            return new MVec((float)x3, (float)y2, (float)z3);
        }

        /// <summary>Giro inverso de <see cref="Rotate"/> (Y, luego X y luego Z con el ángulo cambiado de signo).</summary>
        public static MVec InverseRotate(MVec v, MVec euler)
        {
            double rz = -euler.z * Math.PI / 180.0, rx = -euler.x * Math.PI / 180.0, ry = -euler.y * Math.PI / 180.0;
            double x = v.x * Math.Cos(ry) + v.z * Math.Sin(ry), z = -v.x * Math.Sin(ry) + v.z * Math.Cos(ry), y = v.y;
            double y2 = y * Math.Cos(rx) - z * Math.Sin(rx), z2 = y * Math.Sin(rx) + z * Math.Cos(rx);
            double x3 = x * Math.Cos(rz) - y2 * Math.Sin(rz), y3 = x * Math.Sin(rz) + y2 * Math.Cos(rz);
            return new MVec((float)x3, (float)y3, (float)z2);
        }
    }

    /// <summary>Color propio de los planos (sin depender de UnityEngine.Color).</summary>
    public struct MColor : IEquatable<MColor>
    {
        public float r, g, b;
        public MColor(float r, float g, float b) { this.r = r; this.g = g; this.b = b; }
        public MColor Scale(float f) => new MColor(r * f, g * f, b * f);
        public bool Equals(MColor o) => Math.Abs(r - o.r) < 1e-4f && Math.Abs(g - o.g) < 1e-4f && Math.Abs(b - o.b) < 1e-4f;
        public override bool Equals(object o) => o is MColor c && Equals(c);
        public override int GetHashCode() => (int)(r * 255) * 65536 + (int)(g * 255) * 256 + (int)(b * 255);
    }

    /// <summary>Forma de una pieza. El cilindro y la cápsula tienen el eje en Y (como en Unity).</summary>
    public enum ModelShape { Box, Cylinder, Sphere, Capsule }

    /// <summary>
    /// Papel del material: carcasa oscura, acero claro, goma negra, franja del color del objeto (emisiva suave)
    /// o pieza que brilla mucho (pantallas, núcleos, bobinas).
    /// </summary>
    public enum MaterialRole { Body, Metal, Grip, Accent, Glow }

    /// <summary>Una pieza del modelo. 'Size' es el tamaño real en metros en los ejes de la pieza.</summary>
    public struct ModelPart
    {
        public ModelShape Shape;
        public MVec Position, Euler, Size;
        public MaterialRole Role;
        public MColor Color;
    }

    /// <summary>
    /// Plano de un modelo hecho por código: lista de piezas con forma, posición, giro, tamaño y material.
    /// Lógica pura: ModelFactory lo monta en Unity e IconRasterizer dibuja su icono.
    /// </summary>
    public sealed class ModelBlueprint
    {
        public static readonly MColor BodyColor = new MColor(0.2f, 0.22f, 0.25f);
        public static readonly MColor MetalColor = new MColor(0.58f, 0.61f, 0.66f);
        public static readonly MColor GripColor = new MColor(0.07f, 0.07f, 0.08f);

        public string Key { get; }
        /// <summary>Color propio del objeto (franjas Accent y piezas Glow).</summary>
        public MColor Accent { get; }
        public List<ModelPart> Parts { get; } = new List<ModelPart>();

        public ModelBlueprint(string key, MColor accent) { Key = key; Accent = accent; }

        public static MColor DefaultColor(MaterialRole role, MColor accent)
        {
            switch (role)
            {
                case MaterialRole.Body:  return BodyColor;
                case MaterialRole.Metal: return MetalColor;
                case MaterialRole.Grip:  return GripColor;
                default:                 return accent;
            }
        }

        /// <summary>Añade una pieza con el color por defecto de su papel.</summary>
        public ModelBlueprint Add(ModelShape shape, MVec position, MVec size, MaterialRole role, MVec euler = default(MVec))
            => AddColored(shape, position, size, role, DefaultColor(role, Accent), euler);

        /// <summary>Añade una pieza con un color concreto (p. ej. la placa verde del circuito).</summary>
        public ModelBlueprint AddColored(ModelShape shape, MVec position, MVec size, MaterialRole role, MColor color, MVec euler = default(MVec))
        {
            Parts.Add(new ModelPart { Shape = shape, Position = position, Size = size, Role = role, Color = color, Euler = euler });
            return this;
        }

        /// <summary>Esquina mínima de la caja que contiene todas las piezas (con su giro).</summary>
        public MVec MinCorner() { Corners(out MVec mn, out _); return mn; }
        /// <summary>Esquina máxima de la caja que contiene todas las piezas (con su giro).</summary>
        public MVec MaxCorner() { Corners(out _, out MVec mx); return mx; }

        private void Corners(out MVec mn, out MVec mx)
        {
            mn = new MVec(float.MaxValue, float.MaxValue, float.MaxValue);
            mx = new MVec(float.MinValue, float.MinValue, float.MinValue);
            if (Parts.Count == 0) { mn = mx = default(MVec); return; }
            foreach (var p in Parts)
            {
                var h = p.Size * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    var c = new MVec((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
                    var w = MVec.Rotate(c, p.Euler) + p.Position;
                    mn = MVec.Min(mn, w); mx = MVec.Max(mx, w);
                }
            }
        }

        /// <summary>Copia con posiciones y tamaños multiplicados (los imprescindibles se ven más grandes).</summary>
        public ModelBlueprint Scaled(float f)
        {
            var b = new ModelBlueprint(Key, Accent);
            foreach (var p in Parts)
            {
                var q = p; q.Position = p.Position * f; q.Size = p.Size * f;
                b.Parts.Add(q);
            }
            return b;
        }
    }

    /// <summary>Escala que hay que dar a la primitiva de Unity para que mida 'size' (cilindro y cápsula miden 2 de alto).</summary>
    public static class ModelScale
    {
        public static MVec ForUnity(ModelShape shape, MVec size)
            => shape == ModelShape.Cylinder || shape == ModelShape.Capsule ? new MVec(size.x, size.y * 0.5f, size.z) : size;
    }
}
