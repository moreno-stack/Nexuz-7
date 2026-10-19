using System.Collections.Generic;

namespace Nexus7.Tutorial
{
    /// <summary>Un paso del tutorial: identificador, pista en pantalla y segundos que hay que mantener la acción.</summary>
    public class TutorialStep
    {
        public readonly string id;
        public readonly string hint;
        public readonly float hold;
        public TutorialStep(string id, string hint, float hold) { this.id = id; this.hint = hint; this.hold = hold; }
    }

    /// <summary>Pasos del tutorial de la Fase 1, en orden. La condición de cada id está en TutorialManager.IsStepDone.</summary>
    public static class TutorialSteps
    {
        public static readonly IReadOnlyList<TutorialStep> Phase1 = new List<TutorialStep>
        {
            new TutorialStep("move",     "Usa [W][A][S][D] o el stick izquierdo para moverte. Mueve el ratón para mirar.", 1.2f),
            new TutorialStep("run",      "Mantén [Shift] (L3 en el mando) para correr. Cuidado: correr hace MUCHO ruido.", 0.8f),
            new TutorialStep("crouch",   "Pulsa [Ctrl] o [C] (B en el mando) para agacharte. Agachado avanzas lento, pero casi en silencio.", 1f),
            new TutorialStep("jump",     "Ponte de pie ([Ctrl]/[C] otra vez) y pulsa [Espacio] (A en el mando) para saltar. Al aterrizar haces ruido.", 0f),
            new TutorialStep("camera",   "Pulsa [V] (View en el mando) para cambiar entre cámara en 1ª y 3ª persona.", 0f),
            new TutorialStep("interact", "Apunta a un objeto cercano y pulsa [E] (X en el mando) para interactuar. Prueba con la bitácora de la pared.", 0f),
            new TutorialStep("suit",     "Ponte el traje espacial de la taquilla junto a la cápsula: apunta y pulsa [E].", 0f),
        };
    }
}
