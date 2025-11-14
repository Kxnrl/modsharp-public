# Module Shared API

This tutorial will teach you how to write an API that can be exported for use by other modules.

> [!NOTE]
> This project example is named `SharedInterface`.

First, you need at least 2 projects to handle this task. For demonstration purposes in this tutorial, they are called `SharedInterface.Shared` and `SharedInterface` respectively.

Before writing the API, consider the following questions:

1. Do you need to share code?
   - ✔️: Extension / Module Shared API
   - ❌: No need to worry

2. Do you need data interaction?
   - ✔️: Module Shared API
   - ❌: Extension

This article only teaches how to write a Module API.

In `SharedInterface.Shared`, make the following definitions:

[!code-csharp[SharedInterface.Shared.cs](../../codes/module-api.cs)]

Then, write the following implementation in `SharedInterface`:
[!code-csharp[SharedInterface.cs](../../codes/module-impl.cs)]

Finally, call the API you have written in other plugins:
[!code-csharp[UseSharedModule.cs](../../codes/module-use-api.cs)]
