using System.Collections.Generic;
using UnityEngine;
using Nexus7.Audio;

namespace Nexus7.Combat
{
    /// <summary>
    /// Reproduce los sonidos del combate (canal Efectos). Los clips se generan una vez con CombatSoundSynth.
    ///   CombatSfx.Play(CombatSound.Rifle);                 → en 2D (armas del jugador)
    ///   CombatSfx.PlayAt(CombatSound.Explosion, posición);  → en 3D (impactos, explosiones)
    /// </summary>
    public static class CombatSfx
    {
        private static AudioSource source;
        private static readonly Dictionary<CombatSound, AudioClip> clips = new Dictionary<CombatSound, AudioClip>();

        private static AudioClip Clip(CombatSound kind)
        {
            if (!clips.TryGetValue(kind, out var clip) || clip == null)
            {
                clip = MusicPlayer.MakeClip("Combate_" + kind, CombatSoundSynth.Make(kind));
                clips[kind] = clip;
            }
            return clip;
        }

        private static AudioSource Source()
        {
            if (source == null)
            {
                var go = new GameObject("SonidosCombate");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 1f;
                go.AddComponent<AudioChannel>().Category = AudioCategory.Sfx;
            }
            return source;
        }

        public static void Play(CombatSound kind, float volume = 0.8f) => Source().PlayOneShot(Clip(kind), volume);

        public static void PlayAt(CombatSound kind, Vector3 position) => AudioSource.PlayClipAtPoint(Clip(kind), position);

        public static AudioClip ShotClipFor(string weaponId)
        {
            switch (weaponId)
            {
                case PlayerWeaponCatalog.Rifle:   return Clip(CombatSound.Rifle);
                case PlayerWeaponCatalog.Shotgun: return Clip(CombatSound.Shotgun);
                case PlayerWeaponCatalog.Railgun: return Clip(CombatSound.Railgun);
                default:                          return Clip(CombatSound.Pistol);
            }
        }

        public static void PlayShot(string weaponId) => Source().PlayOneShot(ShotClipFor(weaponId), 0.75f);
    }
}
