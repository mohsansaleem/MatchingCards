using System.Collections.Generic;
using MatchingCards.Config;
using MatchingCards.Core;
using MatchingCards.Mechanics.Events;
using MatchingCards.Model;
using MatchingCards.View;
using UnityEngine;

namespace MatchingCards.Mechanics
{
    /// <summary>
    /// Manages the visual card grid: takes cards from CardPool, positions them
    /// inside the board container, and returns them to the pool when the board
    /// is cleared.
    ///
    /// Card-click events are wired here so that CardView stays decoupled from
    /// the Mechanics layer.
    ///
    /// Empty-cell support: cells listed in <see cref="MatchingCardsModel.EmptyCellIndices"/>
    /// are skipped during layout — a gap is left in the grid but no card view is placed.
    ///
    /// Centering: the card grid is always centred inside <see cref="_boardContainer"/>
    /// regardless of grid size, so smaller grids do not hug the top-left corner.
    /// </summary>
    public class CardBoardController : MonoBehaviour
    {
        public static CardBoardController Instance { get; private set; }

        [SerializeField] CardPool      _cardPool;
        [SerializeField] RectTransform _boardContainer;
        [SerializeField] GameConfig    _config;

        [Tooltip("Gap in pixels between cards and around the board edge.")]
        [SerializeField] float _cardSpacing = 10f;

        CardView[] _cardViews;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void OnEnable()  => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        // ── Board setup ───────────────────────────────────────────────────────

        /// <summary>
        /// Builds the visual board from the current model state.
        /// <para>
        /// Any cards left in <see cref="MatchingCardsModel.PendingCardIndices"/>
        /// (e.g. from a mid-game save) are flipped back to face-down and cleared,
        /// since their in-flight comparison events cannot be restored.
        /// </para>
        /// <para>
        /// The caller is responsible for activating the board's canvas before
        /// calling this method (so Unity registers it as dirty). This method
        /// then calls <see cref="Canvas.ForceUpdateCanvases"/> to guarantee
        /// that <see cref="_boardContainer"/>.rect reflects the true layout
        /// size even on the very first call in a session.
        /// </para>
        /// </summary>
        public void InitBoard(MatchingCardsModel model)
        {
            ClearBoard();
            ResetPendingCards(model);

            // Force Unity to compute the canvas layout immediately.
            // Without this, _boardContainer.rect is (0,0) the first time the
            // canvas is activated in a session, causing cards to be positioned
            // off-screen. Subsequent calls work because Unity caches the rect
            // from the previous active session — but the first call cannot
            // rely on that cache.
            Canvas.ForceUpdateCanvases();

            _cardViews = new CardView[model.Cards.Count];

            Vector2 cellSize   = ComputeCellSize(model.Rows, model.Columns);
            Vector2 gridOffset = ComputeGridOffset(model.Rows, model.Columns, cellSize);

            var emptySet  = new HashSet<int>(model.EmptyCellIndices);
            int cardIndex = 0;

            for (int gridPos = 0; gridPos < model.Rows * model.Columns; gridPos++)
            {
                if (emptySet.Contains(gridPos)) continue;   // leave this cell blank

                int row = gridPos / model.Columns;
                int col = gridPos % model.Columns;

                CardView view = _cardPool.GetCard();
                view.Init(cardIndex, model.Cards[cardIndex], _config?.CardBackSprite);
                view.OnCardClicked += OnCardViewClicked;

                PositionCard(view.GetComponent<RectTransform>(), row, col, cellSize, gridOffset);

                _cardViews[cardIndex] = view;
                cardIndex++;
            }
        }

        /// <summary>
        /// Returns all active card views to the pool and clears the board array.
        /// </summary>
        public void ClearBoard()
        {
            if (_cardViews == null) return;

            foreach (var view in _cardViews)
            {
                if (view != null)
                {
                    view.OnCardClicked -= OnCardViewClicked;
                    _cardPool.AddItemToPool(view);
                }
            }

            _cardViews = null;
        }

        /// <summary>
        /// Returns the CardView at the given model index, or null if out of range.
        /// </summary>
        public CardView GetCardView(int index)
        {
            if (_cardViews == null || index < 0 || index >= _cardViews.Length)
                return null;

            return _cardViews[index];
        }

        // ── Click handler ─────────────────────────────────────────────────────

        void OnCardViewClicked(int cardIndex)
        {
            var ev = Simulation.Schedule<FlipCardEvent>();
            ev.CardIndex = cardIndex;
        }

        // ── Layout helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Computes a uniform cell size that fits the full rows×cols bounding box
        /// inside the board container while preserving square cards.
        /// Empty cells still occupy a slot in the bounding box — their gap is
        /// intentional and keeps the grid aligned.
        /// Note: call after the Canvas layout pass so _boardContainer.rect is valid.
        /// </summary>
        Vector2 ComputeCellSize(int rows, int columns)
        {
            float w = _boardContainer.rect.width;
            float h = _boardContainer.rect.height;

            float cellW = (w - _cardSpacing * (columns + 1)) / columns;
            float cellH = (h - _cardSpacing * (rows    + 1)) / rows;

            float side = Mathf.Min(cellW, cellH);
            return new Vector2(side, side);
        }

        /// <summary>
        /// Returns the (x, y) pixel offset needed to centre the grid inside the
        /// board container. Applied to every card position so the whole grid sits
        /// in the middle of the available area regardless of how many cells it has.
        /// </summary>
        Vector2 ComputeGridOffset(int rows, int cols, Vector2 cellSize)
        {
            float gridW = cols * cellSize.x + (cols + 1) * _cardSpacing;
            float gridH = rows * cellSize.y + (rows + 1) * _cardSpacing;

            float offsetX = Mathf.Max(0f, (_boardContainer.rect.width  - gridW) / 2f);
            float offsetY = Mathf.Max(0f, (_boardContainer.rect.height - gridH) / 2f);

            return new Vector2(offsetX, offsetY);
        }

        void PositionCard(RectTransform rt, int row, int col, Vector2 cellSize, Vector2 gridOffset)
        {
            rt.SetParent(_boardContainer, worldPositionStays: false);
            rt.sizeDelta = cellSize;

            // Anchor and pivot at top-left so anchoredPosition goes right and down.
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            float x =  gridOffset.x + _cardSpacing + col * (cellSize.x + _cardSpacing);
            float y = -(gridOffset.y + _cardSpacing + row * (cellSize.y + _cardSpacing));
            rt.anchoredPosition = new Vector2(x, y);
        }

        // ── Save-restore helper ───────────────────────────────────────────────

        /// <summary>
        /// Clears pending card indices and sets those cards back to face-down
        /// so the board starts in a consistent state after a load.
        /// </summary>
        static void ResetPendingCards(MatchingCardsModel model)
        {
            foreach (int i in model.PendingCardIndices)
                model.Cards[i].IsFlipped = false;

            model.PendingCardIndices.Clear();
        }
    }
}
