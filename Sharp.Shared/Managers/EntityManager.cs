/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
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
    ///     Add <see cref="IEntityListener" /> to listen for events
    /// </summary>
    void InstallEntityListener(IEntityListener listener);

    /// <summary>
    ///     Remove <see cref="IEntityListener" />
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
    ///         If you need to check <c>Controller</c> please call <see cref="IBaseEntity.AsPlayerController" /> yourself
    ///         <br />
    ///     </remarks>
    /// </summary>
    T MakeEntityFromPointer<T>(nint entity) where T : class, IBaseEntity;

    /// <summary>
    ///     Find entity by Index
    /// </summary>
    T? FindEntityByIndex<T>(EntityIndex index) where T : class, IBaseEntity;

    /// <summary>
    ///     Find entity by Classname.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>PERFORMANCE WARNING:</b><br />
    ///         JThis method performs an <b>O(N) linear scan</b> over the global
    ///         entity list
    ///         and uses a wildcard string comparison (<c>V_CompareNameWithWildcards</c>), which is way heavier than a normal
    ///         <c>strcmp</c>, for every entity.
    ///     </para>
    /// </remarks>
    /// <param name="start">Entity cursor, null to start from beginning.</param>
    /// <param name="classname">
    ///     Entity Classname. This is <b>case-insensitive</b> and supports wildcards:
    ///     <c>*</c> matches zero or more characters (e.g., <c>prop_*</c>), and <c>?</c> matches exactly one character.
    /// </param>
    /// <returns>The first entity matching the specified Classname, or <c>null</c> if no match is found.</returns>
    IBaseEntity? FindEntityByClassname(IBaseEntity? start, string classname);

    /// <summary>
    ///     Find entity by Targetname.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>PERFORMANCE WARNING:</b><br />
    ///         This method performs an <b>O(N) linear scan</b> over the global entity list and uses a wildcard string
    ///         comparison (<c>V_CompareNameWithWildcards</c>), which is way heavier than a normal <c>strcmp</c>, for every
    ///         entity.
    ///     </para>
    ///     <para>
    ///         For the best performance, you should not call this function in every server tick/frame (e.g. OnGameFrame,
    ///         EntityThink), and maintain your own entity list with targetname saved instead, assuming their name won't
    ///         change.
    ///     </para>
    /// </remarks>
    /// <param name="start">Entity cursor, null to start from beginning.</param>
    /// <param name="name">
    ///     Entity Targetname. This is <b>case-insensitive</b> and supports wildcards:
    ///     <c>*</c> matches zero or more characters (e.g., <c>door_*</c>), and <c>?</c> matches exactly one character (e.g.,
    ///     <c>door_?</c>).
    /// </param>
    /// <returns>The first entity matching the specified Targetname, or <c>null</c> if no match is found.</returns>
    IBaseEntity? FindEntityByName(IBaseEntity? start, string name);

    /// <summary>
    ///     Find entity by center coordinates
    /// </summary>
    /// <param name="start">Entity cursor, null to start from beginning</param>
    /// <param name="center">Center coordinates</param>
    /// <param name="radius">Radius</param>
    IBaseEntity? FindEntityInSphere(IBaseEntity? start, Vector center, float radius);

    /// <summary>
    ///     Create and spawn entity (full pipeline: create + precache + dispatch spawn).
    ///     Safe for all entity classes including weapons, but at the cost of performance
    ///     (most of the cost is in the precache step).
    ///     <br /><br />
    ///     <b>For high-performance scenarios</b> (e.g. spawning many entities per tick),
    ///     skip this method and call <see cref="CreateEntityByName" /> followed by
    ///     <see cref="IBaseEntity.DispatchSpawn" /> directly. That path bypasses precache
    ///     and is much faster, but <b>spawning weapons will crash</b>, and other entities may fail
    ///     if their required resources have not been loaded yet.
    ///     <br /><br />
    ///     No need to call <see cref="IBaseEntity.DispatchSpawn" /> after this method.
    /// </summary>
    IBaseEntity? SpawnEntitySync(string classname, IReadOnlyDictionary<string, KeyValuesVariantValueItem> keyValues);

    /// <summary>
    ///     Generic version of <see cref="SpawnEntitySync(string, IReadOnlyDictionary{string, KeyValuesVariantValueItem})" />.
    ///     See that overload for cost and the high-performance alternative.
    ///     <br /><br />
    ///     <b>&lt;T&gt; is not checked</b> — caller must ensure type correctness.
    ///     <br /><br />
    ///     No need to call <see cref="IBaseEntity.DispatchSpawn" /> after this method.
    /// </summary>
    T? SpawnEntitySync<T>(string classname, IReadOnlyDictionary<string, KeyValuesVariantValueItem> keyValues)
        where T : class, IBaseEntity;

    /// <summary>
    ///     Create an entity instance without spawning it. You must call
    ///     <see cref="IBaseEntity.DispatchSpawn" /> afterwards to actually spawn it.
    ///     <br /><br />
    ///     This bypasses the precache step, so it is much faster than
    ///     <see cref="SpawnEntitySync(string, IReadOnlyDictionary{string, KeyValuesVariantValueItem})" />,
    ///     but <b>spawning weapons will crash</b> and other entities may fail if their required
    ///     resources have not been loaded yet.
    /// </summary>
    IBaseEntity? CreateEntityByName(string classname);

    /// <summary>
    ///     Generic version of <see cref="CreateEntityByName(string)" />.
    ///     See that overload for the precache caveat.
    ///     <br /><br />
    ///     <b>&lt;T&gt; is not checked</b> — caller must ensure type correctness.
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
