# Multiple Listeners in One Module

Use the [Hello World project settings](hello-world.md).

One module can implement several listener interfaces. Explicitly implement each interface's `ListenerVersion` and `ListenerPriority` so they can have independent versions and priorities.

Register the module once with each corresponding manager and remove every registration in `Shutdown`. `IEventListener.FireGameEvent` is required; the sample logs event names. Other listener callbacks can use their interface defaults when not needed.

[!code-csharp[multi-listeners.cs](../../codes/multi-listeners.cs)]
