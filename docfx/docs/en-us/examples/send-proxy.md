# SendProxyManager

This tutorial demonstrates how to use `ISendProxyManager` to override a networked field's value **per client** as it is sent, without changing the real value on the server.

Register a field on an entity with `Hook`; the callback runs on the main thread once per recipient while that field is written, so each client can be shown a different value (or the real one — return `false`). Native pre-sets the value's `Kind` to what the field's encoder expects; set the matching value and return `true` to override. Supported kinds: int, float, bool, qangle, and string (e.g. a per-viewer player name). The example below shows each.

[!code-csharp[SendProxyExample.cs](../../codes/send-proxy.cs)]
