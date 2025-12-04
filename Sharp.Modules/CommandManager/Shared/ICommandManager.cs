using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using DelegateClientCommand = Sharp.Shared.Managers.IClientManager.DelegateClientCommand;

namespace Sharp.Modules.CommandManager.Shared;

public interface ICommandManager
{
    const string Identity = nameof(ICommandManager);

    /// <summary>
    /// Add Command registry. <br/>
    /// </summary>
    /// <param name="moduleIdentity"></param>
    public ICommandRegistry GetRegistry(string moduleIdentity);

}

public interface ICommandRegistry
{
    /// <summary>
    ///     注册用户指令。<br />
    ///     该指令可被如下途径调用：<br />
    ///     1. 聊天栏中输入 .{该指令内容}，例：.ztele。<br />
    ///     (注：你可以将.换成!或/，它们都是一样的。)<br />
    ///     2. 客户端控制台，你必须添加`ms_`前缀，例：ms_ztele。<br />
    ///     此函数注册的指令无法被服务端控制台调用。
    /// </summary>
    /// <param name="command">指令，你可以不添加ms_前缀，注册的时候会给你自动加上。你也可以加上，这个不影响。但是如果你是ms_ms_这种，那我们爱莫能助。</param>
    /// <param name="call"></param>
    void RegisterClientCommand(string command, Action<IGameClient, StringCommand> call);

    /// <summary>
    ///     创建一个控制台指令。<br />
    ///     该指令只会在服务端控制台生效。
    /// </summary>
    /// <param name="description"></param>
    /// <param name="addPrefix">是否要添加ms_前缀？默认添加。</param>
    /// <param name="command"></param>
    /// <param name="call"></param>
    void RegisterServerCommand(string command, Action<StringCommand> call, string description = "", bool addPrefix = true);

    /// <summary>
    ///     创建一个控制台指令。<br />
    ///     该指令只会在服务端控制台生效。
    /// </summary>
    /// <param name="description"></param>
    /// <param name="addPrefix">是否要添加ms_前缀？默认添加。</param>
    /// <param name="command"></param>
    /// <param name="call"></param>
    void RegisterServerCommand(string command, Action call, string description = "", bool addPrefix = true);

    /// <summary>
    /// 注册一个「通用」指令：客户端聊天栏，客户端控制台，服务端控制台均可使用。<br />
    /// 一定会添加ms_标签，请注意
    /// </summary>
    /// <param name="command"></param>
    /// <param name="call"></param>
    /// <param name="description"></param>
    void RegisterGenericCommand(string command, Action<IGameClient?, StringCommand> call, string description = "");

    /// <summary>
    ///     创建一个控制台指令。 <br />
    ///     该指令只会在客户端控制台和服务端控制台生效。<br />
    /// </summary>
    /// <param name="command"></param>
    /// <param name="callback"></param>
    /// <param name="addPrefix">是否添加ms_前缀？默认添加。</param>
    void RegisterConsoleCommand(string command, Action<IGameClient?, StringCommand> callback, bool addPrefix = true);

    /// <summary>
    ///     监听指令。<br />
    ///     一般来说，该函数只用于监听客户端控制台内输入的指令，如player_ping。<br />
    ///     可自行参阅函数调用。
    /// </summary>
    /// <param name="commandName"></param>
    /// <param name="callback"></param>
    void AddCommandListener(string commandName, DelegateClientCommand callback);

}