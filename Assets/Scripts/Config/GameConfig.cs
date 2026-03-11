using System.Collections.Generic;
using UnityEngine;

namespace MatchingCards.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObjects/Create GameConfig ScriptableObject", order = 1)]
    public class GameConfig : ScriptableObject
    {
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

        public int TotalCards => Rows * Columns;

        /// <summary>
        /// A layout is valid when it produces at least one pair and cards divide evenly into pairs.
        /// </summary>
        public bool IsValid => TotalCards > 0 && TotalCards % 2 == 0;
    }
}
