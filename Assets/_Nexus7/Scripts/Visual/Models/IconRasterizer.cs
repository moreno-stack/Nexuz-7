using System;

namespace Nexus7.Visual.Models
{
    /// <summary>
    /// Dibuja el icono de un plano con un trazador de rayos diminuto (lógica pura, sin cámaras ni texturas de Unity):
    /// vista 3/4 ortográfica (giro 35°, inclinación 25°), encuadre automático, luz difusa, brillo en el metal,
    /// piezas Glow casi sin sombra y un contorno oscuro de 1 píxel. Devuelve RGBA en [0, 1], fila 0 abajo.
    /// </summary>
    public static class IconRasterizer
    {
        public const float Yaw = 35f, Pitch = 25f, Margin = 0.1f;
        private static readonly MVec Light = new MVec(-0.45f, 0.75f, -0.5f).Normalized;   // en el espacio de la vista
        private static readonly MVec ViewEuler = new MVec(Pitch, Yaw, 0f);

        public static float[] Render(ModelBlueprint bp, int size)
        {
            var px = new float[size * size * 4];
            if (bp == null || bp.Parts.Count == 0 || size <= 0) return px;

            // Encuadre: límites del modelo en el espacio de la vista
            float minU = float.MaxValue, maxU = float.MinValue, minV = float.MaxValue, maxV = float.MinValue;
            foreach (var p in bp.Parts)
            {
                var h = p.Size * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    var c = new MVec((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
                    var v = ToView(MVec.Rotate(c, p.Euler) + p.Position);
                    minU = Math.Min(minU, v.x); maxU = Math.Max(maxU, v.x);
                    minV = Math.Min(minV, v.y); maxV = Math.Max(maxV, v.y);
                }
            }
            float extent = Math.Max(maxU - minU, maxV - minV) / (1f - 2f * Margin);
            if (extent <= 1e-6f) return px;
            float cu = (minU + maxU) * 0.5f, cv = (minV + maxV) * 0.5f;

            // Datos de cada pieza calculados una vez (el rayo es lineal en u, v): origen y dirección en su espacio
            // y el rectángulo que ocupa en la vista, para descartarla rápido en los píxeles que no toca.
            int n = bp.Parts.Count;
            var o0 = new MVec[n]; var du = new MVec[n]; var dv = new MVec[n]; var dl = new MVec[n];
            var rect = new float[n * 4];
            MVec dirWorld = FromView(new MVec(0f, 0f, 1f)), uAxis = FromView(new MVec(1f, 0f, 0f)), vAxis = FromView(new MVec(0f, 1f, 0f));
            MVec back = FromView(new MVec(0f, 0f, -10f));
            for (int k = 0; k < n; k++)
            {
                var p = bp.Parts[k];
                o0[k] = MVec.InverseRotate(back - p.Position, p.Euler);
                du[k] = MVec.InverseRotate(uAxis, p.Euler);
                dv[k] = MVec.InverseRotate(vAxis, p.Euler);
                dl[k] = MVec.InverseRotate(dirWorld, p.Euler);
                float a = float.MaxValue, b2 = float.MinValue, c = float.MaxValue, d = float.MinValue;
                var h = p.Size * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new MVec((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
                    var v = ToView(MVec.Rotate(corner, p.Euler) + p.Position);
                    a = Math.Min(a, v.x); b2 = Math.Max(b2, v.x); c = Math.Min(c, v.y); d = Math.Max(d, v.y);
                }
                rect[k * 4] = a; rect[k * 4 + 1] = b2; rect[k * 4 + 2] = c; rect[k * 4 + 3] = d;
            }

            var hit = new bool[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = cu + ((x + 0.5f) / size - 0.5f) * extent;
                    float v = cv + ((y + 0.5f) / size - 0.5f) * extent;
                    float best = float.MaxValue; int bestPart = -1; MVec bestNormal = default(MVec);
                    for (int k = 0; k < n; k++)
                    {
                        if (u < rect[k * 4] || u > rect[k * 4 + 1] || v < rect[k * 4 + 2] || v > rect[k * 4 + 3]) continue;
                        var p = bp.Parts[k];
                        MVec o = o0[k] + du[k] * u + dv[k] * v;
                        if (Intersect(p.Shape, p.Size * 0.5f, o, dl[k], out float t, out MVec nrm) && t < best)
                        {
                            best = t; bestPart = k; bestNormal = nrm;
                        }
                    }
                    if (bestPart < 0) continue;
                    hit[y * size + x] = true;
                    var bpPart = bp.Parts[bestPart];
                    Shade(bpPart, ToViewDir(MVec.Rotate(bestNormal, bpPart.Euler)).Normalized, px, (y * size + x) * 4);
                }

            // Contorno: los píxeles vacíos junto al objeto se pintan casi negros
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    if (hit[i]) continue;
                    bool near = (x > 0 && hit[i - 1]) || (x < size - 1 && hit[i + 1]) || (y > 0 && hit[i - size]) || (y < size - 1 && hit[i + size]);
                    if (!near) continue;
                    px[i * 4] = 0.03f; px[i * 4 + 1] = 0.04f; px[i * 4 + 2] = 0.05f; px[i * 4 + 3] = 1f;
                }
            return px;
        }

        // ------------------------------------------------------------------ vista
        private static MVec ToView(MVec w) => MVec.InverseRotate(w, ViewEuler);
        private static MVec ToViewDir(MVec w) => MVec.InverseRotate(w, ViewEuler);
        private static MVec FromView(MVec v) => MVec.Rotate(v, ViewEuler);

        private static void Shade(ModelPart p, MVec n, float[] px, int i)
        {
            if (n.z > 0f) n = n * -1f;   // la cara que mira a la cámara
            float diff = Math.Max(0f, MVec.Dot(n, Light));
            MColor c = p.Color;
            float r, g, b;
            if (p.Role == MaterialRole.Glow)
            {
                float k = 0.85f + 0.15f * diff;
                r = c.r * k + 0.15f; g = c.g * k + 0.15f; b = c.b * k + 0.15f;
            }
            else
            {
                float k = 0.35f + 0.65f * diff;
                if (p.Role == MaterialRole.Accent) k = 0.55f + 0.45f * diff;   // emisiva suave
                r = c.r * k; g = c.g * k; b = c.b * k;
                if (p.Role == MaterialRole.Metal)
                {
                    // brillo especular: reflejo de la luz hacia la cámara (vista hacia -z)
                    MVec refl = n * (2f * MVec.Dot(n, Light)) - Light;
                    float spec = (float)Math.Pow(Math.Max(0f, -refl.z), 16) * 0.35f;
                    r += spec; g += spec; b += spec;
                }
            }
            px[i] = Clamp01(r); px[i + 1] = Clamp01(g); px[i + 2] = Clamp01(b); px[i + 3] = 1f;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        // ------------------------------------------------------------------ intersecciones (espacio de la pieza)
        private static bool Intersect(ModelShape shape, MVec h, MVec o, MVec d, out float t, out MVec n)
        {
            switch (shape)
            {
                case ModelShape.Sphere:   return Ellipsoid(h, o, d, out t, out n);
                case ModelShape.Cylinder: return Cylinder(h.x, h.y, h.z, o, d, out t, out n);
                case ModelShape.Capsule:  return Capsule(h, o, d, out t, out n);
                default:                  return Box(h, o, d, out t, out n);
            }
        }

        private static bool Box(MVec h, MVec o, MVec d, out float t, out MVec n)
        {
            float tmin = float.MinValue, tmax = float.MaxValue; int axis = -1; float sign = 0f;
            float[] oo = { o.x, o.y, o.z }, dd = { d.x, d.y, d.z }, hh = { h.x, h.y, h.z };
            for (int a = 0; a < 3; a++)
            {
                if (Math.Abs(dd[a]) < 1e-8f)
                {
                    if (oo[a] < -hh[a] || oo[a] > hh[a]) { t = 0f; n = default(MVec); return false; }
                    continue;
                }
                float t1 = (-hh[a] - oo[a]) / dd[a], t2 = (hh[a] - oo[a]) / dd[a];
                float s = -1f;
                if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; s = 1f; }
                if (t1 > tmin) { tmin = t1; axis = a; sign = s; }
                if (t2 < tmax) tmax = t2;
                if (tmin > tmax) { t = 0f; n = default(MVec); return false; }
            }
            t = tmin; n = new MVec(axis == 0 ? sign : 0f, axis == 1 ? sign : 0f, axis == 2 ? sign : 0f);
            return axis >= 0 && tmax > 0f;
        }

        private static bool Ellipsoid(MVec h, MVec o, MVec d, out float t, out MVec n)
        {
            var os = new MVec(o.x / h.x, o.y / h.y, o.z / h.z);
            var ds = new MVec(d.x / h.x, d.y / h.y, d.z / h.z);
            float a = MVec.Dot(ds, ds), b = 2f * MVec.Dot(os, ds), c = MVec.Dot(os, os) - 1f;
            float disc = b * b - 4f * a * c;
            if (disc < 0f || a < 1e-12f) { t = 0f; n = default(MVec); return false; }
            t = (-b - (float)Math.Sqrt(disc)) / (2f * a);
            var p = o + d * t;
            n = new MVec(p.x / (h.x * h.x), p.y / (h.y * h.y), p.z / (h.z * h.z)).Normalized;
            return true;
        }

        private static bool Cylinder(float rx, float hy, float rz, MVec o, MVec d, out float t, out MVec n)
        {
            t = float.MaxValue; n = default(MVec);
            // pared
            float ox = o.x / rx, oz = o.z / rz, dx = d.x / rx, dz = d.z / rz;
            float a = dx * dx + dz * dz, b = 2f * (ox * dx + oz * dz), c = ox * ox + oz * oz - 1f;
            if (a > 1e-12f)
            {
                float disc = b * b - 4f * a * c;
                if (disc >= 0f)
                {
                    float tw = (-b - (float)Math.Sqrt(disc)) / (2f * a);
                    float y = o.y + d.y * tw;
                    if (y >= -hy && y <= hy) { t = tw; var p = o + d * tw; n = new MVec(p.x / (rx * rx), 0f, p.z / (rz * rz)).Normalized; }
                }
            }
            // tapas
            if (Math.Abs(d.y) > 1e-8f)
                for (int s = -1; s <= 1; s += 2)
                {
                    float tc = (s * hy - o.y) / d.y;
                    if (tc >= t) continue;
                    var p = o + d * tc;
                    if ((p.x / rx) * (p.x / rx) + (p.z / rz) * (p.z / rz) <= 1f) { t = tc; n = new MVec(0f, s, 0f); }
                }
            return t < float.MaxValue;
        }

        private static bool Capsule(MVec h, MVec o, MVec d, out float t, out MVec n)
        {
            float ry = Math.Min(Math.Min(h.x, h.z), h.y);
            float half = Math.Max(0f, h.y - ry);
            bool any = Cylinder(h.x, half, h.z, o, d, out t, out n);
            for (int s = -1; s <= 1; s += 2)
            {
                var c = new MVec(0f, s * half, 0f);
                if (Ellipsoid(new MVec(h.x, ry, h.z), o - c, d, out float te, out MVec ne) && (!any || te < t)) { t = te; n = ne; any = true; }
            }
            return any;
        }
    }
}
