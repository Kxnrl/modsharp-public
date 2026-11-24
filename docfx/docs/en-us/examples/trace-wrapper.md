# Physics Query (Wrapper)

Use the [Hello World project settings](hello-world.md). The managed wrapper provides line/hull helpers, ignored-entity arguments, and managed filter delegates. For raw engine attributes and unmanaged filters, see [Physics Query (Native)](trace-native.md).

This example runs every 0.1 seconds. Set `ms_show_aim_target_type` to `0` (all), `1` (teammates) or `2` (enemies). The filter excludes the caller while retaining solid geometry, so it does not deliberately trace through walls. The display uses the **target's** health. It also shows an aim position from a query that ignores all players, and reports when the player's hull intersects solid geometry without killing the player.

Use current `InteractionLayers` and `UsefulInteractionLayers` constants rather than old numeric masks. Timers created through `PushTimer` are removed on owner unload according to the callback's assembly load context; callbacks must belong to your module, not a shared assembly with a longer lifetime.

[!code-csharp[TraceWrapper.cs](../../codes/trace-wrapper.cs)]
