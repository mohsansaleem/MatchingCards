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
        /// Handles the game-complete state: removes the save (progress is done)
        /// and signals the rest of the application via the OnExecute callback.
        /// Wire up UI/score display by subscribing to CheckMatchEvent.OnExecute
        /// and checking model.IsComplete.
        /// </summary>
        static void OnGameComplete()
        {
            // Save is no longer needed once the game is won
            GameController.Instance?.DeleteSave();

            // TODO: raise a game-over event or notify MetaGameController
            // e.g. MetaGameController.Instance?.ToggleMainMenu(show: true);
        }
    }
}
