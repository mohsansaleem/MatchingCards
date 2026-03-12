using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GridLayout = MatchingCards.Config.GridLayout;

namespace MatchingCards.UI
{
    /// <summary>
    /// Reads <see cref="GameConfig.GridLayouts"/> at runtime and spawns one
    /// button per layout inside this transform. Highlights the active selection
    /// and exposes it via <see cref="SelectedLayout"/>.
    /// </summary>
    public class GridLayoutSelectorController : MonoBehaviour
    {
        [Tooltip("Button prefab — must contain a TMP_Text child for the layout label.")]
        [SerializeField] Button _layoutButtonPrefab;

        [Tooltip("Tint applied to the currently-selected layout button.")]
        [SerializeField] Color _selectedColor = new Color(0.40f, 0.80f, 1.00f);

        [Tooltip("Tint applied to all non-selected layout buttons.")]
        [SerializeField] Color _normalColor = Color.white;

        readonly List<Button>       _buttons = new List<Button>();
        readonly List<GridLayout>   _layouts = new List<GridLayout>();

        GridLayout _selected;

        /// <summary>The layout the player has currently highlighted.</summary>
        public GridLayout SelectedLayout => _selected;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void Start()
        {
            Populate();
        }

        // ── Setup ─────────────────────────────────────────────────────────────

        void Populate()
        {
            // Remove any designer-placed children so the list is driven purely
            // from config (avoids stale buttons after config changes).
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            _buttons.Clear();
            _layouts.Clear();

            var config = MetaGameController.Instance.Config;
            foreach (var layout in config.GridLayouts)
            {
                if (!layout.IsValid) continue;   // skip odd-total layouts

                var captured = layout;            // safe closure capture

                var btn = Instantiate(_layoutButtonPrefab, transform);
                var lbl = btn.GetComponentInChildren<TMP_Text>();
                if (lbl != null) lbl.text = layout.Label;

                btn.onClick.AddListener(() => Select(captured));

                _buttons.Add(btn);
                _layouts.Add(layout);
            }

            // Auto-select the first valid layout so the HUD is never empty.
            if (_layouts.Count > 0)
                Select(_layouts[0]);
        }

        // ── Selection ─────────────────────────────────────────────────────────

        void Select(GridLayout layout)
        {
            _selected = layout;
            RefreshHighlights();
        }

        void RefreshHighlights()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                bool active = _layouts[i] == _selected;
                var colors  = _buttons[i].colors;
                colors.normalColor      = active ? _selectedColor : _normalColor;
                colors.selectedColor    = active ? _selectedColor : _normalColor;
                _buttons[i].colors      = colors;
            }
        }
    }
}
