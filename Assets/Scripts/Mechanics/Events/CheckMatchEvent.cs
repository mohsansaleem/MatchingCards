using MatchingCards.Core;
using MatchingCards.Model;

namespace MatchingCards.Mechanics.Events
{
    /// <summary>
    /// Fired after a pair of face-up cards has finished animating.
    /// Determines whether the pair is a match or a mismatch and updates
    /// both the model and the card views accordingly.
    ///
    /// Saving is intentionally NOT done here; the player triggers saves
    /// explicitly via the in-game menu.
    ///
    /// Sound hooks: subscribe to the static <c>CheckMatchEvent.OnExecute</c>
    /// and read <see cref="WasMatch"/> to decide which sound to play
    /// (match / mismatch / game-over) without coupling the audio system here.
    /// </summary>
    public class CheckMatchEvent : Simulation.Event<CheckMatchEvent>
    {
        public int IndexA;
        public int IndexB;

        /// <summary>
        /// Set during Execute. Readable by OnExecute subscribers (e.g. audio)
        /// to distinguish match from mismatch without re-querying the model.
        /// </summary>
        public bool WasMatch { get; private set; }

        public override void Execute()
        {
            var model = Simulation.GetModel<MatchingCardsModel>();
            var board = CardBoardController.Instance;

            WasMatch = model.IsMatchingPair(IndexA, IndexB);

            if (WasMatch)
            {
                model.RegisterMatch(IndexA, IndexB);

                board?.GetCardView(IndexA)?.SetMatched();
                board?.GetCardView(IndexB)?.SetMatched();

                if (model.IsComplete)
                    OnGameComplete();
            }
            else
            {
                model.RegisterMismatch(IndexA, IndexB);

                board?.GetCardView(IndexA)?.FlipFaceDown();
                board?.GetCardView(IndexB)?.FlipFaceDown();
            }
        }

        internal override void Cleanup()
        {
            IndexA   = -1;
            IndexB   = -1;
            WasMatch = false;
        }

        // ── Private ───────────────────────────────────────────────────────────

        /// <summary>
        /// Delegates the game-complete state entirely to GameController, which
        /// owns the save lifecycle and broadcasts <see cref="GameController.OnGameCompleted"/>
        /// for any subscriber (e.g. MetaGameController) to react to.
        /// No UI types are imported here — the Mechanics layer stays self-contained.
        /// </summary>
        static void OnGameComplete()
        {
            GameController.Instance?.NotifyGameCompleted();
        }
    }
}
