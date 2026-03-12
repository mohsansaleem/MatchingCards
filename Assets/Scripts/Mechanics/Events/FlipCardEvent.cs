using MatchingCards.Core;
using MatchingCards.Model;

namespace MatchingCards.Mechanics.Events
{
    /// <summary>
    /// Fired when the player clicks a face-down card.
    ///
    /// Responsibilities:
    ///   1. Guard against invalid flips (already flipped / matched).
    ///   2. Update the model and trigger the card's flip animation.
    ///   3. When a pair is ready, schedule a <see cref="CheckMatchEvent"/>
    ///      after the flip animation completes.
    ///
    /// Saving is intentionally NOT done here; the player triggers saves
    /// explicitly via the in-game menu.
    ///
    /// Sound hook: subscribe to the static <c>FlipCardEvent.OnExecute</c>
    /// to play the flip sound effect without coupling the audio system here.
    /// </summary>
    public class FlipCardEvent : Simulation.Event<FlipCardEvent>
    {
        public int CardIndex;

        /// <summary>
        /// Delay (seconds) before the match check fires.
        /// Must be >= total flip animation time (HalfFlipDuration * 2 = 0.30 s).
        /// </summary>
        const float CheckDelay = 0.35f;

        /// <summary>
        /// Checked before Execute AND before OnExecute fires, so the flip sound
        /// is never triggered for an already-flipped or matched card.
        /// </summary>
        public override bool Precondition()
        {
            return Simulation.GetModel<MatchingCardsModel>().CanFlipCard(CardIndex);
        }

        public override void Execute()
        {
            var model = Simulation.GetModel<MatchingCardsModel>();

            // 1. Update model
            model.FlipCard(CardIndex);

            // 2. Animate the card view
            CardBoardController.Instance?.GetCardView(CardIndex)?.FlipFaceUp();

            // 3. If a pair is now ready, schedule comparison
            if (model.TryConsumePendingPair(out int indexA, out int indexB))
            {
                var checkEv = Simulation.Schedule<CheckMatchEvent>(CheckDelay);
                checkEv.IndexA = indexA;
                checkEv.IndexB = indexB;
            }
        }

        internal override void Cleanup()
        {
            CardIndex = -1;
        }
    }
}
