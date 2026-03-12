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
    ///
    /// Stage flow
    ///   • <see cref="StartNewGame"/>  — fresh game starting at a chosen layout.
    ///   • <see cref="AdvanceToNextStage"/> — preserve score, move to next layout.
    ///   • <see cref="NotifyGameCompleted"/> — called by CheckMatchEvent when all
    ///     pairs are matched. Fires <see cref="OnStagePassed"/> when more stages
    ///     remain, or <see cref="OnGameCompleted"/> when the final stage is done.
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

        // ── Static events ─────────────────────────────────────────────────────

        /// <summary>
        /// Raised when a stage is completed and at least one more stage remains.
        /// Subscribe here to auto-advance the board without coupling to the UI.
        /// </summary>
        public static event System.Action OnStagePassed;

        /// <summary>
        /// Raised when the last stage is completed (or when no stage config is
        /// available). Subscribe here to return to the bootstrap screen.
        /// </summary>
        public static event System.Action OnGameCompleted;

        // ── Stage tracking ────────────────────────────────────────────────────

        /// <summary>0-based index of the stage currently being played.</summary>
        public int CurrentStageIndex { get; private set; }

        GameConfig _config;

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
        /// <param name="layout">Starting grid layout (stage).</param>
        /// <param name="config">Game config supplying card metas and score values.</param>
        public void StartNewGame(GridLayout layout, GameConfig config)
        {
            _config = config;
            CurrentStageIndex = config.GridLayouts.IndexOf(layout);

            model.Init(
                layout.Rows,
                layout.Columns,
                layout.EmptyCells,
                config.CardMetaOptions,
                config.BaseMatchScore,
                config.ComboScoreBonus,
                CurrentStageIndex);

            SaveSystem.DeleteSave();
        }

        // ── Stage advancement ─────────────────────────────────────────────────

        /// <summary>
        /// Moves to the next stage, preserving the accumulated score.
        /// Caller is responsible for calling <see cref="CardBoardController.InitBoard"/>
        /// to refresh the visual board after this returns.
        /// </summary>
        public void AdvanceToNextStage(GameConfig config)
        {
            _config = config;
            CurrentStageIndex++;

            var layout = config.GridLayouts[CurrentStageIndex];
            model.InitNextStage(
                layout.Rows,
                layout.Columns,
                layout.EmptyCells,
                config.CardMetaOptions,
                config.BaseMatchScore,
                config.ComboScoreBonus,
                CurrentStageIndex);
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        /// <summary>Persists the current model state to disk.</summary>
        public void SaveGame() => SaveSystem.Save(model);

        /// <summary>
        /// Attempts to restore a previously saved model from disk.
        /// On success the loaded model replaces the current one in both the
        /// inspector field and the Simulation's InstanceRegister.
        /// Also restores <see cref="CurrentStageIndex"/> from the saved model.
        /// </summary>
        public bool TryLoadGame(GameConfig config)
        {
            if (!SaveSystem.TryLoad(config.CardMetaOptions, out var loaded))
                return false;

            model = loaded;
            Simulation.SetModel(loaded);
            _config = config;
            CurrentStageIndex = loaded.StageIndex;
            return true;
        }

        /// <summary>Deletes the save file from disk.</summary>
        public void DeleteSave() => SaveSystem.DeleteSave();

        /// <summary>True when a save file exists on disk.</summary>
        public bool HasSave => SaveSystem.HasSave;

        // ── Game-over notification ────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="CheckMatchEvent"/> when the last pair is matched.
        /// Fires <see cref="OnStagePassed"/> when more stages remain, or
        /// <see cref="OnGameCompleted"/> when all stages are done.
        /// </summary>
        public void NotifyGameCompleted()
        {
            DeleteSave();

            if (_config != null && CurrentStageIndex + 1 < _config.GridLayouts.Count)
                OnStagePassed?.Invoke();
            else
                OnGameCompleted?.Invoke();
        }
    }
}
