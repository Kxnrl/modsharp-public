# Writing ModSharp Extensions

> [!WARNING]
> This is a high-risk operation. You need to have sufficient understanding of what you are writing!

The questions are the same as those in [Module API](./module-api.md):

1. Do you need to share code?
   - ✔️: Extension / Module Shared API
   - ❌: No need to worry

2. Do you need data interaction?
   - ✔️: Module Shared API
   - ❌: Extension

If you are absolutely sure there will be no issues, then you can write the extension package.

This tutorial will teach you how to write an extension package.

First, define the API:
[!code-csharp[SharpExtensionApi.cs](../../codes/sharp-extension-interface.cs)]

Then you just need to write it like this:
[!code-csharp[SharpExtension.cs](../../codes/sharp-extension-example.cs)]

Finally, you need to write the DI:
[!code-csharp[SharpExtensionDi.cs](../../codes/sharp-extension-di.cs)]