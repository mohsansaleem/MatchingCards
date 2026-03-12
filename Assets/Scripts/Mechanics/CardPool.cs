using MatchingCards.Core;
using MatchingCards.View;
using UnityEngine;

namespace MatchingCards.Mechanics
{
    /// <summary>
    /// Object pool for CardView instances.
    /// Extends GameObjectsPool so that cards are recycled across board resets
    /// rather than being destroyed and re-instantiated each time.
    ///
    /// The prefab and initial pool size are configured in the Inspector.
    /// <see cref="CardBoardController"/> calls <see cref="GetCard"/> directly;
    /// it never needs to call <see cref="Init"/> unless it wants to re-warm the pool.
    /// </summary>
    public class CardPool : GameObjectsPool<CardView>
    {
        [Tooltip("CardView prefab to instantiate. Must be assigned in the Inspector.")]
        [SerializeField] CardView _cardViewPrefab;

        [Tooltip("Number of card instances pre-created on Awake.")]
        [SerializeField] int _initialPoolSize = 30;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void Awake()
        {
            if (_cardViewPrefab != null)
                InitPool(_cardViewPrefab, _initialPoolSize);
            else
                Debug.LogError("[CardPool] _cardViewPrefab is not assigned. Assign it in the Inspector.");
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Re-warms the pool with a new prefab and count.
        /// Useful when swapping card skins at runtime; under normal use
        /// the Inspector-configured Awake init is sufficient.
        /// </summary>
        public void Init(CardView prefab, int initialCount)
        {
            InitPool(prefab, initialCount);
        }

        /// <summary>
        /// Returns a card from the pool. If the pool is exhausted, a new instance
        /// is created rather than returning null, so the board can always be filled.
        /// </summary>
        public CardView GetCard()
        {
            return HasItemInPool ? GetItem() : CreateInstance();
        }
    }
}
