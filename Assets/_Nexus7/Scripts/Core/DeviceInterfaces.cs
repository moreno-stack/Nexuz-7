using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Core
{
    /// <summary>
    /// Algo que distrae a los enemigos (señuelo sonoro). Cuando un enemigo oye un ruido cuya fuente
    /// implementa esta interfaz, sabe que es un señuelo: va a inspeccionarlo, lo neutraliza y...
    /// si recibe otro poco después, deduce el engaño y busca en el punto desde donde se lanzó (Origin).
    /// </summary>
    public interface IDistraction
    {
        bool IsActive { get; }
        Vector3 Position { get; }
        /// <summary>Posición del jugador cuando lanzó el señuelo.</summary>
        Vector3 Origin { get; }
        void Neutralize();
    }

    /// <summary>
    /// Punto que los drones de investigación revisan durante su patrulla (bancos de trabajo, despachos).
    /// Si se usó hace poco ("manipulado"), el dron da la alarma.
    /// </summary>
    public interface IInspectable
    {
        string InspectionLabel { get; }
        Vector3 InspectionPosition { get; }
        bool WasTamperedRecently(float window);
        void ClearTamper();
    }

    /// <summary>Todo lo que un Pulso PEM puede inutilizar temporalmente (drones, cámaras, barreras, láseres).</summary>
    public interface IEmpTarget
    {
        Vector3 EmpPosition { get; }
        void OnEmp(float duration);
    }

    /// <summary>Registro global de objetivos PEM (las cámaras no tienen collider, así que no sirve un OverlapSphere).</summary>
    public static class EmpRegistry
    {
        private static readonly List<IEmpTarget> targets = new List<IEmpTarget>();
        public static IReadOnlyList<IEmpTarget> Targets => targets;

        public static void Register(IEmpTarget t) { if (t != null && !targets.Contains(t)) targets.Add(t); }
        public static void Unregister(IEmpTarget t) => targets.Remove(t);

        /// <summary>Aplica el PEM a todo lo que esté dentro del radio. Devuelve cuántos objetivos afectó.</summary>
        public static int Pulse(Vector3 center, float radius, float duration)
        {
            int hit = 0;
            // Copia: un objetivo podría darse de baja al recibir el pulso.
            foreach (var t in targets.ToArray())
            {
                if (t == null || Vector3.Distance(t.EmpPosition, center) > radius) continue;
                t.OnEmp(duration);
                hit++;
            }
            return hit;
        }
    }
}
