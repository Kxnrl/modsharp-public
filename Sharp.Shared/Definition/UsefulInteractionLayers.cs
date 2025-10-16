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

namespace Sharp.Shared.Definition;

public static class UsefulInteractionLayers
{
    /// <summary>
    ///     Copy from SwingOrStab in game code
    ///     <br />
    ///     <remarks>
    ///         <b>798721</b>
    ///     </remarks>
    /// </summary>
    public const InteractionLayers SwingOrStab = InteractionLayers.Solid
                                                 | InteractionLayers.Window
                                                 | InteractionLayers.PassBullets
                                                 | InteractionLayers.Player
                                                 | InteractionLayers.Npc;

    /// <summary>
    ///     Copy from FireBullets in game code
    ///     <br />
    ///     <remarks>
    ///         <b>1847307</b>
    ///     </remarks>
    /// </summary>
    public const InteractionLayers FireBullets = InteractionLayers.Solid
                                                 | InteractionLayers.HitBoxes
                                                 | InteractionLayers.Sky
                                                 | InteractionLayers.Window
                                                 | InteractionLayers.PassBullets
                                                 | InteractionLayers.Player
                                                 | InteractionLayers.Npc
                                                 | InteractionLayers.Debris;

    /// <summary>
    ///     Copy from ping in game code
    ///     <br />
    ///     <remarks>
    ///         <b>1839107</b>
    ///     </remarks>
    /// </summary>
    public const InteractionLayers PlayerPing = InteractionLayers.Solid
                                                | InteractionLayers.HitBoxes
                                                | InteractionLayers.Window
                                                | InteractionLayers.Player
                                                | InteractionLayers.Npc
                                                | InteractionLayers.Debris;

    /// <summary>
    ///     Copy from Brush trace in game code
    ///     <br />
    ///     <remarks>0x1 | 0x2 | 0x8 | 0x4000 | 0x10000 = <b>81931</b></remarks>
    /// </summary>
    public const InteractionLayers BrushOnly = InteractionLayers.Solid
                                               | InteractionLayers.HitBoxes
                                               | InteractionLayers.Sky
                                               | InteractionLayers.WorldGeometry
                                               | InteractionLayers.Slime;
}
