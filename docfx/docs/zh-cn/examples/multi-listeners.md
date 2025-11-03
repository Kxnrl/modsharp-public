# Multi Listeners

本教程将演示如何在一个Module中同时实现多个Listener。

> [!TIP]
>
> 因为多个Listener中均包含``ListenerVersion``和``ListenerPriority``，  
> 因此我们显式实现接口，即可区分每个Listener的优先级和版本

[!code-csharp[MultiListenerExample.cs](../../codes/multi-listeners.cs)]
