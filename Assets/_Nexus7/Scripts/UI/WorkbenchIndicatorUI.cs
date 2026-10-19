using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Crafting;

namespace Nexus7.UI
{
    /// <summary>
    /// Indicador del HUD: «BANCO DE TRABAJO cerca · [E] para fabricar» cuando el jugador está a 'radius' m o menos de un banco.
    /// Colocar en: el Canvas "HUD". El jugador se busca por la etiqueta "Player".
    /// </summary>
    public class WorkbenchIndicatorUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image icon;
        [SerializeField] private float radius = 4f;

        private Transform player;
        private readonly List<Vector3> positions = new List<Vector3>();

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            Show(false);
        }

        private void Update()
        {
            if (player == null) return;
            positions.Clear();
            foreach (var b in Workbench.All) if (b != null) positions.Add(b.transform.position);
            Show(Workbench.NearestInRange(player.position, positions, radius) >= 0);
        }

        private void Show(bool on)
        {
            if (label != null) label.text = on ? "BANCO DE TRABAJO cerca · [E] para fabricar" : "";
            if (icon != null) icon.enabled = on;
        }
    }
}
