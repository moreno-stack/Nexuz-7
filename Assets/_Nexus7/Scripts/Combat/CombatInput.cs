namespace Nexus7.Combat
{
    /// <summary>
    /// Gatillo analógico como botón (lógica pura, probada): se pulsa al pasar de 'Press' y se suelta al bajar de 'Release'.
    /// La histéresis evita que un gatillo apretado a medias «tiemble» entre pulsado y suelto (y dispare la semiautomática sola).
    /// </summary>
    public sealed class TriggerLatch
    {
        public const float Press = 0.5f, Release = 0.3f;
        public bool Held { get; private set; }

        /// <summary>Actualiza con el valor del eje (0..1; algunos mandos dan −1..1 y se toma el valor absoluto).</summary>
        public bool Update(float value)
        {
            float v = value < 0f ? -value : value;
            if (!Held && v >= Press) Held = true;
            else if (Held && v <= Release) Held = false;
            return Held;
        }
    }

    /// <summary>
    /// Cruceta como teclas (lógica pura, probada): avisa solo en el fotograma en que se pulsa cada dirección,
    /// no mientras se mantiene (si no, mantener → derecha cambiaría de arma cada fotograma).
    /// </summary>
    public sealed class DpadEdges
    {
        public const float Threshold = 0.5f;
        private int lastX, lastY;

        public bool Left { get; private set; }
        public bool Right { get; private set; }
        public bool Up { get; private set; }
        public bool Down { get; private set; }

        public void Update(float x, float y)
        {
            int cx = x >= Threshold ? 1 : x <= -Threshold ? -1 : 0;
            int cy = y >= Threshold ? 1 : y <= -Threshold ? -1 : 0;
            Right = cx == 1 && lastX != 1;
            Left = cx == -1 && lastX != -1;
            Up = cy == 1 && lastY != 1;
            Down = cy == -1 && lastY != -1;
            lastX = cx;
            lastY = cy;
        }
    }
}
