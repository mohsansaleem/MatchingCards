using System;
using System.Collections;
using MatchingCards.Core;
using MatchingCards.Model;
using UnityEngine;
using UnityEngine.UI;

namespace MatchingCards.View
{
    /// <summary>
    /// Visual representation of a single card. Handles the flip animation and
    /// click input. Implements IPoolItem so it can be recycled by CardPool.
    ///
    /// Dependency note: CardView has no reference to Mechanics or Events —
    /// clicks are surfaced via the OnCardClicked action and wired up by
    /// CardBoardController, keeping View and Mechanics decoupled.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CardView : MonoBehaviour, IPoolItem
    {
        [SerializeField] Image _frontImage;
        [SerializeField] Image _backImage;

        /// <summary>Index into MatchingCardsModel.Cards.</summary>
        public int CardIndex { get; private set; }

        /// <summary>Raised when the player clicks this card.</summary>
        public event Action<int> OnCardClicked;

        const float HalfFlipDuration = 0.15f;

        Button _button;
        Coroutine _flipCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() => OnCardClicked?.Invoke(CardIndex));
        }

        // ── IPoolItem ─────────────────────────────────────────────────────────

        /// <summary>
        /// Resets the card to a neutral state and deactivates it for re-pooling.
        /// </summary>
        public void ReturnToPool()
        {
            OnCardClicked = null;

            if (_flipCoroutine != null)
            {
                StopCoroutine(_flipCoroutine);
                _flipCoroutine = null;
            }

            // Reset scale in case the card was returned mid-animation
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        // ── Initialisation ────────────────────────────────────────────────────

        /// <summary>
        /// Binds the view to a card's data and restores its visual state
        /// instantly (no animation), used when building or reloading the board.
        /// </summary>
        /// <param name="backSprite">
        /// Sprite for the face-down side. When null the sprite already set on
        /// the prefab's back Image component is kept unchanged.
        /// </param>
        public void Init(int index, CardData data, Sprite backSprite = null)
        {
            CardIndex = index;

            if (_frontImage != null) _frontImage.sprite = data.Meta?.Sprite;
            if (_backImage  != null && backSprite != null) _backImage.sprite = backSprite;

            bool faceUp = data.IsFlipped || data.IsMatched;
            ShowFace(faceUp);

            _button.interactable = !data.IsMatched && !data.IsFlipped;
        }

        // ── Flip control ──────────────────────────────────────────────────────

        /// <summary>Animates the card to the face-up position.</summary>
        public void FlipFaceUp()
        {
            _button.interactable = false;
            StartFlip(faceUp: true);
        }

        /// <summary>Animates the card to the face-down position.</summary>
        public void FlipFaceDown()
        {
            StartFlip(faceUp: false);
        }

        /// <summary>
        /// Marks the card as permanently matched:
        /// disables the button and keeps the card face-up.
        /// </summary>
        public void SetMatched()
        {
            _button.interactable = false;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        void StartFlip(bool faceUp)
        {
            if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);
            _flipCoroutine = StartCoroutine(FlipAnimation(faceUp));
        }

        void ShowFace(bool faceUp)
        {
            if (_frontImage != null) _frontImage.gameObject.SetActive(faceUp);
            if (_backImage  != null) _backImage.gameObject.SetActive(!faceUp);
        }

        IEnumerator FlipAnimation(bool faceUp)
        {
            // Phase 1: shrink to flat along X
            yield return AnimateScaleX(1f, 0f, HalfFlipDuration);

            // Swap visible face at the midpoint
            ShowFace(faceUp);

            // Phase 2: expand back to full
            yield return AnimateScaleX(0f, 1f, HalfFlipDuration);

            _flipCoroutine = null;

            // Restore interactivity based on current model state
            var card = Simulation.GetModel<MatchingCardsModel>().Cards[CardIndex];
            _button.interactable = !card.IsMatched && !card.IsFlipped;
        }

        IEnumerator AnimateScaleX(float from, float to, float duration)
        {
            float elapsed = 0f;
            Vector3 scale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                scale.x = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                transform.localScale = scale;
                yield return null;
            }

            scale.x = to;
            transform.localScale = scale;
        }
    }
}
