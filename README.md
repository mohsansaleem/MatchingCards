# Matching Cards

A Unity memory card game for Desktop and Android/iOS.

**Engine:** Unity 2022.3 LTS &nbsp;|&nbsp; **Targets:** Windows · macOS · Linux · Android · iOS

---

## Table of Contents

1. [Features](#features)
2. [Gameplay](#gameplay)
3. [Architecture](#architecture)
4. [Key Design Decisions](#key-design-decisions)
5. [Project Structure](#project-structure)
6. [Getting Started](#getting-started)

---

## Features

- **Non-blocking flip animations** — players can flip new cards while a previous pair's comparison is still animating; events queue and resolve in order
- **Configurable grid layouts** — any row × column size (2×2, 3×3, 5×6, …); cards scale uniformly to fill the board area and the grid is always centred
- **Irregular grids** — individual cells can be left empty per layout (e.g. two hollow centre cells in a 4×5 grid)
- **Stage progression** — completing a grid auto-advances to the next layout while preserving the accumulated score
- **Combo scoring** — consecutive matches without a mismatch multiply the score; any miss resets the streak
- **Save / Load** — full game state persists to disk; an existing save survives starting a new game with a different grid
- **Pause menu** — Save, Load, and Return-to-Menu available at any time; simulation pauses while the menu is open
- **Sound effects** — flip, match, mismatch, game over

---

## Gameplay

The player flips face-down cards two at a time. Matching pairs stay revealed; mismatched cards flip back after a short delay. Clearing every pair on a grid completes the current stage and the next layout is loaded automatically. The final stage returns to the main menu.

The main menu shows a grid layout picker, a **Play** button (starts fresh), and a **Load** button (resumes the last save — disabled when no save exists). Pressing **Escape** during play opens the pause menu.

---

## Architecture

The codebase is split into five layers with strict one-way dependencies (upper layers may not import lower ones):

```
Config  ──────────────────────────────────────────────────────────
  ScriptableObject holding all data: card sprites, audio clips,
  grid definitions, and score constants. No runtime logic.

Model  ───────────────────────────────────────────────────────────
  Pure-C# serialisable game state — card list, flip/match flags,
  score, combo count, move count, stage index, empty cell indices.
  Zero Unity dependencies; round-trips cleanly through JsonUtility.

Core  ────────────────────────────────────────────────────────────
  Discrete-event Simulation (schedule, tick, pool), generic
  object pool base class, SaveSystem. Framework utilities only.

Mechanics  ───────────────────────────────────────────────────────
  GameController, CardBoardController, CardPool, FlipCardEvent,
  CheckMatchEvent. Owns the game loop and mutates the model via
  scheduled events. Imports no UI types.

UI / View  ───────────────────────────────────────────────────────
  MetaGameController, GridLayoutSelectorController,
  ScoreHUDController, CardView. Reads the model and reacts to
  events; never mutates game state directly.
```

### Discrete-Event Simulation

Game logic runs through `Simulation.Tick()` (called in `GameController.Update`) rather than scattered `MonoBehaviour.Update` polling. Events are allocated from per-type pools, stamped with a future `Time.time`, pushed onto a min-heap priority queue, and executed in timestamp order.

```
Player click
    │
    ▼
CardBoardController.OnCardViewClicked
    │  Simulation.Schedule<FlipCardEvent>()
    ▼
FlipCardEvent.Execute()              ← runs on next Tick()
    ├─ model.FlipCard(index)
    ├─ CardView.FlipFaceUp()          ← starts animation coroutine
    └─ if pair ready →
         Simulation.Schedule<CheckMatchEvent>(delay: 0.35 s)
              │
              ▼
         CheckMatchEvent.Execute()   ← runs after animation ends
              ├─ match  → model.RegisterMatch()    → view.SetMatched()
              └─ miss   → model.RegisterMismatch() → view.FlipFaceDown()
```

The 0.35 s delay gives the flip animation time to finish before the outcome is applied. Multiple pairs can be in flight simultaneously; they resolve in FIFO order automatically.

### Object Pooling

`CardPool` extends `GameObjectsPool<CardView>`. On board clear every `CardView` is returned to the pool; on board build views are re-initialised and re-parented — no `Instantiate` or `Destroy` calls occur. GC pressure stays flat across stage transitions.

### Save System — Sprite Re-linking

`JsonUtility` serialises all value-type fields of `MatchingCardsModel` correctly but silently drops `UnityEngine.Object` references (Sprites inside `CardMeta`). On load, `SaveSystem.RelinkCardMetas` restores each card's sprite using `availableMetas[card.PairId % count]` — the same mapping used when the cards were originally built — so assets are re-linked correctly without storing paths or GUIDs in the save file.

### Grid Scaling & Centering

`CardBoardController.ComputeCellSize` fits a uniform square cell into the board container:

```
cellSide = min(
    (containerWidth  − spacing × (cols + 1)) / cols,
    (containerHeight − spacing × (rows + 1)) / rows
)
```

`ComputeGridOffset` then centres the bounding box inside the container so smaller grids sit in the middle rather than the top-left corner.

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Discrete-event simulation | Game logic is deterministic and time-ordered without touching `Update`. Events are pooled, so no allocations occur at runtime after warm-up. |
| Pure-C# model | `MatchingCardsModel` has no Unity dependencies, serialises cleanly with `JsonUtility`, and is straightforward to unit-test outside Play mode. |
| `GameConfig` ScriptableObject | All designer-facing data lives in one Inspector asset. Grid layouts, score values, and card sprites can be changed without touching code. |
| Static C# events for cross-layer signals | `GameController.OnStagePassed` / `OnGameCompleted` let the UI layer respond to game outcomes without the Mechanics layer importing any UI namespace. |
| FIFO pending-pair queue | Players can flip a third card immediately after the second; each consecutive pair forms an independent comparison that resolves in arrival order. |
| Empty cells stored as flat indices in the model | The board can be fully reconstructed from the saved model alone, with no dependency on the original `GameConfig` layout at load time. |
| Save preserved across new-game starts | Starting a new game with a different grid does not delete an existing save. The save is only overwritten by an explicit in-game Save or by completing the full game. |

---

## Project Structure

```
Assets/
├── Prefabs/
│   ├── CardView.prefab             # UI card: front/back images + Button + CardView
│   └── LayoutButton.prefab         # Grid selector toggle button
├── Resources/
│   └── GameConfig.asset            # ScriptableObject: all designer-facing config
├── Scenes/
│   └── Game.unity                  # Single scene (rebuild via Tools > MatchingCards > Build Scene)
└── Scripts/
    ├── Audio/
    │   └── AudioManager.cs         # Subscribes to simulation events; plays 4 SFX
    ├── Config/
    │   └── GameConfig.cs           # ScriptableObject + CardMeta + GridLayout types
    ├── Core/
    │   ├── Simulation.cs           # Discrete-event engine: schedule, tick, event pool
    │   ├── HeapQueue.cs            # Min-heap backing the event queue
    │   ├── GameObjectsPool.cs      # Generic MonoBehaviour object pool
    │   └── SaveSystem.cs           # JSON save/load with Sprite re-linking
    ├── Editor/
    │   ├── SceneBuilderEditor.cs   # Programmatic scene + prefab builder
    │   └── GameConfigPopulator.cs  # Bulk-populates GameConfig from asset folders
    ├── Mechanics/
    │   ├── GameController.cs       # Model owner, save/load API, stage + game events
    │   ├── CardBoardController.cs  # Visual grid: pooled views, centering, empty cells
    │   ├── CardPool.cs             # CardView prefab pool
    │   └── Events/
    │       ├── FlipCardEvent.cs    # Updates model + view; schedules match check
    │       └── CheckMatchEvent.cs  # Resolves match/mismatch; fires completion signal
    ├── Model/
    │   └── MatchingCardsModel.cs   # Serialisable game state (cards, score, stage)
    ├── UI/
    │   ├── MetaGameController.cs            # 3-state app machine (Bootstrap/Gameplay/Pause)
    │   ├── GridLayoutSelectorController.cs  # Layout toggle group
    │   └── ScoreHUDController.cs            # Live score / combo / move display
    └── View/
        └── CardView.cs             # Flip animation, face swap, matched visual state
```

---

## Getting Started

### Running in the Editor

1. Clone the repository and open the project in **Unity 2022.3 LTS**.
2. If the scene is empty, run **Tools → MatchingCards → Build Scene** to generate prefabs, wire all serialised references, and register the scene in Build Settings.
3. Select `Assets/Resources/GameConfig.asset` and assign card face sprites and audio clips in the Inspector.
4. Press **Play**.

### Building for Android / iOS

The project uses only built-in Unity APIs and TextMeshPro (bundled with the Editor), so no additional packages are required. Switch the active platform in **File → Build Settings**, configure bundle ID and icons in Player Settings, and build.

### Save File Location

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\..\LocalLow\<Company>\<Product>\MatchingCards.json` |
| macOS | `~/Library/Application Support/<Company>/<Product>/MatchingCards.json` |
| Android | App internal storage data directory |
| iOS | Application sandbox Documents folder |
