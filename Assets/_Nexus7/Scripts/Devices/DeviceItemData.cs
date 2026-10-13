using UnityEngine;
using Nexus7.Inventory;

namespace Nexus7.Devices
{
    public enum DeviceKind { SonicDecoy, EmpPulse, OpticalCamo }
    public enum DeviceUseMode { Throw, SelfActivate }

    /// <summary>
    /// Objeto de inventario que además es un dispositivo utilizable. Hereda de ItemData, así que se
    /// guarda, apila, fabrica y muestra como cualquier otro objeto.
    ///
    ///   SonicDecoy  (Throw)        → al caer emite pulsos de ruido: Duration s, radio Radius, cada EffectValue s
    ///   EmpPulse    (Throw)        → al impactar aturde drones y apaga cámaras/barreras/láseres en Radius durante Duration s
    ///   OpticalCamo (SelfActivate) → visibilidad × EffectValue durante Duration s
    ///
    /// Crear: clic derecho en Project → Create → Nexus-7 → Dispositivo.
    /// </summary>
    [CreateAssetMenu(menuName = "Nexus-7/Dispositivo", fileName = "Item_Dispositivo")]
    public class DeviceItemData : ItemData
    {
        [Header("Dispositivo")]
        [SerializeField] private DeviceKind deviceType = DeviceKind.SonicDecoy;
        [SerializeField] private DeviceUseMode useMode = DeviceUseMode.Throw;
        [Tooltip("Tecla de la barra rápida (1, 2, 3...).")]
        [SerializeField, Min(1)] private int quickSlot = 1;
        [SerializeField] private float throwForce = 12f;
        [SerializeField] private float duration = 10f;
        [SerializeField] private float radius = 14f;
        [Tooltip("Señuelo: segundos entre pulsos. Camuflaje: multiplicador de visibilidad (0.1 = 10 %).")]
        [SerializeField] private float effectValue = 1.2f;
        [SerializeField] private Color deviceColor = new Color(1f, 0.6f, 0.1f);

        public DeviceKind Type => deviceType;
        public DeviceUseMode UseMode => useMode;
        public int QuickSlot => quickSlot;
        public float ThrowForce => throwForce;
        public float Duration => duration;
        public float Radius => radius;
        public float EffectValue => effectValue;
        public Color DeviceColor => deviceColor;
    }
}
