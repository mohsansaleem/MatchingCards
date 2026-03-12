using MatchingCards.Config;
using MatchingCards.Mechanics;
using UnityEngine;

namespace MatchingCards.UI
{
    /// <summary>
    /// Responsible for switching control between high-level application contexts
    /// (Main Menu and Gameplay). Also exposes Save / Load actions that are wired
    /// to menu buttons, making the player the explicit trigger for persistence.
    /// </summary>
    public class MetaGameController : MonoBehaviour
    {
        public static MetaGameController Instance { get; private set; }

        [Header("UI")]
        /// <summary>The main UI object used for the menu.</summary>
        public MainUIController mainMenu;

        /// <summary>Canvas objects active during gameplay (hidden while menu is open).</summary>
        public Canvas[] gamePlayCanvasii;

        [Header("References")]
        public GameController gameController;
        public CardBoardController cardBoardController;

        /// <summary>
        /// The active game config — required for loading (re-links card sprites).
        /// Assign in the Inspector.
        /// </summary>
        public GameConfig config;

        bool _showMainCanvas = false;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void OnEnable()
        {
            Instance = this;
            _ToggleMainMenu(_showMainCanvas);
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ToggleMainMenu(show: !_showMainCanvas);
        }

        // ── Menu toggle ───────────────────────────────────────────────────────

        /// <summary>Turn the main menu on or off (no-op if already in that state).</summary>
        public void ToggleMainMenu(bool show)
        {
            if (_showMainCanvas != show)
                _ToggleMainMenu(show);
        }

        void _ToggleMainMenu(bool show)
        {
            if (show)
            {
                Time.timeScale = 0;
                mainMenu.gameObject.SetActive(true);
                foreach (var canvas in gamePlayCanvasii)
                    canvas.gameObject.SetActive(false);
            }
            else
            {
                Time.timeScale = 1;
                mainMenu.gameObject.SetActive(false);
                foreach (var canvas in gamePlayCanvasii)
                    canvas.gameObject.SetActive(true);
            }

            _showMainCanvas = show;
        }

        // ── Menu button handlers ──────────────────────────────────────────────

        /// <summary>
        /// Called by the Save button. Persists the current game state to disk
        /// and keeps the menu open so the player can continue navigating.
        /// </summary>
        public void OnSaveButtonClicked()
        {
            gameController.SaveGame();
        }

        /// <summary>
        /// Called by the Load button. Restores the last saved game state,
        /// rebuilds the card board, and closes the menu to resume play.
        /// Does nothing if no save file exists.
        /// </summary>
        public void OnLoadButtonClicked()
        {
            if (!gameController.TryLoadGame(config)) return;

            cardBoardController.InitBoard(gameController.model);
            ToggleMainMenu(show: false);
        }
    }
}
