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

#include "bridge/natives/PlayerNatives.h"
#include "bridge/adapter.h"

#include "definitions.h"

#include "CoreCLR/Nullable.h"

#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/type/EmitSound.h"

// extern for movement manager
extern void  SetPlayerLaggedMovementValue(CCSPlayerController* pController, float flValue);
extern float GetPlayerLaggedMovementValue(CCSPlayerController* pController);

namespace natives::player
{
//////////////////////////////////////////////////////////////////////

static CBasePlayerPawn* ControllerGetPawn(CBasePlayerController* pController)
{
    return pController->GetPawn<CBasePlayerPawn*>();
}

static void ControllerPrint(CBasePlayerController* pController, HudPrint_t dest, const char* name, const char* param1, const char* param2, const char* param3, const char* param4)
{
    pController->ClientPrint(dest, name, param1, param2, param3, param4);
}

static void ControllerRoundRespawn(CBasePlayerController* pController)
{
    reinterpret_cast<CCSPlayerController*>(pController)->RoundRespawn();
}

static void ControllerSwitchTeam(CBasePlayerController* pController, CStrikeTeam_t team)
{
    pController->SwitchTeam(team);
}

static CBasePlayerController* ControllerFindBySlot(PlayerSlot_t slot)
{
    return CBasePlayerController::FindBySlot(slot);
}

static void ControllerSetClanTag(CBasePlayerController* pController, const char* tag)
{
    pController->SetClanTag(tag);
}

static CEconItemView* ControllerGetItemInLoadoutFromInventory(CCSPlayerController* pController, CStrikeTeam_t team, int32_t itemSlot)
{
    return pController->GetItemInLoadoutFromInventory(team, itemSlot);
}

static CBasePlayerController* ControllerFromEntity(CBaseEntity* pEntity)
{
    return pEntity->ToPlayerController();
}

static int64_t ControllerEmitSoundClient(CBasePlayerController* pController, const char* sound, Nullable<float>* volume)
{
    const auto pVolume = volume->HasValue() ? &volume->GetValue() : nullptr;
    return pController->EmitSoundClient(sound, pVolume);
}

static void ControllerCheckPawn(CCSPlayerController* pController)
{
    pController->CheckPawn();
}

static void ControllerSetPawn(CCSPlayerController* pController, CCSPlayerPawn* pPawn, bool unk1, bool unk2, bool unk3, bool unk4)
{
    pController->SetPawn(pPawn, unk1, unk2, unk3, unk4);
}

static void ControllerSetLaggedMovement(CCSPlayerController* pController, float flValue)
{
    SetPlayerLaggedMovementValue(pController, flValue);
}

static float ControllerGetLaggedMovement(CCSPlayerController* pController)
{
    return GetPlayerLaggedMovementValue(pController);
}

//////////////////////////////////////////////////////////////////////

static CBasePlayerController* PawnGetController(CBasePlayerPawn* pPawn)
{
    return pPawn->GetController<CBasePlayerController*>();
}

static void PawnPrint(CBasePlayerPawn* pPawn, HudPrint_t dest, const char* name, const char* param1, const char* param2, const char* param3, const char* param4)
{
    pPawn->ClientPrint(dest, name, param1, param2, param3, param4);
}

static void PawnSlay(CCSPlayerPawn* pPawn, bool explode)
{
    pPawn->CommitSuicide(explode);
}

static CBaseWeapon* PawnGiveNamedItem(CCSPlayerPawn* pPawn, const char* name)
{
    return pPawn->GiveNamedItem(name);
}

static CBasePlayerPawn* PawnFindBySlot(PlayerSlot_t slot)
{
    return CBasePlayerPawn::FindBySlot(slot);
}

static CBaseWeapon* PawnGetActiveWeapon(CCSPlayerPawn* pPawn)
{
    return pPawn->GetActiveWeapon();
}

static CBaseWeapon* PawnGetWeaponBySlot(CCSPlayerPawn* pPawn, GearSlot_t slot, int position)
{
    return pPawn->GetWeaponBySlot(slot, position);
}

static void PawnRemovePlayerItem(CCSPlayerPawn* pPawn, CBaseWeapon* pItem)
{
    pPawn->RemovePlayerItem(pItem);
}

static void PawnRemoveAllItems(CCSPlayerPawn* pPawn, bool removeSuit)
{
    pPawn->RemoveAllItems(removeSuit);
}

static void PawnDropWeapon(CCSPlayerPawn* pPawn, CBaseWeapon* pWeapon)
{
    pPawn->DropWeapon(pWeapon);
}

static bool PawnSelectItem(CCSPlayerPawn* pPawn, CBaseWeapon* pWeapon)
{
    return pPawn->SelectItem(pWeapon);
}

static bool PawnDetachWeapon(CCSPlayerPawn* pPawn, CBaseWeapon* pWeapon)
{
    return pPawn->DetachWeapon(pWeapon);
}

static void PawnSwitchWeapon(CCSPlayerPawn* pPawn, CBaseWeapon* pWeapon)
{
    return pPawn->SwitchWeapon(pWeapon);
}

static void PawnGiveGloves(CCSPlayerPawn* pPawn, int itemDefIndex, int prefab, float wear, int seed)
{
    pPawn->GiveGloves(itemDefIndex, prefab, wear, seed);
}

static CBasePlayerPawn* PawnFromEntity(CBaseEntity* pEntity)
{
    return pEntity->ToPlayerPawn();
}

static int64_t PawnEmitSoundClient(CBasePlayerPawn* pPawn, const char* sound, Nullable<float>* volume)
{
    const auto pVolume = volume->HasValue() ? &volume->GetValue() : nullptr;
    return pPawn->EmitSoundClient(sound, pVolume);
}
    
static bool PawnIsPlayer(CCSPlayerPawnBase* pPawn)
{
    return pPawn->IsPlayer();
}

//////////////////////////////////////////////////////////////////////

void Init()
{
    // Controller
    bridge::CreateNative("Player.ControllerGetPawn", reinterpret_cast<void*>(ControllerGetPawn));
    bridge::CreateNative("Player.ControllerFromEntity", reinterpret_cast<void*>(ControllerFromEntity));
    bridge::CreateNative("Player.ControllerPrint", reinterpret_cast<void*>(ControllerPrint));
    bridge::CreateNative("Player.ControllerRoundRespawn", reinterpret_cast<void*>(ControllerRoundRespawn));
    bridge::CreateNative("Player.ControllerSwitchTeam", reinterpret_cast<void*>(ControllerSwitchTeam));
    bridge::CreateNative("Player.ControllerFindBySlot", reinterpret_cast<void*>(ControllerFindBySlot));
    bridge::CreateNative("Player.ControllerSetClanTag", reinterpret_cast<void*>(ControllerSetClanTag));
    bridge::CreateNative("Player.ControllerEmitSoundClient", reinterpret_cast<void*>(ControllerEmitSoundClient));
    bridge::CreateNative("Player.ControllerGetItemInLoadoutFromInventory", reinterpret_cast<void*>(ControllerGetItemInLoadoutFromInventory));
    bridge::CreateNative("Player.ControllerCheckPawn", reinterpret_cast<void*>(ControllerCheckPawn));
    bridge::CreateNative("Player.ControllerSetPawn", reinterpret_cast<void*>(ControllerSetPawn));
    bridge::CreateNative("Player.ControllerGetLaggedMovement", reinterpret_cast<void*>(ControllerGetLaggedMovement));
    bridge::CreateNative("Player.ControllerSetLaggedMovement", reinterpret_cast<void*>(ControllerSetLaggedMovement));

    // Pawn
    bridge::CreateNative("Player.PawnGetController", reinterpret_cast<void*>(PawnGetController));
    bridge::CreateNative("Player.PawnFromEntity", reinterpret_cast<void*>(PawnFromEntity));
    bridge::CreateNative("Player.PawnPrint", reinterpret_cast<void*>(PawnPrint));
    bridge::CreateNative("Player.PawnSlay", reinterpret_cast<void*>(PawnSlay));
    bridge::CreateNative("Player.PawnGiveNamedItem", reinterpret_cast<void*>(PawnGiveNamedItem));
    bridge::CreateNative("Player.PawnFindBySlot", reinterpret_cast<void*>(PawnFindBySlot));
    bridge::CreateNative("Player.PawnGetActiveWeapon", reinterpret_cast<void*>(PawnGetActiveWeapon));
    bridge::CreateNative("Player.PawnGetWeaponBySlot", reinterpret_cast<void*>(PawnGetWeaponBySlot));
    bridge::CreateNative("Player.PawnRemovePlayerItem", reinterpret_cast<void*>(PawnRemovePlayerItem));
    bridge::CreateNative("Player.PawnRemoveAllItems", reinterpret_cast<void*>(PawnRemoveAllItems));
    bridge::CreateNative("Player.PawnDropWeapon", reinterpret_cast<void*>(PawnDropWeapon));
    bridge::CreateNative("Player.PawnSelectItem", reinterpret_cast<void*>(PawnSelectItem));
    bridge::CreateNative("Player.PawnDetachWeapon", reinterpret_cast<void*>(PawnDetachWeapon));
    bridge::CreateNative("Player.PawnSwitchWeapon", reinterpret_cast<void*>(PawnSwitchWeapon));
    bridge::CreateNative("Player.PawnGiveGloves", reinterpret_cast<void*>(PawnGiveGloves));
    bridge::CreateNative("Player.PawnEmitSoundClient", reinterpret_cast<void*>(PawnEmitSoundClient));
    bridge::CreateNative("Player.PawnIsPlayer", reinterpret_cast<void*>(PawnIsPlayer));
}
} // namespace natives::player