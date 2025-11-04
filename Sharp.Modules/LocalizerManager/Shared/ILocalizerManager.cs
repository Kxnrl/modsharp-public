using System.Diagnostics.CodeAnalysis;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager.Shared;

public interface ILocalizerManager
{
    const string Identity = nameof(ILocalizerManager);

    /// <summary>
    ///     加载翻译文件
    /// </summary>
    /// <param name="name">位于{sharp}/locales的json文件<br />文件名不包含.json</param>
    void LoadLocaleFile(string name);

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
