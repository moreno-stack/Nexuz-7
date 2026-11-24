using System;
using System.Collections.Generic;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Vidas del jugador (lógica pura, probada):
    ///   · 5 en Fácil, 3 en Normal y 2 en Difícil; se rellenan al entrar en una fase nueva;
    ///   · cada captura o caída cuesta una; despejar un encuentro de oleadas devuelve una (sin pasar del máximo);
    ///   · al quedarse sin vidas la fase se reinicia desde su inicio con las vidas llenas, y se cuenta el reinicio
    ///     (la variación de patrullas y oleadas lo usa para que el intento siguiente no sea igual).
    /// </summary>
    public sealed class LivesModel
    {
        public static int MaxFor(int difficulty) => difficulty <= 0 ? 5 : difficulty >= 2 ? 2 : 3;

        public int Max { get; private set; }
        public int Lives { get; private set; }
        public string Scene { get; private set; }
        public bool IsLastLife => Lives == 1;

        private readonly Dictionary<string, int> restarts = new Dictionary<string, int>();

        public LivesModel(int difficulty) { Max = MaxFor(difficulty); Lives = Max; }

        /// <summary>Entra en una escena: si es una fase distinta de la anterior, rellena las vidas.</summary>
        public void EnterScene(string scene, int difficulty)
        {
            Max = MaxFor(difficulty);
            if (scene != Scene) Lives = Max;
            else Lives = Math.Min(Lives, Max);
            Scene = scene;
        }

        /// <summary>Pierde una vida. true = se ha quedado sin vidas (hay que reiniciar la fase).</summary>
        public bool LoseLife()
        {
            if (Lives <= 0) return true;
            Lives--;
            return Lives == 0;
        }

        /// <summary>Gana una vida (máximo 'Max'). true si de verdad la ha ganado.</summary>
        public bool GainLife()
        {
            if (Lives >= Max) return false;
            Lives++;
            return true;
        }

        /// <summary>Reinicio de la fase por quedarse sin vidas: vidas llenas y un reinicio más en esta fase.</summary>
        public void RestartPhase()
        {
            Lives = Max;
            if (Scene == null) return;
            restarts.TryGetValue(Scene, out int n);
            restarts[Scene] = n + 1;
        }

        public int RestartsIn(string scene) => scene != null && restarts.TryGetValue(scene, out int n) ? n : 0;
    }
}
