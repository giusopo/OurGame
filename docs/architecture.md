# Architecture Overview

This document describes core architectural decisions for the OurGame farming project.

## Core Principles

1. **Data-Driven Design**
   - Game data (crops, items, etc.) are defined as `ScriptableObject` assets.
   - Systems read from these assets at runtime, enabling designers to tweak values without code changes.

2. **Singleton Managers**
   - Global services (e.g. `GameManager`, `SaveManager`, `TimeManager`) inherit from `SingletonMono<T>`.
   - Instances are created automatically or via scene prefabs and persist across scene loads.

3. **Event-Driven Communication**
   - `GameEvents` static class declares `Action` events.
   - Systems raise events and other systems subscribe.
   - For inspector-driven connections, `GameEvent` assets can be raised from components.

4. **Folder Structure**
   ```text
   Assets/Scripts/
     Core/        managers, domain state, save/load, schedulers
     Gameplay/    farming, items, creatures, world interactions
     Player/      player movement and input-facing interaction logic
     Systems/     shared runtime systems such as inventory
     UI/          UI controllers, views, enums and presentation helpers
     Utils/       scene tools and lightweight utility behaviours
   ```

5. **Namespace Strategy**
   - Prefer explicit namespaces for shared, non-scene-bound code such as data structures, save payloads and system models.
   - Introduce namespaces in low-risk code first; avoid mass namespace moves on scene-bound `MonoBehaviour` classes unless scene/prefab migration is planned.
   - This keeps code searchable and reduces conflicts without breaking serialized Unity references.

6. **Scene Tools vs Runtime**
   - Keep one-off terrain/spawn helpers clearly marked as tools via inspector naming and placement.
   - Remove empty placeholder systems quickly so the folder structure reflects the real runtime architecture.

## Getting Started

1. Clone the repo and open the project in Unity.
2. Verify manager bootstrap in the main scene before adding new systems.
3. Create data assets through the relevant `CreateAssetMenu` entries.
4. Prefer extending existing runtime systems over adding parallel placeholders.

---
This document should evolve together with the codebase.
