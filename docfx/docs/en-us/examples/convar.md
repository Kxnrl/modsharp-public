# ConVar

Use the [Hello World project settings](hello-world.md).

Choose a `CreateConVar` overload matching the intended value type and check its nullable result. This example creates an integer ConVar `example_value`, writes it with `Set(int)`, reads it with `GetInt32`, and reads `sv_cheats` without enabling cheats on the server.

Use typed setters for typed values. `Set(string)` parses text strictly for any ConVar type (throwing `ArgumentException` on invalid input); `SetString` uses permissive fallback conversion by type (and truncates string ConVars at 255 UTF-8 bytes), so do not assume that malformed input restores the variable's configured default.

[!code-csharp[convar.cs](../../codes/convar.cs)]
