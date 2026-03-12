using MatchingCards.Config;
using MatchingCards.Core;
using MatchingCards.Mechanics.Events;
using MatchingCards.Model;
using UnityEngine;

namespace MatchingCards.Audio
{
    /// <summary>
    /// Plays the four required sound effects by subscribing to simulation event
    /// callbacks. No direct coupling to gameplay logic — sounds are driven purely
    /// by event hooks so the audio layer stays independent.
    ///
    /// Audio clips are defined in <see cref="GameConfig"/> so all game assets
    /// live in one place.
    ///
    /// Sounds triggered:
    ///   • Flip     — every valid card flip        (FlipCardEvent.OnExecute)
    ///   • Match    — a matched pair, game ongoing  (CheckMatchEvent.OnExecute, WasMatch)
    ///   • Game Over— the final pair, game complete (CheckMatchEvent.OnExecute, WasMatch + IsComplete)
    ///   • Mismatch — a non-matching pair           (CheckMatchEvent.OnExecute, !WasMatch)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] GameConfig _config;

        AudioSource _audioSource;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        void OnEnable()
        {
            Instance = this;
            FlipCardEvent.OnExecute   += HandleFlip;
            CheckMatchEvent.OnExecute += HandleCheckMatch;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            FlipCardEvent.OnExecute   -= HandleFlip;
            CheckMatchEvent.OnExecute -= HandleCheckMatch;
        }

        // ── Event handlers ────────────────────────────────────────────────────

        void HandleFlip(FlipCardEvent ev)
        {
            Play(_config.FlipClip);
        }

        void HandleCheckMatch(CheckMatchEvent ev)
        {
            if (ev.WasMatch)
            {
                // IsComplete is already updated in the model when OnExecute fires
                bool isGameOver = Simulation.GetModel<MatchingCardsModel>().IsComplete;
                Play(isGameOver ? _config.GameOverClip : _config.MatchClip);
            }
            else
            {
                Play(_config.MismatchClip);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void Play(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;
            _audioSource.PlayOneShot(clip);
        }
    }
}
