using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Combat;
using Nexus7.Core;

namespace Nexus7.Director
{
    /// <summary>
    /// Informe de la IA (PhaseReportModel): al entrar en una fase, si la anterior duró 20 s o más, aparece arriba a la
    /// derecha durante 9 s lo que el analizador vio en ella (precisión, bajas, caídas, arma dominante, acampada,
    /// tensión, acciones de la IA, estilo de juego) y qué hará con eso. Si la fase se reinicia por quedarte sin vidas,
    /// enseña el del intento anterior.
    /// Se añade solo al jugador (CombatInstaller.PlayerInstalled).
    /// </summary>
    public class PhaseReportPanel : MonoBehaviour
    {
        private const float ShowDelay = 1.5f, ShowTime = 9f, FadeTime = 1f, Width = 470f;

        private PhaseReportModel report;
        private List<string> lines;
        private string title, verdict;
        private float shownAt = -1f;
        private GUIStyle style, titleStyle, verdictStyle;
        private static Texture2D white;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            CombatInstaller.PlayerInstalled -= Attach;
            CombatInstaller.PlayerInstalled += Attach;
        }

        private static void Attach(GameObject player)
        {
            if (player.GetComponent<PhaseReportPanel>() == null) player.AddComponent<PhaseReportPanel>();
        }

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(ShowDelay);
            report = DirectorSession.TakeReport(SceneManager.GetActiveScene().name, out bool same);
            if (report == null) { enabled = false; yield break; }
            title = same ? "INFORME DE LA IA · intento anterior" : "INFORME DE LA IA · fase anterior";
            lines = report.Lines(id =>
            {
                var spec = PlayerWeaponCatalog.Get(id);
                return spec != null ? spec.DisplayName : null;
            });
            lines.Insert(0, report.PhaseTitle);
            verdict = "«" + report.Verdict() + "»";
            shownAt = Time.unscaledTime;
        }

        private void OnGUI()
        {
            if (shownAt < 0f) return;
            float t = Time.unscaledTime - shownAt;
            if (t > ShowTime) { shownAt = -1f; enabled = false; return; }
            float alpha = Mathf.Clamp01((ShowTime - t) / FadeTime) * Mathf.Clamp01(t / 0.3f);

            if (white == null) { white = new Texture2D(1, 1, TextureFormat.RGBA32, false); white.SetPixel(0, 0, Color.white); white.Apply(); }
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
                titleStyle = new GUIStyle(style) { fontSize = 16, fontStyle = FontStyle.Bold };
                verdictStyle = new GUIStyle(style) { wordWrap = true, fontStyle = FontStyle.Italic };
            }
            float verdictH = verdictStyle.CalcHeight(new GUIContent(verdict), Width - 24f);
            float h = 18f + 24f + lines.Count * 20f + 8f + verdictH + 12f;
            float x = Screen.width - Width - 16f, y = 16f;

            GUI.color = new Color(0.02f, 0.05f, 0.1f, 0.8f * alpha);
            GUI.DrawTexture(new Rect(x, y, Width, h), white);
            GUI.color = new Color(0.35f, 0.8f, 1f, alpha);
            GUI.DrawTexture(new Rect(x, y, 3f, h), white);   // franja de la IA

            float cy = y + 10f;
            GUI.color = new Color(0.55f, 0.85f, 1f, alpha);
            GUI.Label(new Rect(x + 12f, cy, Width - 24f, 24f), title, titleStyle);
            cy += 26f;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            foreach (var line in lines)
            {
                GUI.Label(new Rect(x + 12f, cy, Width - 24f, 20f), line, style);
                cy += 20f;
            }
            cy += 8f;
            GUI.color = new Color(1f, 0.85f, 0.5f, alpha);
            GUI.Label(new Rect(x + 12f, cy, Width - 24f, verdictH), verdict, verdictStyle);
            GUI.color = Color.white;
        }
    }
}
