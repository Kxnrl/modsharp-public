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

using Sharp.Shared.Enums;
using Sharp.Shared.Units;

namespace Sharp.Shared.HookParams;

public interface ITextMsgHookParams : IFunctionParams
{
    /// <summary>
    /// The specific HUD channel where this message will be displayed 
    /// (e.g., Chat, Notify, Center, Console).
    /// </summary>
    HudPrintChannel Channel   { get; }
    
    /// <summary>
    /// The content of the message. 
    /// <para>
    /// Note: This is usually a localization token (e.g., "#Cstrike_TitlesTXT_Cannot_Carry_Anymore")
    /// rather than the raw translated text.
    /// </para>
    /// </summary>
    string          Name      { get; }
    
    /// <summary>
    /// The target audience for this message (e.g., specific client, broadcast, or team).
    /// </summary>
    NetworkReceiver Receivers { get; }
    
    /// <summary>
    /// Utility method to check if a specific player slot is among the recipients of this message.
    /// </summary>
    /// <param name="slot">The player slot to check.</param>
    /// <returns>True if the player at this slot will receive the message; otherwise, false.</returns>
    bool HasClient(PlayerSlot slot);
}
