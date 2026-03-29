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
    ///     Add <see cref="IEntityListener"/> to listen for events
    /// </summary>
    void InstallEntityListener(IEntityListener listener);

    /// <summary>
    ///     Remove <see cref="IEntityListener"/>
    /// </summary>
    void RemoveEntityListener(IEntityListener listener);

    /// <summary>
    ///     Find entity by EHandle
    /// </summary>
    T? FindEntityByHandle<T>(CEntityHandle<T> eHandle) where T : class, IBaseEntity;

    /// <summary>
    ///     Find entity by Index
    /// </summary>
    IBaseEntity? FindEntityByIndex(EntityIndex index);

    /// <summary>
    ///     Build entity from pointer <br />
    ///     <remarks>
    ///         Does not guarantee correct type, type is based on input parameter <br />
    ///         If you need to check <c>Pawn</c> please call <see cref="IBaseEntity.AsPlayerPawn" /> yourself <br />
    ///         If you need to check <c>Controller</c> please call <see cref="IBaseEntity.AsPlayerController" /> yourself <br />
    ///     </remarks>
    /// </summary>
    T MakeEntityFromPointer<T>(nint entity) where T : class, IBaseEntity;

    /// <summary>
    ///     Find entity by Index
    /// </summary>
    T? FindEntityByIndex<T>(EntityIndex index) where T : class, IBaseEntity;

    /// <summary>
    ///     Find entity by Classname
    /// </summary>
    /// <param name="start">Entity cursor, null to start from beginning</param>
    /// <param name="classname">Entity Classname</param>
    IBaseEntity? FindEntityByClassname(IBaseEntity? start, string classname);

    /// <summary>
    ///     Find entity by Targetname
    /// </summary>
    /// <param name="start">Entity cursor, null to start from beginning</param>
    /// <param name="name">Entity Targetname</param>
    IBaseEntity? FindEntityByName(IBaseEntity? start, string name);

    /// <summary>
    ///     Find entity by center coordinates
    /// </summary>
    /// <param name="start">Entity cursor, null to start from beginning</param>
    /// <param name="center">Center coordinates</param>
    /// <param name="radius">Radius</param>
    IBaseEntity? FindEntityInSphere(IBaseEntity? start, Vector center, float radius);

    /// <summary>
    ///     Create and spawn entity <br />
    ///     <remarks>
    ///         No need to call <see cref="IBaseEntity.DispatchSpawn" />
    ///     </remarks>
    /// </summary>
    IBaseEntity? SpawnEntitySync(string classname, IReadOnlyDictionary<string, KeyValuesVariantValueItem> keyValues);

    /// <summary>
    ///     Create and spawn entity <br />
    ///     <remarks>
    ///         No need to call <see cref="IBaseEntity.DispatchSpawn" /> <br />
    ///         <b>&lt;T&gt;</b> is not checked, caller must ensure type correctness
    ///     </remarks>
    /// </summary>
    T? SpawnEntitySync<T>(string classname, IReadOnlyDictionary<string, KeyValuesVariantValueItem> keyValues)
        where T : class, IBaseEntity;

    /// <summary>
    ///     Create entity <br />
    ///     <remarks>
    ///         Cannot create weapon-related entities, otherwise it will crash
    ///     </remarks>
    /// </summary>
    IBaseEntity? CreateEntityByName(string classname);

    /// <summary>
    ///     Create entity <br />
    ///     <remarks>
    ///         Cannot create weapon-related entities, otherwise it will crash <br />
    ///         <b>&lt;T&gt;</b> is not checked, caller must ensure type correctness
    ///     </remarks>
    /// </summary>
    T? CreateEntityByName<T>(string classname) where T : class, IBaseEntity;

    /// <summary>
    ///     Create persistent CString in game
    /// </summary>
    CUtlSymbolLarge AllocPooledString(string content);

    /// <summary>
    ///     Listen for entity Output
    /// </summary>
    void HookEntityOutput(string classname, string output);

    /// <summary>
    ///     Listen for entity Input
    /// </summary>
    void HookEntityInput(string classname, string input);

    /*  Player  */

    /// <summary>
    ///     Find PlayerPawn by PlayerSlot
    /// </summary>
    IBasePlayerPawn? FindPlayerPawnBySlot(PlayerSlot slot);

    /// <summary>
    ///     Find PlayerController by PlayerSlot
    /// </summary>
    IPlayerController? FindPlayerControllerBySlot(PlayerSlot slot);

    /// <summary>
    ///     Find all existing PlayerControllers
    /// </summary>
    /// <param name="inGame">Whether in game</param>
    IEnumerable<IPlayerController> GetPlayerControllers(bool inGame = true);

    /// <summary>
    ///     List all existing PlayerControllers
    /// </summary>
    /// <param name="inGame">Whether in game</param>
    List<IPlayerController> FindPlayerControllers(bool inGame = true);

    /// <summary>
    ///     Update Econ entity attributes
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
    ///     Get CCSTeam
    /// </summary>
    IBaseTeam? GetGlobalCStrikeTeam(CStrikeTeam team);
}
