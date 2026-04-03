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

#ifndef MS_ROOT_GAMEDATA_H
#define MS_ROOT_GAMEDATA_H

#define RESOLVE_PAT_ENGINE(p) \
    modules::engine->FindPatternStrict(gamedata::signature::engine::p)

#define RESOLVE_PAT_SERVER(p) \
    modules::server->FindPatternStrict(gamedata::signature::server::p)

#define RESOLVE_PAT_ENGINE_M(c, p) \
    modules::engine->FindPatternStrict(gamedata::signature::engine::c::p)

#define RESOLVE_PAT_SERVER_M(c, p) \
    modules::server->FindPatternStrict(gamedata::signature::server::c::p)

namespace gamedata::offset::schemasystem
{
inline constexpr int FindDeclaredClass      = 2;
inline constexpr int GlobalType             = 11;
inline constexpr int FindTypeScopeForModule = 13;
} // namespace gamedata::offset::schemasystem

#include <cstdint>
#include <filesystem>
#include <optional>
#include <string>
#include <unordered_map>
#include <vector>

class IGameData
{
protected:
    virtual ~IGameData() = default;

public:
    virtual bool GetOffset(const char* name, int* offset)              = 0;
    virtual bool GetAddress(const char* name, std::uintptr_t& address) = 0;
    virtual bool GetVFunctionIndex(const char* name, int* offset)      = 0;
    virtual bool Register(const char* name, char* error, int maxlen)   = 0;
    virtual bool Unregister(const char* name, char* error, int maxlen) = 0;
};

struct GameDataOffset
{
    int32_t     m_Index;
    std::string m_File;
};

struct GameDataAddress
{
    std::string              m_Module;
    std::string              m_Signature;
    std::string              m_Base;
    std::string              m_Factory;
    std::vector<std::string> m_StringRefs{};
    std::vector<std::string> m_CvarRefs{};
    std::vector<std::string> m_VTableRefs{};
    std::string              m_FromVTable{};
    std::uintptr_t           m_FoundAddress = 0;
    std::string              m_File;
    bool                     m_LoadOnDemand = false;
};

struct GameDataPatch
{
    std::string          m_AddressKey;
    std::vector<uint8_t> m_ValidateBytes; // for validating
    std::vector<uint8_t> m_PatchBytes;
    std::vector<uint8_t> m_StoreBytes; // store for restoring
    std::string          m_File;
    uint8_t*             m_Address;
};

struct StringHash
{
    using is_transparent = void;
    size_t operator()(std::string_view txt) const noexcept { return std::hash<std::string_view>{}(txt); }
};

class GameData : public IGameData
{
public:
    bool GetOffset(const char* name, int* offset) override;
    bool GetAddress(const char* name, std::uintptr_t& address) override;
    bool GetVFunctionIndex(const char* name, int* offset) override;
    bool Register(const char* name, char* error, int maxlen) override;
    bool Unregister(const char* name, char* error, int maxlen) override;

    bool Register(const char* name);
    void Unregister(const char* name);
    int  GetOffset(const char* name);
    int  GetVFunctionIndex(const char* name);

    template <typename T>
    T GetAddress(const char* name)
    {
        return reinterpret_cast<T>(GetAddressInternal(name));
    }

private:
    bool  LoadRawTextJson(const char* content, const std::filesystem::path& path, char* error, int maxlen);
    void* GetAddressInternal(const char* name);
    bool  InitPatch(const std::string& name, GameDataPatch* item);

    std::unordered_map<std::string, GameDataOffset, StringHash, std::equal_to<>>  m_Offsets;
    std::unordered_map<std::string, GameDataOffset, StringHash, std::equal_to<>>  m_VFuncs;
    std::unordered_map<std::string, GameDataAddress, StringHash, std::equal_to<>> m_Addresses;
    std::unordered_map<std::string, GameDataPatch, StringHash, std::equal_to<>>   m_Patches;
};
#endif
