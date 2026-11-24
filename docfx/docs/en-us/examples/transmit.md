# Transmit Manager

This example automatically hides teammates while leaving enemies and the receiver's own player visible. It does **not** register a `.hide` command. Use the [Hello World project settings](hello-world.md).

Hook player **controllers**, not pawns: the native transmit manager rejects direct pawn hooks. In `SetEntityState`, `true` means transmit/visible, `false` means blocked, and channel `-1` changes all channels.

A repeating timer handles existing players, late joins, and team changes, and removes stale entity wrappers after map changes. Transmit hooks are global per entity rather than module-scoped: the example skips entities already hooked by another module, tracks only the hooks it created, and removes them in `Shutdown`. Coordinate ownership if multiple modules need to modify the same entity's transmit state.

[!code-csharp[Transmit.cs](../../codes/transmit.cs)]
