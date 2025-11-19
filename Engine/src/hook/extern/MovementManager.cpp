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

#include "bridge/forwards/forward.h"
#include "global.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"
#include "memory/memory_access.h"
#include "module.h"
#include "vhook/hook.h"

#include "cstrike/component/PlayerPawnComponent.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/type/CGlobalVars.h"
#include "cstrike/type/CMoveData.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/VProf.h"

#include <proto/cs_usercmd.pb.h>
#include <proto/networkbasetypes.pb.h>
#include <proto/usercmd.pb.h>

#include <Zydis.h>
#include <safetyhook.hpp>

// #define LOG_FIX_TICKCOUNT

constexpr float CStrikeMaxSpeed   = 260.f;
constexpr float DefaultSpeedValue = 1.f;

volatile float g_flReplaceMaxSpeed;
volatile bool  g_bInWalkMove;

static float m_flLaggedMovementValue[CS_MAX_PLAYERS];
static float m_flRunSpeedValue[CS_MAX_PLAYERS];

static CConVarBaseData* ms_disable_usercmd_subtick_moves = nullptr;
static CConVarBaseData* ms_disable_usercmd_subtick_input = nullptr;
static CConVarBaseData* ms_fix_usercmd_rapid_fire        = nullptr;

static inline bool FloatCompare(float a, float b)
{
    return fabs(a - b) < 0.001f;
}

struct CUserCmdInButtonState
{
private:
    [[maybe_unused]] int8_t pad0[0x8];

public:
    uint64_t m_ButtonPressed;
    uint64_t m_ButtonChanged;
    uint64_t m_ButtonScroll;
};

class CUserCmd
{
private:
    [[maybe_unused]] int8_t pad0[0x10];

public:
    CSGOUserCmdPB         Command;
    CUserCmdInButtonState Button;

private:
    [[maybe_unused]] int8_t pad1[0x18];
#ifdef PLATFORM_WINDOWS
    [[maybe_unused]] int8_t pad2[0x8];
#endif
};
static_assert(sizeof(CSGOUserCmdPB) == 72);
#ifdef PLATFORM_WINDOWS
static_assert(sizeof(CUserCmd) == 152);
#else
static_assert(sizeof(CUserCmd) == 144);
#endif

#define ValidateCCP()                                                      \
    const auto pPawn = pService->GetPawn<CCSPlayerPawn*>();                \
    AssertPtr(pPawn);                                                      \
    const auto pController = pPawn->GetController<CCSPlayerController*>(); \
    AssertPtr(pController);                                                \
    const auto pClient = sv->GetClient(pController->GetPlayerSlot());      \
    AssertPtr(pClient)

#define ValidateCP()                                                       \
    const auto pPawn = pService->GetPawn<CCSPlayerPawn*>();                \
    AssertPtr(pPawn);                                                      \
    const auto pController = pPawn->GetController<CCSPlayerController*>(); \
    AssertPtr(pController)

BeginMemberHookScope(CPlayer_MovementServices)
{
    DeclareMemberDetourHook(RunCommand, void, (CPlayer_MovementServices * pService, CUserCmd * pCommand))
    {
        if (!pCommand)
            return RunCommand(pService, pCommand);

        VPROF_MS_HOOK();

        const auto pPawn = pService->GetPawn<CBasePlayerPawn*>();
        AssertPtr(pPawn);
        const auto pController = pPawn->GetController<CCSPlayerController*>();
        AssertPtr(pController);
        auto pClient = sv->GetClient(pController->GetPlayerSlot());
        AssertPtr(pClient);

        const auto action = forwards::OnPlayerRunCommandPre->Invoke(pClient, pController, pPawn, pService, pCommand);
        if (action == EHookAction::SkipCallReturnOverride)
        {
            forwards::OnPlayerRunCommandPost->Invoke(pClient, pController, pPawn, pService, pCommand, action);
            return;
        }

        if (action != EHookAction::Ignored)
        {
            FatalError("Not Impl Yet");
        }

        RunCommand(pService, pCommand);

        forwards::OnPlayerRunCommandPost->Invoke(pClient, pController, pPawn, pService, pCommand, action);
    }
}

BeginMemberHookScope(CCSPlayer_MovementServices)
{
    DeclareMemberDetourHook(ProcessMove, void, (CCSPlayer_MovementServices * pService, CMoveData * pMoveData))
    {
        VPROF_MS_HOOK();
        AssertPtr(gpGlobals);

        ValidateCCP();

        const auto nSlot = pClient->GetSlot();

        if (pClient)
        {
            forwards::OnPlayerProcessMovePre->Invoke(pClient, pController, pPawn, pService, pMoveData);
        }

        if (nSlot >= CS_MAX_PLAYERS || FloatCompare(m_flLaggedMovementValue[nSlot], DefaultSpeedValue) || m_flLaggedMovementValue[nSlot] < 0)
        {
            ProcessMove(pService, pMoveData);
        }
        else
        {
            const auto flStoredFrameTime = gpGlobals->flFrameTime;
            gpGlobals->flFrameTime *= m_flLaggedMovementValue[nSlot];
            ProcessMove(pService, pMoveData);
            gpGlobals->flFrameTime = flStoredFrameTime;
        }

        if (pClient)
        {
            forwards::OnPlayerProcessMovePost->Invoke(pClient, pController, pPawn, pService, pMoveData);
        }
    }

    // 因为MovementLocker, 所以需要魔法替换MV本身的最大速度
    // RunCommand
    //   -> WalkMove
    //      -> Accelerate
    //   -> WalkOther
    // EndContext

    DeclareMemberDetourHook(WalkMove, void, (CCSPlayer_MovementServices * pService, CMoveData * pMoveData))
    {
        ValidateCP();

        const auto pClient = sv->GetClient(pController->GetPlayerSlot());

        if (!pClient)
            return WalkMove(pService, pMoveData);

        const auto flMaxSpeed = pMoveData->m_flMaxSpeed;
        const auto nMaxSpeed  = static_cast<int>(flMaxSpeed);
        auto       nSpeed     = nMaxSpeed;
        forwards::OnPlayerWalkMove->Invoke(pClient, pController, pPawn, pService, pMoveData, &nSpeed);

        if (nSpeed == nMaxSpeed)
            return WalkMove(pService, pMoveData);

        g_flReplaceMaxSpeed = static_cast<float>(nSpeed);

        g_bInWalkMove = true;
        WalkMove(pService, pMoveData);
        g_bInWalkMove = false;

        pMoveData->m_flMaxSpeed = flMaxSpeed;
        g_flReplaceMaxSpeed     = CStrikeMaxSpeed;
    }

    DeclareMemberDetourHook(Accelerate, void, (CCSPlayer_MovementServices * pService, CMoveData * pMoveData, float a3, float* a4, float a5, float a6))
    {
        Accelerate(pService, pMoveData, a3, a4, a5, a6);

        if (g_bInWalkMove)
        {
            pMoveData->m_flMaxSpeed = g_flReplaceMaxSpeed;
        }
    }

    // Fix mega push
    DeclareVirtualHook(CheckMovingGround, void, (CCSPlayer_MovementServices * pService, double frametime))
    {
        ValidateCP();

        const auto slot = pController->GetPlayerSlot();

        static int32_t nPlayerTicks[CS_MAX_PLAYERS];

        if (nPlayerTicks[slot] == gpGlobals->nTickCount)
            return;

        nPlayerTicks[slot] = gpGlobals->nTickCount;

        CheckMovingGround(pService, frametime);
    }
}

BeginMemberHookScope(CCSPlayerController)
{
    DeclareMemberDetourHook(ProcessUserCommands, void, (CCSPlayerController * pController, CUserCmd * pCommands, int commandCount, bool paused, float margin))
    {
        VPROF_MS_HOOK();

        if (commandCount > 0 && ms_fix_usercmd_rapid_fire->GetValue<bool>())
        {
            if (const auto pPawn = pController->GetPlayerPawn(); pPawn && pPawn->IsAlive())
            {
                const auto nTickCount = gpGlobals->nTickCount;
                const auto nTickMinFx = nTickCount - 4;

                for (auto i = 0; i < commandCount; i++)
                {
                    const auto pUserCmd = &pCommands[i].Command;

                    for (auto j = 0; j < pUserCmd->input_history_size(); j++)
                    {
                        const auto history = pUserCmd->mutable_input_history(j);

                        if (history->player_tick_count() < nTickMinFx)
                        {
#ifdef LOG_FIX_TICKCOUNT
                            FLOG("Fix player %s<%llu> tickcount from %d to %d at %d.",
                                 pController->GetPlayerName(),
                                 pController->GetSteamID(),
                                 history->player_tick_count(),
                                 nTickMinFx,
                                 nTickCount);
#endif

                            history->set_player_tick_count(nTickMinFx);
                        }
                    }
                }
            }
        }

        if (commandCount > 0 && ms_disable_usercmd_subtick_moves->GetValue<bool>())
        {
            constexpr uint64_t EButtonInAttack = static_cast<uint64_t>(EButtons::IN_ATTACK);
            constexpr uint64_t EButtonInUse    = static_cast<uint64_t>(EButtons::IN_USE);
            constexpr uint64_t EButtonInReload = static_cast<uint64_t>(EButtons::IN_RELOAD);
            constexpr uint64_t LeftsButtons    = EButtonInAttack | EButtonInUse | EButtonInReload;

            for (auto i = 0; i < commandCount; i++)
            {
                const auto pCommand = &pCommands[i].Command;
                const auto pMoves   = pCommand->mutable_base()->mutable_subtick_moves();

                for (int32_t j = pMoves->size() - 1; j >= 0; --j)
                {
                    const auto pMove   = pMoves->Mutable(j);
                    const auto buttons = pMove->button();

                    if ((pMove->has_pitch_delta() && std::abs(pMove->pitch_delta() - 0) > FLT_EPSILON
                         || (pMove->has_yaw_delta() && std::abs(pMove->yaw_delta() - 0) > FLT_EPSILON)))
                    {
                        // LOG("Removed due to delta");
                        pMoves->DeleteSubrange(j, 1);
                    }
                    else if (const auto modified = buttons & LeftsButtons)
                    {
                        if (buttons != modified)
                        {
                            // LOG("Adjust Buttons -> %llu :: %llu", pMove->button(), buttons);
                            pMove->set_button(buttons);
                        }
                    }
                    else
                    {
                        // LOG("Removed Buttons -> %llu", pMove->button());
                        pMoves->DeleteSubrange(j, 1);
                    }
                }
            }
        }

#ifdef REMOVE_ATTACK
        if (buttons == EButtonInAttack)
        {
            pMoves->DeleteSubrange(j, 1);
            continue;
        }

        if (buttons & EButtonInAttack)
        {
            pMove->set_button(buttons & ~EButtonInAttack);
        }
#endif

        if (commandCount > 0 && ms_disable_usercmd_subtick_input->GetValue<bool>())
        {
            for (auto i = 0; i < commandCount; i++)
            {
                const auto pCommand = &pCommands[i].Command;

                pCommand->clear_input_history();
                pCommand->clear_attack1_start_history_index();
                pCommand->clear_attack2_start_history_index();
            }
        }

        return ProcessUserCommands(pController, pCommands, commandCount, paused, margin);
    }
}

static void PatchJumpInWaterVelocityZ()
{
    auto addresses = modules::server->FindPatternMulti("00 00 C8 42");

    ZydisDecoder decoder{};
    if (ZYAN_FAILED(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
    {
        FatalError("Failed to initialize decoder");
    }

    ZydisDecodedInstruction instr{};
    ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT]{};

    static constexpr int32_t  vel_offset     = offsetof(CMoveData, m_vecVelocity.z);
    static constexpr uint32_t new_z_velocity = 0x43110000; // 145.0f

    for (auto& address : addresses)
    {
        CAddress instruction_address = address - 8;

        for (auto j = 0; j < 5; j++)
        {
            if (ZYAN_SUCCESS(ZydisDecoderDecodeFull(&decoder, instruction_address.As<uint8_t*>(), ZYDIS_MAX_INSTRUCTION_LENGTH, &instr, operands))
                && instr.mnemonic == ZYDIS_MNEMONIC_MOV
                && instr.operand_count_visible == 2
                && operands[0].type == ZYDIS_OPERAND_TYPE_MEMORY
                && operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE
                && operands[0].mem.disp.value == vel_offset)
            {
                auto* target_address = instruction_address.Offset(instr.raw.imm[0].offset).As<uint8_t*>();

                SetMemoryAccess(target_address, sizeof(new_z_velocity), g_nReadWriteExecuteAccess);
                *reinterpret_cast<uint32_t*>(target_address) = new_z_velocity;
                SetMemoryAccess(target_address, sizeof(new_z_velocity), g_nReadExecuteAccess);

                FLOG("Successfully patched JumpInWaterVelocityZ @ server+0x%llx", instruction_address.GetPtr() - modules::server->Base());

                return;
            }

            instruction_address = instruction_address + instr.length;
        }
    }

    WARN("Found pattern for JumpInWaterVelocityZ but failed to validate and patch the instruction.");
}

void InstallMovementHook()
{
    InstallMemberDetourAutoSig(CPlayer_MovementServices, RunCommand);

    InstallMemberDetourAutoSig(CCSPlayer_MovementServices, WalkMove);
    InstallMemberDetourAutoSig(CCSPlayer_MovementServices, Accelerate);
    InstallMemberDetourAutoSig(CCSPlayer_MovementServices, ProcessMove);

    InstallVirtualHookAutoWithVTableAuto(CCSPlayer_MovementServices, CheckMovingGround, server);

    InstallMemberDetourAutoSig(CCSPlayerController, ProcessUserCommands);

    PatchJumpInWaterVelocityZ();

    g_pHookManager->Hook_ClientActivate(HookType_Post, [](PlayerSlot_t slot, const char* name, uint64_t steamId) {
        m_flLaggedMovementValue[slot] = DefaultSpeedValue;
        m_flRunSpeedValue[slot]       = DefaultSpeedValue;
    });

    g_pHookManager->Hook_PlayerSpawned(HookType_Pre, [](CCSPlayerPawn* pPawn, CServerSideClient* pClient) {
        m_flLaggedMovementValue[pClient->GetSlot()] = DefaultSpeedValue;
        m_flRunSpeedValue[pClient->GetSlot()]       = DefaultSpeedValue;
    });

    g_pHookManager->Hook_PlayerKilled(HookType_Pre, [](CCSPlayerPawn* pPawn, CServerSideClient* pClient) {
        m_flLaggedMovementValue[pClient->GetSlot()] = DefaultSpeedValue;
        m_flRunSpeedValue[pClient->GetSlot()]       = DefaultSpeedValue;
    });

    ms_disable_usercmd_subtick_moves = g_ConVarManager.CreateConVar("ms_disable_usercmd_subtick_moves", false, "Remove subtick moves command in ProcessUserCommands", FCVAR_RELEASE);
    ms_disable_usercmd_subtick_input = g_ConVarManager.CreateConVar("ms_disable_usercmd_subtick_input", false, "Remove subtick input command in ProcessUserCommands", FCVAR_RELEASE);
    ms_fix_usercmd_rapid_fire        = g_ConVarManager.CreateConVar("ms_fix_usercmd_rapid_fire", false, "Fix rapid fire command", FCVAR_RELEASE);
}

void SetPlayerLaggedMovementValue(CCSPlayerController* pController, float flValue)
{
    if (!pController)
        return;

    m_flLaggedMovementValue[pController->GetPlayerSlot()] = std::clamp(flValue, 0.01f, 5.0f);
}

void SetPlayerRunSpeedValue(CCSPlayerController* pController, float flValue)
{
    if (!pController)
        return;

    m_flRunSpeedValue[pController->GetPlayerSlot()] = std::clamp(flValue, 0.01f, 5.0f);
}

float GetPlayerLaggedMovementValue(CCSPlayerController* pController)
{
    if (!pController)
        return 1;

    return m_flLaggedMovementValue[pController->GetPlayerSlot()];
}

float GetPlayerRunSpeedValue(CCSPlayerController* pController)
{
    if (!pController)
        return 1;

    return m_flRunSpeedValue[pController->GetPlayerSlot()];
}