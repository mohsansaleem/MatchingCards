using MatchingCards.Core;
using MatchingCards.View;

namespace MatchingCards.Mechanics
{
    /// <summary>
    /// Object pool for CardView instances.
    /// Extends GameObjectsPool so that cards are recycled across board resets
    /// rather than being destroyed and re-instantiated each time.
    /// </summary>
    public class CardPool : GameObjectsPool<CardView>
    {
        /// <summary>
        /// Warms up the pool with <paramref name="initialCount"/> pre-instantiated cards.
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
