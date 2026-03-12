# Architecture Overview

This document describes core architectural decisions for the OurGame farming project.

## Core Principles

1. **Data-Driven Design**
   - Game data (crops, items, etc.) are defined as `ScriptableObject` assets.
   - Systems read from these assets at runtime, enabling designers to tweak values without code changes.

2. **Singleton Managers**
   - Global services (e.g. `GameManager`, `SaveManager`, `AudioManager`) inherit from `SingletonMono<T>`.
   - Instances are created automatically or via scene prefabs and persist across scene loads.

3. **Event-Driven Communication**
   - `GameEvents` static class declares `Action` events.
   - Systems raise events (e.g. `GameEvents.CropPlanted(data)`) and other systems subscribe.
   - For inspector-driven connections, `GameEvent` assets can be raised from components.

4. **Folder Structure**
   ```
   Assets/
    └─ Scripts/
       ├─ Core/        # managers, utility classes, event aggregator
       ├─ Farming/     # crop and farm-related logic
       ├─ Player/      # player controller, inventory
       ├─ UI/          # UI controllers, views
       └─ Utils/       # helper extensions
   ```

5. **Namespaces**
   - Use `OurGame.Core`, `OurGame.Farming`, etc.
   - Keeps code searchable and reduces conflicts.

## Getting Started

1. Clone the repo and open the project in Unity.
2. Create necessary manager prefabs or rely on the automatic creation via `SingletonMono`.
3. Use the right-click menu to create data assets (see `/Assets/ScriptableObjects`).
4. Write systems that depend on the `GameEvents` class for loose coupling.

---
This document will evolve as the codebase expands.
