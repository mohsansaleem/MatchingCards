using MatchingCards.Core;
using MatchingCards.Mechanics.Events;
using MatchingCards.Model;
using TMPro;
using UnityEngine;

namespace MatchingCards.UI
{
    /// <summary>
    /// Displays the live score, combo streak, and move count during gameplay.
    ///
    /// Refreshes by subscribing to <see cref="CheckMatchEvent.OnExecute"/> —
    /// no direct coupling to game logic. The combo label is hidden whenever
    /// the streak is fewer than two consecutive matches.
    /// </summary>
    public class ScoreHUDController : MonoBehaviour
    {
        [Header("Score Labels")]
        [SerializeField] TMP_Text _scoreText;
        [SerializeField] TMP_Text _movesText;

        [Header("Combo")]
        [Tooltip("Text shown when ComboCount >= 2. Hidden otherwise.")]
        [SerializeField] TMP_Text _comboText;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void OnEnable()
        {
            CheckMatchEvent.OnExecute += HandleCheckMatch;

            // Sync display with whatever the model currently holds
            // (handles the case where the HUD re-enables mid-game or after load).
            Refresh(Simulation.GetModel<MatchingCardsModel>());
        }

        void OnDisable()
        {
            CheckMatchEvent.OnExecute -= HandleCheckMatch;
        }

        // ── Event handler ─────────────────────────────────────────────────────

        void HandleCheckMatch(CheckMatchEvent ev)
        {
            Refresh(Simulation.GetModel<MatchingCardsModel>());
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately updates all HUD labels from <paramref name="model"/>.
        /// Call this after loading a saved game to sync the display.
        /// </summary>
        public void Refresh(MatchingCardsModel model)
        {
            if (model == null) return;

            if (_scoreText != null)
                _scoreText.text = $"Score\n{model.Score:N0}";

            if (_movesText != null)
                _movesText.text = $"Moves\n{model.MoveCount}";

            if (_comboText != null)
            {
                bool hasCombo = model.ComboCount >= 2;
                _comboText.gameObject.SetActive(hasCombo);
                if (hasCombo)
                    _comboText.text = $"Combo ×{model.ComboCount}!";
            }
        }
    }
}
