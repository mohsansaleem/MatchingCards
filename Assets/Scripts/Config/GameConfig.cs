using System.Collections.Generic;
using UnityEngine;

namespace MatchingCards.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObjects/Create GameConfig ScriptableObject", order = 1)]
    public class GameConfig : ScriptableObject
    {
        [Header("Card Visuals")]
        /// <summary>
        /// Sprite shown on the back of every card (face-down state).
        /// </summary>
        public Sprite CardBackSprite;

        [Header("Card Definitions")]
        /// <summary>
        /// Pool of card face sprites to pick from when building a grid.
        /// </summary>
        public List<CardMeta> CardMetaOptions;

        [Header("Grid Layouts")]
        /// <summary>
        /// Supported grid configurations (e.g. 2x2, 3x3, 5x6).
        /// Only layouts where Rows * Columns is even are valid.
        /// </summary>
        public List<GridLayout> GridLayouts;

        [Header("Scoring")]
        /// <summary>
        /// Base points awarded for each matched pair.
        /// </summary>
        public int BaseMatchScore = 100;

        /// <summary>
        /// Bonus points added per consecutive combo level on top of BaseMatchScore.
        /// Score per match = BaseMatchScore + ComboScoreBonus * (comboCount - 1)
        /// </summary>
        public int ComboScoreBonus = 50;

        [Header("Sound Effects")]
        /// <summary>Played every time a card is flipped face-up.</summary>
        public AudioClip FlipClip;

        /// <summary>Played when a pair matches but the game is not yet complete.</summary>
        public AudioClip MatchClip;

        /// <summary>Played when a pair does not match.</summary>
        public AudioClip MismatchClip;

        /// <summary>Played when the final pair is matched and the game is complete.</summary>
        public AudioClip GameOverClip;
    }

    [System.Serializable]
    public class CardMeta
    {
        public string Name;
        public Sprite Sprite;
    }

    [System.Serializable]
    public class GridLayout
    {
        /// <summary>
        /// Human-readable label shown in UI (e.g. "2x2", "3x3", "5x6").
        /// </summary>
        public string Label;

        public int Rows;
        public int Columns;

        /// <summary>
        /// Cells to leave visually empty. Each entry is (X = column, Y = row), 0-based.
        /// Example: a 4×5 grid with the two mid-cells of column 2 empty →
        /// add Vector2Int(2, 1) and Vector2Int(2, 2).
        /// The remaining active cell count must still be even.
        /// </summary>
        [Tooltip("Cells to leave empty. X = column index, Y = row index (both 0-based).")]
        public List<UnityEngine.Vector2Int> EmptyCells = new List<UnityEngine.Vector2Int>();

        /// <summary>Number of active (non-empty) card slots.</summary>
        public int TotalCards => Rows * Columns - EmptyCells.Count;

        /// <summary>
        /// A layout is valid when it produces at least one pair and cards divide evenly into pairs.
        /// </summary>
        public bool IsValid => TotalCards > 0 && TotalCards % 2 == 0;
    }
}
