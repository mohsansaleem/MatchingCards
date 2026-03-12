using MatchingCards.Config;
using MatchingCards.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace MatchingCards.UI
{
    /// <summary>
    /// Top-level application state machine with three mutually-exclusive states:
    ///
    ///   Bootstrap  — shown on launch; player picks a grid layout and presses
    ///                Play (new game) or Load (resume saved game).
    ///
    ///   Gameplay   — card board + score HUD are visible; Escape pauses.
    ///
    ///   PauseMenu  — translucent overlay with Save / Load / Return-to-Menu
    ///                buttons; simulation is paused (timeScale = 0).
    ///
    /// Transitions
    ///   Bootstrap  ──Play──►  Gameplay
    ///   Bootstrap  ──Load──►  Gameplay   (only if save exists)
    ///   Gameplay   ──Esc───►  PauseMenu
    ///   PauseMenu  ──Esc───►  Gameplay
    ///   PauseMenu  ──Load──►  Gameplay
    ///   PauseMenu  ──Menu──►  Bootstrap
    ///   Gameplay   ──Win───►  Bootstrap  (triggered by CheckMatchEvent)
    /// </summary>
    public class MetaGameController : MonoBehaviour
    {
        public static MetaGameController Instance { get; private set; }

        // ── Bootstrap UI ──────────────────────────────────────────────────────

        [Header("Bootstrap")]
        [Tooltip("Root GameObject of the bootstrap / start screen.")]
        [SerializeField] GameObject _bootstrapPanel;

        [Tooltip("The Load button on the bootstrap screen — disabled when no save exists.")]
        [SerializeField] Button _bootstrapLoadButton;

        [Tooltip("Reads the player's layout selection on the bootstrap screen.")]
        [SerializeField] GridLayoutSelectorController _layoutSelector;

        // ── In-game pause menu ────────────────────────────────────────────────

        [Header("Pause Menu")]
        [Tooltip("Panel shown when the player presses Escape during gameplay.")]
        [SerializeField] GameObject _pauseMenuPanel;

        // ── Gameplay canvases ─────────────────────────────────────────────────

        [Header("Gameplay")]
        [Tooltip("All canvases that should be visible only during active gameplay " +
                 "(card board, score HUD, etc.).")]
        [SerializeField] Canvas[] _gameplayCanvases;

        // ── References ────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] GameController        _gameController;
        [SerializeField] CardBoardController   _cardBoardController;
        [SerializeField] ScoreHUDController    _scoreHUD;
        [SerializeField] GameConfig            _config;

        /// <summary>
        /// The active game config. Exposed so other UI components (e.g.
        /// <see cref="GridLayoutSelectorController"/>) can read it through
        /// <c>MetaGameController.Instance.Config</c> instead of holding their
        /// own serialized reference.
        /// </summary>
        public GameConfig Config => _config;

        // ── State ─────────────────────────────────────────────────────────────

        enum AppState { Bootstrap, Gameplay, PauseMenu }
        AppState _state;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void OnEnable()
        {
            Instance = this;
            GameController.OnStagePassed   += HandleStagePassed;
            GameController.OnGameCompleted += HandleGameCompleted;
            EnterBootstrap();
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            GameController.OnStagePassed   -= HandleStagePassed;
            GameController.OnGameCompleted -= HandleGameCompleted;
        }

        void Update()
        {
            // Escape only acts during gameplay (toggle pause) or pause menu (resume).
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            if      (_state == AppState.Gameplay)  EnterPauseMenu();
            else if (_state == AppState.PauseMenu) EnterGameplay();
        }

        // ── State transitions ─────────────────────────────────────────────────

        void EnterBootstrap()
        {
            _state = AppState.Bootstrap;

            // Activate the panel first so the button is enabled when we set
            // interactable — Unity's DoStateTransition only fires on active objects.
            SetPanelsActive(bootstrap: true, gameplay: false, pauseMenu: false);

            // Refresh Load button — only enabled when a save file exists.
            if (_bootstrapLoadButton != null)
                _bootstrapLoadButton.interactable = _gameController.HasSave;

            Time.timeScale = 1;
        }

        void EnterGameplay()
        {
            _state = AppState.Gameplay;
            SetPanelsActive(bootstrap: false, gameplay: true, pauseMenu: false);
            Time.timeScale = 1;
        }

        void EnterPauseMenu()
        {
            _state = AppState.PauseMenu;
            SetPanelsActive(bootstrap: false, gameplay: false, pauseMenu: true);
            Time.timeScale = 0;
        }

        // ── Panel visibility helper ───────────────────────────────────────────

        void SetPanelsActive(bool bootstrap, bool gameplay, bool pauseMenu)
        {
            if (_bootstrapPanel  != null) _bootstrapPanel .SetActive(bootstrap);
            if (_pauseMenuPanel  != null) _pauseMenuPanel .SetActive(pauseMenu);

            foreach (var canvas in _gameplayCanvases)
                if (canvas != null) canvas.gameObject.SetActive(gameplay);
        }

        // ── Bootstrap button handlers ─────────────────────────────────────────

        /// <summary>
        /// Called by the Play button on the bootstrap screen.
        /// Starts a fresh game using the layout selected in the grid picker.
        /// </summary>
        public void OnBootstrapPlayButtonClicked()
        {
            var layout = _layoutSelector != null
                ? _layoutSelector.SelectedLayout
                : (_config.GridLayouts.Count > 0 ? _config.GridLayouts[0] : null);

            if (layout == null)
            {
                Debug.LogWarning("[MetaGameController] No layout selected — cannot start game.");
                return;
            }

            _gameController.StartNewGame(layout, _config);

            // Activate the gameplay canvas BEFORE building the board so that
            // _boardContainer.rect is registered as dirty by Unity's layout
            // system. CardBoardController.InitBoard then calls
            // Canvas.ForceUpdateCanvases() to flush the layout immediately.
            EnterGameplay();
            _cardBoardController.InitBoard(_gameController.model);
            _scoreHUD?.Refresh(_gameController.model);
        }

        /// <summary>
        /// Called by the Load button on the bootstrap screen.
        /// Restores the last saved game and jumps straight into gameplay.
        /// Does nothing (and logs a warning) if no save exists.
        /// </summary>
        public void OnBootstrapLoadButtonClicked()
        {
            if (!_gameController.TryLoadGame(_config))
            {
                Debug.LogWarning("[MetaGameController] Load requested but no save found.");
                return;
            }

            EnterGameplay();  // activate canvas first — see OnBootstrapPlayButtonClicked
            _cardBoardController.InitBoard(_gameController.model);
            _scoreHUD?.Refresh(_gameController.model);
        }

        // ── Pause-menu button handlers ────────────────────────────────────────

        /// <summary>
        /// Called by the Resume button. Closes the pause menu and resumes play.
        /// </summary>
        public void OnResumeButtonClicked()
        {
            if (_state == AppState.PauseMenu)
                EnterGameplay();
        }

        /// <summary>
        /// Called by the Save button inside the pause menu.
        /// Persists the current game state; keeps the menu open.
        /// </summary>
        public void OnSaveButtonClicked()
        {
            _gameController.SaveGame();
        }

        /// <summary>
        /// Called by the Load button inside the pause menu.
        /// Restores the last save, rebuilds the board, and resumes gameplay.
        /// Does nothing if no save exists.
        /// </summary>
        public void OnPauseMenuLoadButtonClicked()
        {
            if (!_gameController.TryLoadGame(_config)) return;

            EnterGameplay();  // activate canvas first — see OnBootstrapPlayButtonClicked
            _cardBoardController.InitBoard(_gameController.model);
            _scoreHUD?.Refresh(_gameController.model);
        }

        /// <summary>
        /// Called by the "Return to Menu" button inside the pause menu.
        /// Abandons the current session and returns to the bootstrap screen.
        /// Note: unsaved progress is lost — the player must Save first if needed.
        /// </summary>
        public void OnReturnToMenuButtonClicked()
        {
            EnterBootstrap();
        }

        // ── Stage/game-over (raised by GameController events) ─────────────────

        /// <summary>
        /// Subscribed to <see cref="GameController.OnStagePassed"/>.
        /// Automatically advances to the next stage, preserving the score.
        /// </summary>
        void HandleStagePassed()
        {
            _gameController.AdvanceToNextStage(_config);
            EnterGameplay();
            _cardBoardController.InitBoard(_gameController.model);
            _scoreHUD?.Refresh(_gameController.model);

            // Auto-save so the Load button stays enabled if the player returns to menu.
            _gameController.SaveGame();
        }

        /// <summary>
        /// Subscribed to <see cref="GameController.OnGameCompleted"/>.
        /// Returns to the bootstrap screen when the last stage is finished.
        /// Save deletion is handled by <see cref="GameController.NotifyGameCompleted"/>
        /// before this callback fires, so no save management is needed here.
        /// </summary>
        void HandleGameCompleted() => EnterBootstrap();

        // ── Legacy shim (kept so any existing wiring still compiles) ──────────

        /// <summary>
        /// Retained for backward compatibility with any existing Inspector wiring.
        /// Prefer <see cref="OnPauseMenuLoadButtonClicked"/> for new wiring.
        /// </summary>
        public void OnLoadButtonClicked() => OnPauseMenuLoadButtonClicked();

        /// <summary>
        /// Retained for backward compatibility.
        /// Prefer <see cref="OnResumeButtonClicked"/> or Escape to dismiss the pause menu.
        /// </summary>
        public void ToggleMainMenu(bool show)
        {
            if (show) EnterPauseMenu();
            else      EnterGameplay();
        }
    }
}
