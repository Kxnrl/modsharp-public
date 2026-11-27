# Sound Hooks

Use the [Hello World project settings](hello-world.md).

Install matching pre/post callbacks for `EmitSound` and `SoundEvent`; not every sound goes through `EmitSound` (sounds emitted via `EmitSound` do not also fire `SoundEvent`, and map music routes to `EmitMusic` instead of `SoundEvent`). Remove every installed callback in `Shutdown`.

The sample blocks the victim headshot sound, replaces the onlooker sound and changes its receiver mask, and then demonstrates post-emission parameter modification and logging. If an earlier hook already returned `SkipCallReturnOverride`, return `default` without modifying parameters. For both `EmitSound` and `SoundEvent`, returning `SkipCallReturnOverride` from a pre-hook skips the engine call and that hook's post callbacks (the `SkipCallReturnOverride` checks inside the post callbacks below are purely defensive). Return `ChangeParamReturnDefault` when modifying sound arguments, or `Ignored` when leaving them alone.

[!code-csharp[sound-hook.cs](../../codes/sound-hook.cs)]
