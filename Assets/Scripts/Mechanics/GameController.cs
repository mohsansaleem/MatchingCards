using MatchingCards.Config;
using MatchingCards.Core;
using MatchingCards.Model;
using UnityEngine;
using GridLayout = MatchingCards.Config.GridLayout;

namespace MatchingCards.Mechanics
{
    /// <summary>
    /// This class exposes the game model in the inspector, ticks the simulation,
    /// and provides save/load access for the active game state.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        // This model field is public and can therefore be modified in the inspector.
        // The reference actually comes from the InstanceRegister, and is shared
        // through the simulation and events. Unity will deserialize over this
        // shared reference when the scene loads, allowing the model to be
        // conveniently configured inside the inspector.
        public MatchingCardsModel model = Simulation.GetModel<MatchingCardsModel>();

        /// <summary>
        /// Raised (on the main thread) when the last card pair is matched and
        /// the game is complete. Subscribe here to react to game-over without
        /// creating an upward dependency on the UI layer.
        /// </summary>
        public static event System.Action OnGameCompleted;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void OnEnable()  => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        void Update()
        {
            Simulation.Tick();
        }

        // ── New game ──────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the model for a brand-new game and clears any stale save.
        /// Call <see cref="CardBoardController.InitBoard"/> immediately after
        /// to rebuild the visual board from the freshly initialised model.
        /// </summary>
        /// <param name="layout">Grid dimensions to play.</param>
        /// <param name="config">Game config supplying card metas and score values.</param>
        public void StartNewGame(GridLayout layout, GameConfig config)
        {
            model.Init(
                layout.Rows,
                layout.Columns,
                config.CardMetaOptions,
                config.BaseMatchScore,
                config.ComboScoreBonus);

            // A stale save from a previous session is no longer valid.
            SaveSystem.DeleteSave();
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        /// <summary>
        /// Persists the current model state to disk.
        /// Call this after any meaningful state change (match, mismatch, game over).
        /// </summary>
        public void SaveGame()
        {
            SaveSystem.Save(model);
        }

        /// <summary>
        /// Attempts to restore a previously saved model from disk.
        /// On success the loaded model replaces the current one in both the
        /// inspector field and the Simulation's InstanceRegister.
        /// </summary>
        /// <param name="config">
        /// The active <see cref="GameConfig"/>; its card meta list is used to
        /// re-link Sprite references that were stripped during serialization.
        /// </param>
        /// <returns>True when a save was found and loaded successfully.</returns>
        public bool TryLoadGame(GameConfig config)
        {
            if (!SaveSystem.TryLoad(config.CardMetaOptions, out var loaded))
                return false;

            model = loaded;
            Simulation.SetModel(loaded);
            return true;
        }

        /// <summary>Deletes the save file from disk.</summary>
        public void DeleteSave() => SaveSystem.DeleteSave();

        /// <summary>True when a save file exists on disk.</summary>
        public bool HasSave => SaveSystem.HasSave;

        // ── Game-over notification ────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="CheckMatchEvent"/> when the last pair is matched.
        /// Clears the finished save and raises <see cref="OnGameCompleted"/> so
        /// that higher-level systems (e.g. MetaGameController) can react without
        /// being directly coupled to the mechanics layer.
        /// </summary>
        public void NotifyGameCompleted()
        {
            DeleteSave();
            OnGameCompleted?.Invoke();
        }
    }
}
