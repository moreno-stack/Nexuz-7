using System;
using System.Collections.Generic;

namespace Nexus7.Combat
{
    public enum WeaponState { Ready, Reloading, Overheated }

    public enum FireResult { Fired, Cooldown, NeedsRelease, Empty, Reloading, Overheated }

    /// <summary>
    /// Lógica de un arma (pura, probada en Tests/Suites/WeaponTest.cs): cargador, reserva, recarga, calor,
    /// cadencia y apertura de la dispersión («bloom»). No sabe nada de Unity: PlayerWeapon le pasa el tiempo.
    ///   · Cadencia: no dispara antes de 1/FireRate s. Semiautomática: hay que soltar el gatillo entre disparos.
    ///   · Calor: sube con cada disparo y baja a 40/s tras 0,25 s sin disparar. A 100 → sobrecalentada 1,5 s y vuelve a 50.
    ///   · Recarga: con [R] o sola al vaciar el cargador si hay reserva. Cambiar de arma la cancela sin perder munición.
    ///   · Accesorios (WeaponModModel): 'Spec' es el arma con sus accesorios aplicados; 'BaseSpec', la del catálogo.
    /// </summary>
    public sealed class WeaponModel
    {
        public const float MaxHeat = 100f;
        public const float CoolRate = 40f;
        public const float CoolDelay = 0.25f;
        public const float OverheatTime = 1.5f;
        public const float HeatAfterOverheat = 50f;
        public const float BloomRecovery = 4f;

        /// <summary>El arma con sus accesorios aplicados (es lo que usan el disparo, el HUD y el director).</summary>
        public PlayerWeaponSpec Spec { get; private set; }
        /// <summary>El arma tal como está en el catálogo.</summary>
        public PlayerWeaponSpec BaseSpec { get; }
        public IReadOnlyList<WeaponMod> Mods => mods;
        private readonly List<WeaponMod> mods = new List<WeaponMod>();
        public int Ammo { get; private set; }
        public int Reserve { get; private set; }
        public float Heat { get; private set; }
        public float Bloom { get; private set; }
        public WeaponState State { get; private set; } = WeaponState.Ready;
        /// <summary>Progreso de la recarga (0..1) para el HUD.</summary>
        public float ReloadProgress(float time) =>
            State == WeaponState.Reloading && Spec.ReloadTime > 0f ? Clamp01(1f - (reloadEnd - time) / Spec.ReloadTime) : 0f;

        public bool IsEmpty => Ammo <= 0 && Reserve <= 0;
        public bool CanReload => State == WeaponState.Ready && Ammo < Spec.MagazineSize && Reserve > 0;

        private float lastShot = -999f;
        private float reloadEnd, overheatEnd;
        private bool triggerWasDown;

        public WeaponModel(PlayerWeaponSpec spec, int ammo, int reserve, IList<WeaponMod> installed = null)
        {
            BaseSpec = spec ?? throw new ArgumentNullException(nameof(spec));
            if (installed != null)
                foreach (var m in installed) WeaponModModel.Install(mods, m, spec, out _);
            Spec = mods.Count > 0 ? WeaponModModel.Apply(spec, mods) : spec;
            Ammo = Math.Max(0, Math.Min(Spec.MagazineSize, ammo));
            Reserve = Math.Max(0, Math.Min(Spec.MaxReserve, reserve));
        }

        /// <summary>Arma nueva: cargador lleno y la reserva inicial del catálogo.</summary>
        public static WeaponModel Fresh(PlayerWeaponSpec spec) => new WeaponModel(spec, spec.MagazineSize, spec.StartReserve);

        /// <summary>
        /// Intenta disparar en el instante 'time' con el gatillo pulsado. Llamar cada fotograma con el estado del gatillo
        /// (triggerDown) para que la semiautomática sepa cuándo se ha soltado.
        /// </summary>
        public FireResult TryFire(float time, bool triggerDown)
        {
            bool pressedNow = triggerDown && !triggerWasDown;
            triggerWasDown = triggerDown;
            if (!triggerDown) return FireResult.Cooldown;
            if (State == WeaponState.Overheated) return FireResult.Overheated;
            if (State == WeaponState.Reloading) return FireResult.Reloading;
            if (Spec.Mode == FireMode.SemiAuto && !pressedNow) return FireResult.NeedsRelease;
            if (time - lastShot < Spec.FireInterval - 1e-4f) return FireResult.Cooldown;
            if (Ammo <= 0)
            {
                StartReload(time);
                return FireResult.Empty;
            }

            Ammo--;
            lastShot = time;
            Bloom = Math.Min(Spec.MaxBloom, Bloom + Spec.BloomPerShot);
            if (Spec.HeatPerShot > 0f)
            {
                Heat = Math.Min(MaxHeat, Heat + Spec.HeatPerShot);
                if (Heat >= MaxHeat) { State = WeaponState.Overheated; overheatEnd = time + OverheatTime; }
            }
            return FireResult.Fired;
        }

        /// <summary>Suelta el gatillo (al cambiar de arma o abrir un menú) para que la semiautomática no dispare sola.</summary>
        public void ReleaseTrigger() => triggerWasDown = false;

        public bool StartReload(float time)
        {
            if (!CanReload) return false;
            State = WeaponState.Reloading;
            reloadEnd = time + Spec.ReloadTime;
            return true;
        }

        public void CancelReload()
        {
            if (State == WeaponState.Reloading) State = WeaponState.Ready;
        }

        /// <summary>Avanza el tiempo: recarga, enfriamiento, fin del sobrecalentamiento y recuperación de la dispersión.</summary>
        public void Tick(float time, float dt)
        {
            if (dt < 0f) dt = 0f;
            Bloom = Math.Max(0f, Bloom - BloomRecovery * dt);

            if (State == WeaponState.Overheated)
            {
                if (time >= overheatEnd) { State = WeaponState.Ready; Heat = HeatAfterOverheat; }
            }
            else if (Heat > 0f && time - lastShot >= CoolDelay)
                Heat = Math.Max(0f, Heat - CoolRate * dt);

            if (State == WeaponState.Reloading && time >= reloadEnd)
            {
                int take = Math.Min(Spec.MagazineSize - Ammo, Reserve);
                Ammo += take;
                Reserve -= take;
                State = WeaponState.Ready;
            }

            // Cargador vacío y con reserva: recarga sola en cuanto termina la cadencia del último disparo
            if (State == WeaponState.Ready && Ammo == 0 && Reserve > 0 && time - lastShot >= Spec.FireInterval)
                StartReload(time);
        }

        /// <summary>Añade munición a la reserva (sin pasar del máximo). Devuelve cuánta se añadió.</summary>
        public int AddReserve(int amount)
        {
            if (amount <= 0) return 0;
            int added = Math.Min(amount, Spec.MaxReserve - Reserve);
            if (added < 0) added = 0;
            Reserve += added;
            return added;
        }

        /// <summary>
        /// Monta un accesorio. Cambia los valores del arma al momento; la munición que ya tiene se conserva
        /// (si el cargador encoge al quitar un cargador ampliado, lo que sobra vuelve a la reserva).
        /// </summary>
        public ModInstall InstallMod(WeaponMod mod, out WeaponMod replaced)
        {
            var result = WeaponModModel.Install(mods, mod, BaseSpec, out replaced);
            if (result != ModInstall.Installed && result != ModInstall.Replaced) return result;
            Spec = WeaponModModel.Apply(BaseSpec, mods);
            if (Ammo > Spec.MagazineSize)
            {
                int extra = Ammo - Spec.MagazineSize;
                Ammo = Spec.MagazineSize;
                Reserve = Math.Min(Spec.MaxReserve, Reserve + extra);
            }
            return result;
        }

        public bool HasMod(WeaponMod mod) => mods.Contains(mod);

        /// <summary>Quita munición de la reserva (penalización al caer). Devuelve cuánta se quitó.</summary>
        public int RemoveReserve(int amount)
        {
            int removed = Math.Max(0, Math.Min(amount, Reserve));
            Reserve -= removed;
            return removed;
        }

        /// <summary>Fracción de munición total respecto al máximo (cargador + reserva), para el director y los paquetes.</summary>
        public float AmmoFraction =>
            (float)(Ammo + Reserve) / Math.Max(1, Spec.MagazineSize + Spec.MaxReserve);

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
