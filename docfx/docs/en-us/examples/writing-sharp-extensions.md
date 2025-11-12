# Writing ModSharp Extensions

> [!CAUTION]
> This is a high-risk operation. You need to have a thorough understanding of what you are writing, otherwise it is easy to crash your server!

The questions are the same as those in [Module API](./module-api.md):

1. Do you need your components to be used across projects?
   - Yes: Extension/Shared
   - No: Ignore this directly

2. Do your components require shared memory (e.g., if Plugin A registers something, Plugin B cannot register the same)?
   - Yes: Module API
   - No: Extension

If you are absolutely sure there will be no issues, then you can write the extension package.

This tutorial will teach you how to write an extension package.

First, define the API:
[!code-csharp[SharpExtensionApi.cs](../../codes/sharp-extension-interface.cs)]

Then you just need to write it like this:
[!code-csharp[SharpExtension.cs](../../codes/sharp-extension-example.cs)]

Finally, you need to write the DI:
[!code-csharp[SharpExtensionDi.cs](../../codes/sharp-extension-di.cs)]