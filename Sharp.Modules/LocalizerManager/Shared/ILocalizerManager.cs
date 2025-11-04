using System.Diagnostics.CodeAnalysis;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager.Shared;

public interface ILocalizerManager
{
    const string Identity = nameof(ILocalizerManager);

    /// <summary>
    ///     加载/重载翻译文件 <br />
    ///     <remarks>
    ///         当多个翻译文件包含相同的键值时 <br />
    ///         后加载的文件会覆盖先加载文件的数据 <br />
    ///     </remarks>
    /// </summary>
    /// <param name="name">位于{sharp}/locales的json文件<br />文件名不包含.json</param>
    /// <param name="suppressDuplicationWarnings">当包含重复键值时是否警告</param>
    void LoadLocaleFile(string name, bool suppressDuplicationWarnings = false);

    /// <summary>
    ///     获取客户端的本地化器 <br />
    ///     <remarks>当客户端本地化尚未就绪时返回默认本地化器</remarks>
    /// </summary>
    ILocalizer GetLocalizer(IGameClient client);

    /// <summary>
    ///     获取客户端的本地化器 <br />
    ///     <remarks>当客户端本地化尚未就绪时返回默认本地化器</remarks>
    /// </summary>
    ILocalizer this[IGameClient client] { get; }

    /// <summary>
    ///     尝试获取客户端的本地化器
    /// </summary>
    bool TryGetLocalizer(IGameClient client, [NotNullWhen(true)] out ILocalizer? localizer);
}
