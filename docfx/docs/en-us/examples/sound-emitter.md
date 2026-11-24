# Sound Emitter

Use the [Hello World project settings](hello-world.md).

Run `.hello Player.DeathHeadShot` in chat or `ms_hello Player.DeathHeadShot` in the client console to trigger the audio demo; without an argument it uses that same built-in sound event. Check `IsSoundEventValid` before playing custom event names; the required sound resources must exist and be available on the server/client.

The example shows sound duration, client-only and entity/team/global emission, sound parameter updates and stopping a sound by its returned `SoundOpEventGuid`. The sends are alternatives that are shown together here, so the demo deliberately plays several copies of the sound. Delayed parameter/stop operations capture only the sound GUID and manager, not a player entity. The module's timers are removed automatically on owner unload, and its command callback is explicitly removed in `Shutdown`.

[!code-csharp[sound-emitter.cs](../../codes/sound-emitter.cs)]
