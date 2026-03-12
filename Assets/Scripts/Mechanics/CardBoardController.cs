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
    /// </summary>
    public class CardBoardController : MonoBehaviour
    {
        public static CardBoardController Instance { get; private set; }

        [SerializeField] CardPool _cardPool;
        [SerializeField] RectTransform _boardContainer;
        [SerializeField] GameConfig _config;

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
        /// </summary>
        public void InitBoard(MatchingCardsModel model)
        {
            ClearBoard();
            ResetPendingCards(model);

            int total = model.Cards.Count;
            _cardViews = new CardView[total];

            Vector2 cellSize = ComputeCellSize(model.Rows, model.Columns);

            for (int i = 0; i < total; i++)
            {
                CardView view = _cardPool.GetCard();
                view.Init(i, model.Cards[i], _config?.CardBackSprite);
                view.OnCardClicked += OnCardViewClicked;

                int row = i / model.Columns;
                int col = i % model.Columns;
                PositionCard(view.GetComponent<RectTransform>(), row, col, cellSize);

                _cardViews[i] = view;
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
        /// Computes a uniform cell size that fits the grid inside the board container
        /// while preserving square cards.
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

        void PositionCard(RectTransform rt, int row, int col, Vector2 cellSize)
        {
            rt.SetParent(_boardContainer, worldPositionStays: false);
            rt.sizeDelta = cellSize;

            // Anchor and pivot at top-left so anchoredPosition goes right and down
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            float x =  _cardSpacing + col * (cellSize.x + _cardSpacing);
            float y = -(_cardSpacing + row * (cellSize.y + _cardSpacing));
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
