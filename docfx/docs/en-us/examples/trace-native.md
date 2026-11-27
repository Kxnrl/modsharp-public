# Physics Query (Native)

Use the [Hello World project settings](hello-world.md) and enable `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`. No generator package is needed. Keep the assembly's `DisableRuntimeMarshalling` attribute: this example passes an unmanaged callback with a native boolean return value.

The example registers client chat commands `.trace_line` and `.trace_custom` (client console equivalents: `ms_trace_line`, `ms_trace_custom`). Add any argument to enable a filter that accepts CT player pawns and non-player geometry. The first command traces a line; the second demonstrates a hull with custom `RnQueryShapeAttr` flags. Both ignore the calling pawn.

Native traces expose the engine's query attributes and raw hit entity pointer. A geometry hit can have no entity pointer; check it before constructing an entity wrapper. Callbacks marked `UnmanagedCallersOnly` must be static and must not allow managed exceptions to cross the native boundary. Trace-result native pointers are not safe to retain after the operation.

For managed filters and ready-made helpers, see [Physics Query (Wrapper)](trace-wrapper.md). Use the current SDK's `InteractionLayers` and built-in query attributes rather than copied numeric masks from an old CS2 build.

[!code-csharp[TraceNative.cs](../../codes/trace-native.cs)]
