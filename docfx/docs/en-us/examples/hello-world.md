# Hello World

This example demonstrates how to write the simplest plugin.

First, create the following project:

[!code-csharp[HelloWorld.xml](../../codes/hello-world.xml)]

> [!WARNING]
>
> 1. `ModSharp.Sharp.Shared` must have the `PrivateAssets="all"` property because we need to prevent automatic copying of `Sharp.Shared.dll` and all its related dlls during Publish.  
> 2. `AssemblyName` is a required field, otherwise dependency loading issues may occur. This field should match the project name. Our project name is **Example**, so we enter `Example`. Adjust accordingly during actual development.

Write the module:

[!code-csharp[HelloWorld.cs](../../codes/hello-world.cs)]
