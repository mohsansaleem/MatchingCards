using System.Collections.Generic;
using MatchingCards.Config;
using UnityEngine;

namespace MatchingCards.Model
{
    /// <summary>
    /// The main model containing all data needed for a MatchingCards game.
    /// This class only holds data and methods that directly operate on that data.
    /// It is initialised by GameController and is designed to be fully serializable
    /// for the save/load system.
    /// </summary>
    [System.Serializable]
    public class MatchingCardsModel
    {
        // ── Grid ─────────────────────────────────────────────────────────────
        public int Rows;
        public int Columns;

        /// <summary>
        /// Flat grid indices (row * Columns + col) of cells that have no card.
        /// Populated from <see cref="GridLayout.EmptyCells"/> at Init time.
        /// Persisted so save/load can reconstruct the board without the original config.
        /// </summary>
        public List<int> EmptyCellIndices = new List<int>();

        /// <summary>Which stage (0-based index into GameConfig.GridLayouts) this game is on.</summary>
        public int StageIndex;

        /// <summary>Total card pairs in the current grid (empty cells excluded).</summary>
        public int TotalPairs => (Rows * Columns - EmptyCellIndices.Count) / 2;

        /// <summary>True when all pairs have been matched.</summary>
        public bool IsComplete => MatchedPairs >= TotalPairs;

        // ── Cards ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Flat list of all cards in row-major order.
        /// Index matches the card's position in the grid: index = row * Columns + col.
        /// </summary>
        public List<CardData> Cards;

        /// <summary>
        /// Indices of cards that are face-up and waiting to be paired with a second flip.
        /// Cards are added in flip order; every two form a pending comparison pair (FIFO).
        /// </summary>
        public List<int> PendingCardIndices;

        // ── Score ─────────────────────────────────────────────────────────────
        public int Score;

        /// <summary>Current consecutive-match streak. Resets on any mismatch.</summary>
        public int ComboCount;

        /// <summary>Total number of flip-pair attempts (each pair of flips = 1 move).</summary>
        public int MoveCount;

        public int MatchedPairs;

        // ── Scoring config (serialized so save/load is self-contained) ────────
        public int BaseMatchScore;
        public int ComboScoreBonus;

        // ── Initialisation ────────────────────────────────────────────────────

        /// <summary>
        /// Initialises a fresh game from config values.
        /// Builds and shuffles the card list.
        /// </summary>
        /// <param name="emptyCells">
        /// Cells to skip, expressed as (X = column, Y = row). Stored as flat indices
        /// (row * cols + col) so the board controller can reconstruct the layout from
        /// the model alone (e.g. after a save/load).
        /// </param>
        public void Init(int rows, int cols,
                         List<Vector2Int> emptyCells,
                         List<CardMeta> availableMetas,
                         int baseMatchScore, int comboScoreBonus,
                         int stageIndex = 0)
        {
            Rows            = rows;
            Columns         = cols;
            StageIndex      = stageIndex;
            Score           = 0;
            ComboCount      = 0;
            MoveCount       = 0;
            MatchedPairs    = 0;
            BaseMatchScore  = baseMatchScore;
            ComboScoreBonus = comboScoreBonus;
            PendingCardIndices = new List<int>();

            EmptyCellIndices = new List<int>(emptyCells?.Count ?? 0);
            if (emptyCells != null)
                foreach (var c in emptyCells)
                    EmptyCellIndices.Add(c.y * cols + c.x);  // X = col, Y = row

            BuildCards(availableMetas);
        }

        /// <summary>
        /// Advances to a new stage while preserving the accumulated score.
        /// All other state (cards, combos, moves) is reset as in a fresh game.
        /// </summary>
        public void InitNextStage(int rows, int cols,
                                   List<Vector2Int> emptyCells,
                                   List<CardMeta> availableMetas,
                                   int baseMatchScore, int comboScoreBonus,
                                   int stageIndex)
        {
            int savedScore = Score;
            Init(rows, cols, emptyCells, availableMetas,
                 baseMatchScore, comboScoreBonus, stageIndex);
            Score = savedScore;
        }

        void BuildCards(List<CardMeta> availableMetas)
        {
            int pairs = TotalPairs;
            Cards = new List<CardData>(pairs * 2);

            for (int i = 0; i < pairs; i++)
            {
                var meta = availableMetas[i % availableMetas.Count];
                Cards.Add(new CardData { Id = i * 2,     PairId = i, Meta = meta });
                Cards.Add(new CardData { Id = i * 2 + 1, PairId = i, Meta = meta });
            }

            Shuffle(Cards);
        }

        void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ── Card state helpers ────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the card at <paramref name="index"/> may be flipped:
        /// it must be face-down and not already matched.
        /// </summary>
        public bool CanFlipCard(int index)
        {
            if (index < 0 || index >= Cards.Count) return false;
            var card = Cards[index];
            return !card.IsFlipped && !card.IsMatched;
        }

        /// <summary>
        /// Flips the card at <paramref name="index"/> face-up and adds it to the pending queue.
        /// Call <see cref="TryConsumePendingPair"/> immediately after to check whether a pair
        /// is ready for comparison.
        /// </summary>
        public void FlipCard(int index)
        {
            Cards[index].IsFlipped = true;
            PendingCardIndices.Add(index);
        }

        /// <summary>
        /// Attempts to extract the oldest waiting pair from <see cref="PendingCardIndices"/>.
        /// Cards are paired FIFO — the first waiting card is matched with the second flip.
        /// Each consumed pair counts as one move.
        /// </summary>
        /// <returns>True when a pair was available and has been consumed.</returns>
        public bool TryConsumePendingPair(out int indexA, out int indexB)
        {
            indexA = -1;
            indexB = -1;

            if (PendingCardIndices.Count < 2) return false;

            indexA = PendingCardIndices[0];
            indexB = PendingCardIndices[1];
            PendingCardIndices.RemoveRange(0, 2);

            MoveCount++;
            return true;
        }

        /// <summary>True when the two cards share the same PairId.</summary>
        public bool IsMatchingPair(int indexA, int indexB)
        {
            return Cards[indexA].PairId == Cards[indexB].PairId;
        }

        // ── Match / mismatch resolution ───────────────────────────────────────

        /// <summary>
        /// Marks both cards as matched, increments the combo streak, and awards score.
        /// Score = BaseMatchScore + ComboScoreBonus * (comboCount - 1)
        /// </summary>
        public void RegisterMatch(int indexA, int indexB)
        {
            Cards[indexA].IsMatched = true;
            Cards[indexB].IsMatched = true;

            ComboCount++;
            Score += BaseMatchScore + ComboScoreBonus * (ComboCount - 1);
            MatchedPairs++;
        }

        /// <summary>
        /// Flips both cards back face-down and resets the combo streak.
        /// </summary>
        public void RegisterMismatch(int indexA, int indexB)
        {
            Cards[indexA].IsFlipped = false;
            Cards[indexB].IsFlipped = false;

            ComboCount = 0;
        }
    }

    // ── Supporting data types ─────────────────────────────────────────────────

    [System.Serializable]
    public class CardData
    {
        /// <summary>Unique index within the card list.</summary>
        public int Id;

        /// <summary>Cards sharing the same PairId are a matching pair.</summary>
        public int PairId;

        /// <summary>Visual/meta information for this card's face.</summary>
        public CardMeta Meta;

        public bool IsFlipped;
        public bool IsMatched;
    }
}
