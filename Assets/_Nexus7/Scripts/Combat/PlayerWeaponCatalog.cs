using System;
using System.Collections.Generic;

namespace Nexus7.Combat
{
    public enum FireMode { SemiAuto, FullAuto }

    /// <summary>
    /// Datos de un arma del jugador. Todo lo que se equilibra está aquí (ver docs/guias/mejoras/GUIA_ARMAS_DIRECTOR.md).
    /// Clase de datos pura: se puede crear en las pruebas sin Unity. Se llama PlayerWeaponSpec porque
    /// WeaponData ya es el arma cuerpo a cuerpo de los robots.
    /// </summary>
    [Serializable]
    public sealed class PlayerWeaponSpec
    {
        public string Id;
        public string DisplayName;
        public FireMode Mode;
        /// <summary>Daño de cada proyectil (la escopeta dispara varios).</summary>
        public float Damage;
        /// <summary>Proyectiles por disparo (1 = normal; escopeta = 7).</summary>
        public int Pellets = 1;
        /// <summary>Disparos por segundo (cadencia).</summary>
        public float FireRate;
        public int MagazineSize;
        public float ReloadTime;
        /// <summary>Calor por disparo (0 = el arma no se calienta). A 100 se sobrecalienta.</summary>
        public float HeatPerShot;
        public float ProjectileSpeed;
        /// <summary>Alcance en metros: el proyectil desaparece al recorrerlo.</summary>
        public float Range;
        /// <summary>A partir de esta distancia el daño baja linealmente hasta MinDamageFactor al llegar al alcance.</summary>
        public float FalloffStart;
        public float MinDamageFactor = 1f;
        /// <summary>Dispersión (grados, radio del cono) disparando desde la cadera y apuntando.</summary>
        public float HipSpread, AimSpread;
        /// <summary>Lo que se abre la dispersión por disparo y su máximo (se recupera a BloomRecovery °/s).</summary>
        public float BloomPerShot, MaxBloom;
        /// <summary>Retroceso visual (grados hacia arriba) por disparo.</summary>
        public float Recoil;
        /// <summary>Radio del ruido del disparo (los robots lo oyen con NoiseSystem).</summary>
        public float NoiseRadius;
        /// <summary>Enemigos que atraviesa el proyectil (0 = se detiene en el primero).</summary>
        public int Pierce;
        public int StartReserve, MaxReserve;
        /// <summary>Munición de reserva que da un paquete de munición de un robot destruido.</summary>
        public int AmmoPerPickup;
        /// <summary>Color del proyectil (r, g, b).</summary>
        public float ColorR, ColorG, ColorB;

        public float FireInterval => FireRate > 0f ? 1f / FireRate : 0.25f;

        /// <summary>Copia independiente (los accesorios cambian la copia, no el catálogo).</summary>
        public PlayerWeaponSpec Clone() => (PlayerWeaponSpec)MemberwiseClone();

        /// <summary>Factor de daño a 'distance' metros (1 hasta FalloffStart, MinDamageFactor al alcance).</summary>
        public float DamageFactorAt(float distance)
        {
            if (distance <= FalloffStart || Range <= FalloffStart) return 1f;
            float k = Math.Min(1f, (distance - FalloffStart) / (Range - FalloffStart));
            return 1f + (MinDamageFactor - 1f) * k;
        }
    }

    /// <summary>
    /// Arsenal del jugador (valores de diseño). Para añadir un arma nueva basta con añadir una entrada aquí:
    /// el arsenal, el HUD, el director y las pruebas la recogen solos.
    ///   · Pistola de plasma  — estándar: precisa, semiautomática, se calienta.
    ///   · Rifle de asalto    — rápido: automático, cargador grande, poco daño por bala.
    ///   · Escopeta de pulsos — dispersión: 7 perdigones, devastadora de cerca, inútil de lejos.
    ///   · Cañón de riel      — especial: 95 de daño, atraviesa 2 robots, munición escasa y mucho ruido.
    /// </summary>
    public static class PlayerWeaponCatalog
    {
        public const string Pistol = "pistola_plasma";
        public const string Rifle = "rifle_asalto";
        public const string Shotgun = "escopeta_pulsos";
        public const string Railgun = "canon_riel";

        public static readonly IReadOnlyList<PlayerWeaponSpec> All = new List<PlayerWeaponSpec>
        {
            new PlayerWeaponSpec
            {
                Id = Pistol, DisplayName = "Pistola de plasma", Mode = FireMode.SemiAuto,
                Damage = 20f, Pellets = 1, FireRate = 4f, MagazineSize = 12, ReloadTime = 1.3f, HeatPerShot = 12f,
                ProjectileSpeed = 60f, Range = 60f, FalloffStart = 60f, MinDamageFactor = 1f,
                HipSpread = 1.2f, AimSpread = 0.3f, BloomPerShot = 0.4f, MaxBloom = 3f, Recoil = 0.6f,
                NoiseRadius = 10f, Pierce = 0, StartReserve = 36, MaxReserve = 96, AmmoPerPickup = 12,
                ColorR = 0.35f, ColorG = 0.85f, ColorB = 1f,
            },
            new PlayerWeaponSpec
            {
                Id = Rifle, DisplayName = "Rifle de asalto", Mode = FireMode.FullAuto,
                Damage = 12f, Pellets = 1, FireRate = 9f, MagazineSize = 30, ReloadTime = 2f, HeatPerShot = 4f,
                ProjectileSpeed = 120f, Range = 80f, FalloffStart = 40f, MinDamageFactor = 0.7f,
                HipSpread = 2.5f, AimSpread = 0.6f, BloomPerShot = 0.4f, MaxBloom = 3f, Recoil = 0.35f,
                NoiseRadius = 18f, Pierce = 0, StartReserve = 90, MaxReserve = 180, AmmoPerPickup = 30,
                ColorR = 1f, ColorG = 0.75f, ColorB = 0.3f,
            },
            new PlayerWeaponSpec
            {
                Id = Shotgun, DisplayName = "Escopeta de pulsos", Mode = FireMode.SemiAuto,
                Damage = 9f, Pellets = 7, FireRate = 1.2f, MagazineSize = 6, ReloadTime = 2.4f, HeatPerShot = 0f,
                ProjectileSpeed = 55f, Range = 22f, FalloffStart = 6f, MinDamageFactor = 0.25f,
                HipSpread = 6f, AimSpread = 4.5f, BloomPerShot = 0.8f, MaxBloom = 2f, Recoil = 2.2f,
                NoiseRadius = 16f, Pierce = 0, StartReserve = 18, MaxReserve = 36, AmmoPerPickup = 6,
                ColorR = 0.6f, ColorG = 1f, ColorB = 0.45f,
            },
            new PlayerWeaponSpec
            {
                Id = Railgun, DisplayName = "Cañón de riel", Mode = FireMode.SemiAuto,
                Damage = 95f, Pellets = 1, FireRate = 0.7f, MagazineSize = 3, ReloadTime = 2.8f, HeatPerShot = 45f,
                ProjectileSpeed = 220f, Range = 90f, FalloffStart = 90f, MinDamageFactor = 1f,
                HipSpread = 0.8f, AimSpread = 0f, BloomPerShot = 0f, MaxBloom = 0f, Recoil = 3f,
                NoiseRadius = 26f, Pierce = 2, StartReserve = 3, MaxReserve = 9, AmmoPerPickup = 1,
                ColorR = 0.85f, ColorG = 0.4f, ColorB = 1f,
            },
        };

        public static PlayerWeaponSpec Get(string id)
        {
            foreach (var w in All) if (w.Id == id) return w;
            return null;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].Id == id) return i;
            return -1;
        }
    }
}
