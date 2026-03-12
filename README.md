# OurGame
OurGame

## Project Architecture
This repository follows a modular, event-driven structure designed for maintainability as the game scales. Key concepts:

- **Assets/Scripts/** is organized into subfolders such as `Core`, `Farming`, `Player`, `UI`, and `Utils`.
- **Core** contains singleton managers (`GameManager`, `SaveManager`, etc.) and shared utilities.
- **ScriptableObjects** store data assets and event assets for data-driven design.
- **GameEvents** static class provides a pub-sub event system; systems communicate via events rather than tight coupling.

### Usage
- Create `ScriptableObject` assets using the right-click menu (e.g. Farming/Crop Data).
- Managers implement `SingletonMono<>` and persist across scenes.
- To respond to events, subscribe to `GameEvents` or use `GameEvent` assets.

Refer to `docs/architecture.md` for more details (coming soon).
