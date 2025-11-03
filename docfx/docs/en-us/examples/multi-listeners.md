# Multi Listeners

This tutorial demonstrates how to implement multiple Listeners in one Module.

> [!TIP]
>
> Because multiple Listeners all contain `ListenerVersion` and `ListenerPriority`,  
> we explicitly implement the interfaces to distinguish the priority and version of each Listener.

[!code-csharp[MultiListenerExample.cs](../../codes/multi-listeners.cs)]
