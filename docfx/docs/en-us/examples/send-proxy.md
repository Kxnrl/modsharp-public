# SendProxyManager

This tutorial demonstrates how to use `ISendProxyManager` to override a networked field's value **per client** as it is sent, without changing the real value on the server.

Register a field on an entity with `Hook`; the callback runs on the main thread **once per tick** for that field (not once per client), and fills the value each client should see via `batch.SetFor(client, value)` — clients you don't set receive the real value. Set the value matching `batch.Kind`. Supported kinds: int, float, bool, qangle, and string (e.g. a per-viewer player name). The example below shows each.

[!code-csharp[SendProxyExample.cs](../../codes/send-proxy.cs)]
