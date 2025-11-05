/*
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;

namespace Sharp.Shared.Managers;

public interface IEntityManager
{
    /// <summary>
    ///     添加EntityListener以监听事件
    /// </summary>
    void InstallEntityListener(IEntityListener listener);

    /// <summary>
    ///     移除EntityListener
    /// </summary>
    void RemoveEntityListener(IEntityListener listener);

    /// <summary>
    ///     通过EHandle查找实体
    /// </summary>
    T? FindEntityByHandle<T>(CEntityHandle<T> eHandle) where T : class, IBaseEntity;

    /// <summary>
    ///     通过Index查找实体
    /// </summary>
    IBaseEntity? FindEntityByIndex(EntityIndex index);

    /// <summary>
    ///     通过指针构建实体 <br />
    ///     <remarks>
    ///         不确保正确类型, 类型以入参为准 <br />
    ///         如若需要判断<c>Pawn</c>请自行调用<see cref="IBaseEntity.AsPlayerPawn" /> <br />
    ///         如若需要判断<c>Controller</c>请自行调用<see cref="IBaseEntity.AsPlayerController" /> <br />
    ///     </remarks>
    /// </summary>
    T MakeEntityFromPointer<T>(nint entity) where T : class, IBaseEntity;

    /// <summary>
    ///     通过Index查找实体
    /// </summary>
    T? FindEntityByIndex<T>(EntityIndex index) where T : class, IBaseEntity;

    /// <summary>
    ///     通过Classname查找实体
    /// </summary>
    /// <param name="start">实体游标, null则为从头开始</param>
    /// <param name="classname">实体Classname</param>
    IBaseEntity? FindEntityByClassname(IBaseEntity? start, string classname);

    /// <summary>
    ///     通过Targetname查找实体
    /// </summary>
    /// <param name="start">实体游标, null则为从头开始</param>
    /// <param name="name">实体Targetname</param>
    IBaseEntity? FindEntityByName(IBaseEntity? start, string name);

    /// <summary>
    ///     通过中心坐标查找实体
    /// </summary>
    /// <param name="start">实体游标, null则为从头开始</param>
    /// <param name="center">中心坐标</param>
    /// <param name="radius">半径</param>
    IBaseEntity? FindEntityInSphere(IBaseEntity? start, Vector center, float radius);

    /// <summary>
    ///     创建并生成实体 <br />
    ///     <remarks>
    ///         实体的kv中的key必须要小写, 否则会不认
    ///     </remarks>
    /// </summary>
    IBaseEntity? SpawnEntitySync(string classname, IReadOnlyDictionary<string, KeyValuesVariantValueItem> keyValues);

    /// <summary>
    ///     创建并生成实体 <br />
    ///     <remarks>
    ///         实体的kv中的key必须要小写, 否则会不认 <br />
    ///         T并不会进行检查, 由调用者自己保证类型
    ///     </remarks>
    /// </summary>
    T? SpawnEntitySync<T>(string classname, IReadOnlyDictionary<string, KeyValuesVariantValueItem> keyValues)
        where T : class, IBaseEntity;

    /// <summary>
    ///     创建实体 <br />
    ///     <remarks>
    ///         不要创建武器相关的任何实体，否则之后的所有相关操作都会把服炸了。
    ///     </remarks>
    /// </summary>
    IBaseEntity? CreateEntityByName(string classname);

    /// <summary>
    ///     创建实体 <br />
    ///     <remarks>
    ///         不要创建武器相关的任何实体，否则之后的所有相关操作都会把服炸了。
    ///     </remarks>
    /// </summary>
    T? CreateEntityByName<T>(string classname) where T : class, IBaseEntity;

    /// <summary>
    ///     创建游戏内持久化CString
    /// </summary>
    CUtlSymbolLarge AllocPooledString(string content);

    /// <summary>
    ///     监听实体Output
    /// </summary>
    void HookEntityOutput(string classname, string output);

    /// <summary>
    ///     监听实体Input
    /// </summary>
    void HookEntityInput(string classname, string input);

    /*  Player  */

    /// <summary>
    ///     通过PlayerSlot查找PlayerPawn
    /// </summary>
    IPlayerPawn? FindPlayerPawnBySlot(PlayerSlot slot);

    /// <summary>
    ///     通过PlayerSlot查找PlayerController
    /// </summary>
    IPlayerController? FindPlayerControllerBySlot(PlayerSlot slot);

    /// <summary>
    ///     更新Econ实体的属性
    /// </summary>
    bool UpdateEconItemAttributes(IBaseEntity entity,
        uint                                  accountId,
        string                                nameTag,
        int                                   paint,
        int                                   pattern,
        float                                 wear,
        int                                   nSticker1,
        float                                 flSticker1,
        int                                   nSticker2,
        float                                 flSticker2,
        int                                   nSticker3,
        float                                 flSticker3,
        int                                   nSticker4,
        float                                 flSticker4);

    /// <summary>
    ///     获取CCSTeam
    /// </summary>
    IBaseTeam? GetGlobalCStrikeTeam(CStrikeTeam team);
}
