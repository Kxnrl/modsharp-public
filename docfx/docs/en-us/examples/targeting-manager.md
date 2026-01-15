# Custom Target Resolver

This example demonstrates how to implement a custom target resolver (specifically `@aim`) for the Targeting Manager.

It covers two advanced concepts:
1.  **Raycasting:** How to trace a line from a player's eyes to find an entity.
2.  **Soft Dependencies:** How to interact with the Targeting Manager only if it is installed, without crashing if it is missing.

## Key Concepts

### 1. Raycasting Logic
To determine who a player is looking at, we perform a raycast.

> [!NOTE]
> It is crucial to set `attr.SetEntityToIgnore(pawn, 0)` so the raycast ignores the shooter themselves.

### 2. Module Interop
We do not hard-reference the Targeting Manager. Instead, we look for its interface via `ITargetingManager.Identity`. We use `OnLibraryConnected` to handle cases where the Targeting Manager is loaded after our module.

## Full Source Code

[!code-csharp[TargetingManager.cs](../../codes/targeting-manager.cs)]