using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nexus7.Enemies;
using Nexus7.Environment;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Cinematics
{
    /// <summary>
    /// Reproduce una cinemática: planos de cámara con subtítulos en español y franjas negras de cine.
    /// Mientras dura: el jugador está bloqueado y los robots en pausa (no ven ni se mueven).
    /// Se salta MANTENIENDO [Espacio] (o A del mando) 1 segundo. Al acabar, la cámara vuelve al jugador.
    ///
    /// Colocar en: GameSystems de la escena. 'Player' = PlayerController · 'Camera Transform' = PlayerCamera.
    /// </summary>
    public class CinematicDirector : MonoBehaviour
    {
        /// <summary>Hay una cinemática en curso (los sistemas que reaccionan al jugador deben esperar).</summary>
        public static bool IsPlaying { get; private set; }

        [SerializeField] private List<CinematicShot> shots = new List<CinematicShot>();
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private SubtitleUI subtitles;
        [SerializeField] private float skipHoldSeconds = 1f;
        [SerializeField] private float returnBlendSeconds = 0.8f;
        [Tooltip("Al terminar, devolver la cámara al jugador. Desactivado en las cinemáticas de los finales (la cámara se queda fuera).")]
        [SerializeField] private bool returnToPlayer = true;

        private bool playing, ended;
        private Transform cameraParent;
        private Vector3 cameraLocalPos;
        private Quaternion cameraLocalRot;
        private CameraShake shake;
        private bool shakeWasEnabled;

        /// <summary>Se dispara una sola vez al terminar (o al saltarla).</summary>
        public event Action Finished;

        public List<CinematicShot> Shots => shots;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { IsPlaying = false; EnemyPatrol.Paused = false; }

        private IEnumerator Start()
        {
            if (!playOnStart || shots.Count == 0) yield break;
            yield return null;                       // deja que el resto de la escena se inicialice
            Play();
        }

        /// <summary>Si la escena se descarga a mitad de cinemática, no dejar a los robots en pausa.</summary>
        private void OnDisable() => End();

        public void Play()
        {
            if (playing) return;
            StartCoroutine(Run());
        }

        /// <summary>Entra en modo cinemática: bloquea al jugador, pausa a los robots, cámara libre.</summary>
        public void Begin()
        {
            if (playing) return;
            playing = true; ended = false;
            IsPlaying = true;
            EnemyPatrol.Paused = true;
            foreach (var e in EnemyPatrol.All)
            {
                var m = e != null ? e.GetComponent<EnemyMeleeAttack>() : null;
                if (m != null) m.Cancel();
            }
            if (player != null) player.AddInputLock(this);
            if (cameraTransform != null)
            {
                cameraParent = cameraTransform.parent;
                cameraLocalPos = cameraTransform.localPosition;
                cameraLocalRot = cameraTransform.localRotation;
                shake = cameraTransform.GetComponent<CameraShake>();
                if (shake != null) { shakeWasEnabled = shake.enabled; shake.enabled = false; }
                cameraTransform.SetParent(null, true);
            }
            if (subtitles != null) subtitles.SetLetterbox(true);
        }

        /// <summary>Salta al final (se puede llamar varias veces).</summary>
        public void Skip()
        {
            if (playing && !ended) End();
        }

        /// <summary>Sale del modo cinemática. Idempotente: Finished se dispara una sola vez.</summary>
        public void End()
        {
            if (!playing || ended) return;
            ended = true;
            playing = false;
            IsPlaying = false;
            EnemyPatrol.Paused = false;
            if (player != null) player.RemoveInputLock(this);
            if (returnToPlayer && cameraTransform != null && cameraParent != null)
            {
                cameraTransform.SetParent(cameraParent, false);
                cameraTransform.localPosition = cameraLocalPos;
                cameraTransform.localRotation = cameraLocalRot;
            }
            // Si la cámara se queda fuera (finales), la vibración sigue apagada: su posición local ya no es la del jugador.
            if (shake != null && returnToPlayer) shake.enabled = shakeWasEnabled;
            if (subtitles != null) { subtitles.Hide(); subtitles.SetLetterbox(false); subtitles.SetSkipProgress(0f); }
            Finished?.Invoke();
        }

        private IEnumerator Run()
        {
            Begin();
            var durations = new List<float>();
            foreach (var s in shots) durations.Add(s.duration);
            var timeline = new CinematicTimeline(durations);
            var skip = new SkipHold(skipHoldSeconds);
            float t = 0f;
            int lastShot = -1, lastLine = -2;

            while (!ended && !timeline.IsFinished(t))
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                int shotIndex; float k;
                timeline.Evaluate(t, out shotIndex, out k);
                var shot = shots[shotIndex];
                if (shotIndex != lastShot)
                {
                    lastShot = shotIndex; lastLine = -2;
                    if (shot.activateOnStart != null) foreach (var go in shot.activateOnStart) if (go != null) go.SetActive(true);
                }

                float e = Mathf.SmoothStep(0f, 1f, k);
                if (cameraTransform != null)
                {
                    Vector3 pos = Vector3.Lerp(shot.fromPos, shot.toPos, e);
                    if (shot.shake > 0f) pos += new Vector3(Mathf.PerlinNoise(t * 9f, 0f) - 0.5f, Mathf.PerlinNoise(0f, t * 9f) - 0.5f, 0f) * shot.shake;
                    Vector3 look = Vector3.Lerp(shot.fromLook, shot.toLook, e);
                    cameraTransform.position = pos;
                    Vector3 dir = look - pos;
                    if (dir.sqrMagnitude > 0.0001f) cameraTransform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }

                int line = CinematicTimeline.ActiveLine(shot.lines, timeline.LocalSeconds(t, shotIndex));
                if (line != lastLine && subtitles != null)
                {
                    lastLine = line;
                    if (line >= 0) subtitles.Show(shot.lines[line].speaker, shot.lines[line].text); else subtitles.Hide();
                }

                bool held = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.JoystickButton0);
                bool done = skip.Update(held, dt);
                if (subtitles != null) subtitles.SetSkipProgress(skip.Progress);
                if (done) break;
                yield return null;
            }

            // Vuelta suave a la cámara del jugador
            if (returnToPlayer && !ended && cameraTransform != null && cameraParent != null && returnBlendSeconds > 0f)
            {
                Vector3 fromP = cameraTransform.position; Quaternion fromR = cameraTransform.rotation;
                for (float b = 0f; b < returnBlendSeconds && !ended; b += Time.unscaledDeltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, b / returnBlendSeconds);
                    cameraTransform.position = Vector3.Lerp(fromP, cameraParent.TransformPoint(cameraLocalPos), k);
                    cameraTransform.rotation = Quaternion.Slerp(fromR, cameraParent.rotation * cameraLocalRot, k);
                    yield return null;
                }
            }
            End();
        }
    }
}
