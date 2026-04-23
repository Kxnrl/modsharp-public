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

#include "gamedata.h"
#include "address.h"
#include "logging.h"
#include "module.h"
#include "sdkproxy.h"
#include "strtool.h"

#include "cstrike/interface/ICvar.h"
#include "memory/zydis_utility.h"

#include <Zydis.h>
#include <json.hpp>
#include <safetyhook.hpp>

#include <charconv>
#include <deque>
#include <fstream>

// #define DEBUG

static std::vector<uint8_t> ParseStringToBytesVector(const std::string& content)
{
    if (content.empty())
        return {};

    std::vector<uint8_t> items;
    const auto           validates = StringSplit(content.c_str(), " ");

    for (const auto& item : validates)
    {
        if (item.empty())
            continue;

        if (item.length() > 2)
            continue;

        const auto byte = static_cast<uint8_t>(strtol(item.c_str(), nullptr, 16));
        items.emplace_back(byte);
    }

    return items;
}

CModule* GetModuleByName(std::string_view module_name)
{
    static const std::unordered_map<std::string_view, CModule*> module_map = {
        {"engine",          modules::engine         },
        {"server",          modules::server         },
        {"tier0",           modules::tier0          },
        {"schemasystem",    modules::schemas        },
        {"resourcesystem",  modules::resource       },
        {"vscript",         modules::vscript        },
        {"vphysics2",       modules::vphysics2      },
        {"soundsystem",     modules::sound          },
        {"networksystem",   modules::network        },
        {"worldrenderer",   modules::worldrenderer  },
        {"matchmaking",     modules::matchmaking    },
        {"filesystem",      modules::filesystem     },
        {"steamsockets",    modules::steamsockets   },
        {"materialsystem2", modules::materialsystem2},
        {"animationsystem", modules::animationsystem}
    };

    auto it = module_map.find(module_name);

    if (it != module_map.end())
    {
        return it->second;
    }

    return nullptr;
}

static bool FindPattern(std::string_view module_name, std::string_view pattern, std::uintptr_t& address)
{
    auto module_ptr = GetModuleByName(module_name);
    if (module_ptr == nullptr)
    {
        FERROR("Unknown module name \"%s\"", module_name.data());
        return false;
    }

    if (pattern.empty())
        return false;

    // a sane symbol name should not contain a whitespace
    if (pattern.starts_with('@') && pattern.find(' ') == std::string_view::npos) [[unlikely]]
        address = module_ptr->GetFunctionByName(pattern.substr(1));
    else
        address = module_ptr->FindPatternStrict(pattern);

    return address != 0;
}

enum class RefResult : uint8_t
{
    NoReferences,
    Failed,
    Success
};

static RefResult FindFunctionFromReferences(const GameDataAddress& game_data, std::string_view key, std::uintptr_t& out_address)
{
    out_address = 0;

    std::string_view module_name = game_data.m_Module;
    if (module_name.empty()) [[unlikely]]
    {
        FERROR("Empty module name in \"%s\"", key.data());
        return RefResult::Failed;
    }

    auto module_ptr = GetModuleByName(module_name);
    if (!module_ptr) [[unlikely]]
    {
        FERROR("Unknown module name \"%s\" in %s", module_name.data(), key.data());
        return RefResult::Failed;
    }

    std::vector<std::span<const CModule::ReferenceEntry>> ref_sets;
    ref_sets.reserve(game_data.m_StringRefs.size() + game_data.m_CvarRefs.size() + game_data.m_FromVTable.size());

    std::deque<std::vector<CModule::ReferenceEntry>> union_reference_storage;

    auto collect_refs = [&](const std::string& name, const char* type_desc, auto find_target_fn) -> bool {
        auto target_addr = find_target_fn();

        if (!target_addr.IsValid())
        {
            FERROR("Failed to find %s \"%s\".", type_desc, name.c_str());
            return false;
        }

        auto references = module_ptr->GetReferenceRange(target_addr);
        if (references.empty())
        {
            FERROR("%s \"%s\" (at %s+0x%llx) has no references in code.", type_desc, name.c_str(), module_name.data(), target_addr.GetPtr() - module_ptr->Base());
            return false;
        }

        ref_sets.push_back(references);
        return true;
    };

    auto trim = [&](std::string_view str) -> std::string_view {
        while (!str.empty() && std::isspace(static_cast<unsigned char>(str.front())))
            str.remove_prefix(1);

        while (!str.empty() && std::isspace(static_cast<unsigned char>(str.back())))
            str.remove_suffix(1);

        return str;
    };

    for (const auto& raw_str : game_data.m_StringRefs)
    {
        if (raw_str.find("[ptr]") == std::string::npos)
        {
            if (!collect_refs(raw_str, "String", [&]() -> CAddress {
                return module_ptr->FindString(raw_str, false, true);
                }))
                return RefResult::Failed;

            continue;
        }

        std::string_view str_sv = trim(raw_str);
        if (str_sv.empty())
            continue;

        auto ptr_idx = str_sv.find("[ptr]");

        auto        substr_sv = trim(str_sv.substr(0, ptr_idx));
        std::string search_str(substr_sv);

        auto str_address = module_ptr->FindString(search_str, false, true);
        if (!str_address)
        {
            continue;
        }

        auto str_ptrs = module_ptr->FindPtrs(str_address);

        std::vector<CModule::ReferenceEntry> merged_refs;

        for (auto str_ptr : str_ptrs)
        {
            auto references = module_ptr->GetReferenceRange(str_ptr);
            if (!references.empty())
            {
                merged_refs.insert(merged_refs.end(), references.begin(), references.end());
            }
        }

        if (merged_refs.empty())
        {
            FERROR("String ptr \"%s\" (at %s+0x%llx) has no references in code.",
                   search_str.c_str(), module_name.data(), str_address.GetPtr() - module_ptr->Base());
            return RefResult::Failed;
        }

        union_reference_storage.emplace_back(std::move(merged_refs));
        ref_sets.emplace_back(union_reference_storage.back());
    }

    for (const auto& raw_ref : game_data.m_VTableRefs)
    {
        constexpr std::string_view suffix = "[typeinfo]";

        std::string_view ref_sv = trim(raw_ref);
        if (ref_sv.empty())
            continue;

        if (!ref_sv.ends_with(suffix))
        {
            std::string search_name(ref_sv);

            if (!collect_refs(search_name, "VTable", [&]() -> CAddress { return module_ptr->GetVirtualTableByName(search_name); }))
                return RefResult::Failed;

            continue;
        }

        auto        name_part = trim(ref_sv.substr(0, ref_sv.size() - suffix.size()));
        std::string search_name(name_part);

        if (!collect_refs(search_name, "TypeInfo", [&]() -> CAddress { return module_ptr->GetTypeInfoFromName(search_name); }))
            return RefResult::Failed;
    }

    for (const auto& cvar : game_data.m_CvarRefs)
    {
        std::string_view cvar_view = trim(cvar);
        if (cvar_view.empty())
            continue;

        const auto last_space_index = cvar_view.rfind(' ');

        std::string      cvar_name;
        std::string_view suffix;

        if (last_space_index == std::string_view::npos)
        {
            cvar_name = cvar;
        }
        else
        {
            std::string_view name_view = trim(cvar_view.substr(0, last_space_index));
            cvar_name                  = std::string(name_view);

            suffix = cvar_view.substr(last_space_index + 1);
        }

        auto convar_ptr = icvar->FindConVarIterator(cvar_name.c_str());
        if (!convar_ptr)
        {
            FLOG("Invalid cvar ptr \"%s\"", cvar_name.c_str());
            return RefResult::Failed;
        }

        auto ptr_to_cvar = module_ptr->FindPtr(reinterpret_cast<uintptr_t>(convar_ptr));
        if (!ptr_to_cvar.IsValid())
        {
            FLOG("Cannot find ptr to cvar \"%s\"", cvar_name.c_str());
            return RefResult::Failed;
        }

        bool add_ptr{};
        bool add_handle{};

        if (suffix.empty())
        {
            // use ptr for default behavior
            add_ptr = true;
        }
        else
        {
            const bool is_ptr    = (suffix == "[ptr]");
            const bool is_handle = (suffix == "[handle]");
            const bool is_both   = (suffix == "[*]" || suffix == "[both]");

            add_ptr    = (is_ptr || is_both);
            add_handle = (is_handle || is_both);

            // if suffix exists but is invalid, use ptr instead
            if (!add_ptr && !add_handle)
                add_ptr = true;
        }

        std::vector<CModule::ReferenceEntry> merged_refs;

        if (add_ptr)
        {
            auto refs = module_ptr->GetReferenceRange(ptr_to_cvar);
            if (!refs.empty())
                merged_refs.insert(merged_refs.end(), refs.begin(), refs.end());
        }

        if (add_handle)
        {
            auto refs = module_ptr->GetReferenceRange(ptr_to_cvar - sizeof(void*));
            if (!refs.empty())
            {
                for (const auto& ref : refs)
                {
                    constexpr int SEARCH_WINDOW = 64;
                    bool          pattern_found = false;

                    auto scan_start = ref.source_ip - SEARCH_WINDOW;
                    auto scan_end   = ref.source_ip + SEARCH_WINDOW;
                    auto current    = scan_start;

                    while (current < scan_end)
                    {
                        ZydisDecodedInstruction inst;
                        ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT];

                        if (ZYAN_SUCCESS(ZydisDecoderDecodeFull(&ZydisUtility::DefaultDecoder, reinterpret_cast<const void*>(current), 15, &inst, operands)))
                        {
                            if (inst.mnemonic == ZYDIS_MNEMONIC_MOV && inst.operand_count >= 2)
                            {
                                auto& op_dest = operands[0];
                                auto& op_src  = operands[1];

                                bool is_target_reg =
                                    (op_dest.type == ZYDIS_OPERAND_TYPE_REGISTER) && (op_dest.reg.value == ZYDIS_REGISTER_EDX || op_dest.reg.value == ZYDIS_REGISTER_ESI);

                                bool is_target_imm =
                                    (op_src.type == ZYDIS_OPERAND_TYPE_IMMEDIATE) && (static_cast<uint32_t>(op_src.imm.value.u) == 0xFFFFFFFF);

                                if (is_target_reg && is_target_imm)
                                {
                                    pattern_found = true;
                                    break;
                                }
                            }
                        }

                        current++;
                    }

                    if (pattern_found)
                        merged_refs.push_back(ref);
                }
            }
        }
        if (merged_refs.empty())
        {
            std::string type_str = (add_ptr && add_handle) ? "Ptr or Handle" : (add_handle ? "Handle" : "Ptr");
            FERROR("Cvar %s \"%s\" has no references in code.", type_str.c_str(), cvar_name.c_str());
            return RefResult::Failed;
        }

        union_reference_storage.emplace_back(std::move(merged_refs));
        ref_sets.emplace_back(union_reference_storage.back());
    }

    if (ref_sets.empty())
    {
        return RefResult::NoReferences;
    }

    auto matches = module_ptr->IntersectFunctionReferences(ref_sets);

    if (matches.empty())
    {
        FERROR("No references was found for %s", key.data());
        return RefResult::Failed;
    }

    if (auto vtable_name = game_data.m_FromVTable; vtable_name.empty())
    {
        if (matches.size() > 1)
        {
            FERROR("Ambiguous: %zu functions matched for %s.", matches.size(), key.data());
            return RefResult::Failed;
        }
    }
    else
    {
        std::vector<std::uintptr_t> vfuncs = module_ptr->GetVFunctionsFromVTable(vtable_name);
        if (vfuncs.empty())
        {
            FERROR("No vfuncs was found from %s for %s", vtable_name.c_str(), key.data());
            return RefResult::Failed;
        }

        std::ranges::sort(matches);
        std::ranges::sort(vfuncs);

        std::vector<std::uintptr_t> intersection;
        intersection.reserve(matches.size());

        std::ranges::set_intersection(matches, vfuncs, std::back_inserter(intersection));

        matches = std::move(intersection);

        if (matches.empty())
        {
            FERROR("Candidates found, but none exist in VTable %s for %s.", vtable_name.data(), key.data());
            return RefResult::Failed;
        }

        if (matches.size() > 1)
        {
            FERROR("Ambiguous: %zu functions matched within VTable %s for %s.", matches.size(), vtable_name.data(), key.data());
            return RefResult::Failed;
        }
    }

    out_address = matches[0];
    return RefResult::Success;
}

static bool FindAddress(std::unordered_map<std::string, GameDataAddress, StringHash, std::equal_to<>>& addresses, std::string_view name, std::uintptr_t& pAddress)
{
    auto it = addresses.find(name);

    if (it == addresses.end())
    {
        FERROR("Key '%s' does not exist", name.data());
        return false;
    }

    auto& item = it->second;

    if (item.m_FoundAddress != 0)
    {
        pAddress = item.m_FoundAddress;
        return true;
    }

    uintptr_t address = 0;
    if (!item.m_Base.empty())
    {
        if (!FindAddress(addresses, item.m_Base, address))
            return false;
    }
    else if (!item.m_Module.empty())
    {
        auto has_signature = item.m_Signature.empty() == false;

        std::uintptr_t ref_addr   = 0;
        auto           ref_result = FindFunctionFromReferences(item, name, ref_addr);

        if (ref_result == RefResult::Success)
        {
            address = ref_addr;
        }

        std::uintptr_t sig_addr  = 0;
        bool           sig_found = false;

        if (has_signature && (address == 0 || IsDebugMode()))
        {
            sig_found = FindPattern(item.m_Module, item.m_Signature, sig_addr);
        }

        if (address == 0)
        {
            if (ref_result == RefResult::Failed && has_signature)
            {
                WARN("Failed to find address for '%s', falling back to signature.", name.data());
            }

            if (sig_found)
            {
                address = sig_addr;
            }
        }

        if (IsDebugMode() && address != 0 && sig_found && address != sig_addr)
        {
            auto module       = GetModuleByName(item.m_Module);
            auto base_address = module->Base();

            auto rva_scan = address > base_address ? address - base_address : 0;
            auto rva_sig  = sig_addr > base_address ? sig_addr - base_address : 0;

            WARN("Address mismatch for %s!\n"
                 " > [Ref Scan]: %s+0x%llx\n"
                 " > [Sig Scan]: %s+0x%llx",
                 name.data(),
                 item.m_Module.c_str(), rva_scan,
                 item.m_Module.c_str(), rva_sig);
        }
    }

    if (address == 0)
    {
        return false;
    }

    // factory
    if (!item.m_Factory.empty())
    {
        auto factory_view = std::string_view(item.m_Factory);

        for (const auto subrange : factory_view | std::views::split(' '))
        {
            if (subrange.empty())
                continue;

            std::string_view op(subrange.begin(), subrange.end());

            if (op.empty()) [[unlikely]]
                continue;

            if (address < 0x10000) [[unlikely]]
                return false;

            if (const auto cmd = op.front(); cmd == 'r')
            {
                address = address + sizeof(int32_t) + *reinterpret_cast<int32_t*>(address);
            }
            else if (cmd == 'd') // Dereference
            {
                address = *reinterpret_cast<uintptr_t*>(address);
            }
            else
            {
                int32_t offset    = 0;
                auto    num_start = op.data();
                auto    num_end   = op.data() + op.size();

                if (cmd == '+' || cmd == '-')
                {
                    num_start++;
                }

                auto [ptr, ec] = std::from_chars(num_start, num_end, offset);

                if (ec == std::errc())
                {
                    if (cmd == '-')
                        address -= offset;
                    else
                        address += offset;
                }
            }
        }
    }

    pAddress            = address;
    item.m_FoundAddress = address;

    return address > 0;
}

static bool LoadGameDataFile(const std::filesystem::path& file_path, std::string& output_content, std::string& error_msg)
{
    std::ifstream f(file_path, std::ios::in | std::ios::binary | std::ios::ate);
    if (!f.is_open())
    {
        error_msg = "Failed to open file: " + file_path.generic_string();
        return false;
    }

    const auto file_size = f.tellg();
    if (file_size == -1 || file_size == 0)
    {
        error_msg = "Failed to read file size, or it is empty.";
        return false;
    }

    output_content.resize(file_size);

    f.seekg(0, std::ios::beg);
    f.read(output_content.data(), file_size);
    return true;
}

bool GameData::Register(const char* name, char* error, int maxlen)
{
    std::filesystem::path path = "../../sharp/gamedata/";
    path /= name;
    path.replace_extension("jsonc");

    std::string content{};
    std::string error_string{};

    if (!LoadGameDataFile(path, content, error_string))
    {
        snprintf(error, maxlen, "%s", error_string.c_str());
        return false;
    }

    return LoadRawTextJson(content.c_str(), path, error, maxlen);
}

bool GameData::Unregister(const char* name, char* error, int maxLen)
{
    std::filesystem::path file_path = "../../sharp/gamedata/";
    file_path /= name;

    for (auto it = m_Patches.cbegin(); it != m_Patches.cend();)
    {
        auto& patch = it->second;
        if (patch.m_File == file_path)
        {
            // restore patches
            if (patch.m_StoreBytes.empty())
            {
                FatalError("Failed to restore memory patch: %s", patch.m_AddressKey.c_str());
            }

            // restore patches
            const auto address = patch.m_Address;
            if (auto unprotect_guard = safetyhook::unprotect(address, patch.m_StoreBytes.size()))
            {
                memcpy(address, patch.m_StoreBytes.data(), patch.m_StoreBytes.size());
                FLOG("Patch Restored: %s", patch.m_AddressKey.c_str());
            }

            it = m_Patches.erase(it);
        }
        else
        {
            ++it;
        }
    }

    auto predicate = [&](const auto& pair) {
        return pair.second.m_File == file_path;
    };

    std::erase_if(m_Addresses, predicate);
    std::erase_if(m_Offsets, predicate);
    std::erase_if(m_VFuncs, predicate);

    return true;
}

bool GameData::GetOffset(const char* name, int* offset)
{
    if (auto it = m_Offsets.find(name); it != m_Offsets.end())
    {
        *offset = it->second.m_Index;
        return true;
    }

    return false;
}

bool GameData::GetAddress(const char* name, std::uintptr_t& address)
{
    address = 0;

    return FindAddress(m_Addresses, name, address);
}

bool GameData::GetVFunctionIndex(const char* name, int* offset)
{
    if (auto it = m_VFuncs.find(name); it != m_VFuncs.end())
    {
        *offset = it->second.m_Index;
        return true;
    }

    return false;
}

bool GameData::InitPatch(const std::string& name, GameDataPatch* item)
{
    const auto address = GetAddress<uint8_t*>(item->m_AddressKey.c_str());
    item->m_Address    = address;
    
    // validate
    if (!item->m_ValidateBytes.empty())
    {
        int i = 0;
        for (const auto byte : item->m_ValidateBytes)
        {
            if (address[i++] != byte)
            {
                FERROR("Patch validate failed: %s\n", name.c_str());
                return false;
            }
        }
    }

    const auto size = item->m_PatchBytes.size();
    if (auto unprotect_guard = safetyhook::unprotect(address, size))
    {
        item->m_StoreBytes.resize(size);
        memcpy(item->m_StoreBytes.data(), address, size);
        memcpy(address, item->m_PatchBytes.data(), size);
    }
    else
    {
        FERROR("Failed to unprotect memory at %p", address);
        return false;
    }

    FLOG("Patch initialized: %s", name.c_str());
    return true;
}

static void ParseAddresses(const std::filesystem::path& path, std::string_view platform_name, const nlohmann::json& json, std::unordered_map<std::string, GameDataAddress, StringHash, std::equal_to<>>& out)
{
    auto address_object = json.find("Addresses");
    if (address_object == json.end())
        return;

    for (auto& [key, entry_object] : address_object->items())
    {
        if (!entry_object.is_object())
        {
            continue;
        }

        GameDataAddress item;
        item.m_File = path.string();

        if (auto library_object = entry_object.find("library"); library_object != entry_object.end() && library_object->is_string())
        {
            item.m_Module = library_object->get<std::string>();
        }

        if (auto base_object = entry_object.find("base"); base_object != entry_object.end() && base_object->is_string())
        {
            item.m_Base = base_object->get<std::string>();
        }

        if (auto optional_object = entry_object.find("on_demand"); optional_object != entry_object.end() && optional_object->is_boolean())
        {
            item.m_LoadOnDemand = optional_object->get<bool>();
        }

        bool has_refs = false;

        if (entry_object.contains("refs"))
        {
            const auto& refs = entry_object["refs"];

            auto parse_ref_list = [](const nlohmann::json& node, const char* ref_key, std::vector<std::string>& out_vec) {
                if (!node.contains(ref_key))
                    return;

                const auto& val = node[ref_key];

                if (val.is_string())
                {
                    if (auto str = val.get<std::string>(); !str.empty())
                        out_vec.emplace_back(str);
                }
                else if (val.is_array())
                {
                    for (const auto& element : val)
                    {
                        if (element.is_string())
                        {
                            if (auto str = element.get<std::string>(); !str.empty())
                                out_vec.emplace_back(str);
                        }
                    }
                }
            };

            if (refs.is_object())
            {
                parse_ref_list(refs, "strings", item.m_StringRefs);
                parse_ref_list(refs, "cvars", item.m_CvarRefs);
                parse_ref_list(refs, "vtables", item.m_VTableRefs);
                if (refs.contains("vtable"))
                {
                    const auto& val = refs["vtable"];
                    if (val.is_string())
                    {
                        if (auto str = val.get<std::string>(); !str.empty())
                            item.m_FromVTable = std::move(str);
                    }
                }
            }
            has_refs = !item.m_StringRefs.empty() || !item.m_CvarRefs.empty() || !item.m_VTableRefs.empty() || !item.m_FromVTable.empty();
        }

        if (auto platform_object = entry_object.find(platform_name); platform_object != entry_object.end() && !platform_object->is_null())
        {
            if (platform_object->is_string())
            {
                auto str         = platform_object->get<std::string>();
                item.m_Signature = str;
            }
            else if (platform_object->is_object())
            {
                if (auto signature_object = platform_object->find("signature"); signature_object != platform_object->end() && signature_object->is_string())
                {
                    item.m_Signature = signature_object->get<std::string>();
                }
                if (auto base_object = platform_object->find("base"); base_object != platform_object->end() && base_object->is_string())
                {
                    item.m_Base = base_object->get<std::string>();
                }

                auto factory_object = platform_object->find("factory");
                if (factory_object != platform_object->end())
                {
                    if (factory_object->is_string())
                    {
                        item.m_Factory = factory_object->get<std::string>();
                    }
                    else if (factory_object->is_number())
                    {
                        item.m_Factory = "+" + factory_object->dump();
                        WARN("[%s] %s[\"%s\"][\"factory\"] is a number, this is not an expected behavior, parsing as string '%s'", path.filename().c_str(), key.c_str(), platform_name.data(), item.m_Factory.c_str());
                    }
                    else
                    {
                        FERROR("[%s] %s[\"%s\"][\"factory\"] must be a string", path.filename().c_str(), key.c_str(), platform_name.data());
                    }
                }
            }
        }

        if (item.m_Module.empty() && item.m_Base.empty())
        {
            continue;
        }

        if (item.m_Signature.empty() && !has_refs)
        {
            continue;
        }

        out[key] = std::move(item);
    }
}

static void ParseData(const std::filesystem::path& path, std::string_view platform_name, const nlohmann::json& json, const std::string& section_name, std::unordered_map<std::string, GameDataOffset, StringHash, std::equal_to<>>& out)
{
    auto section_object = json.find(section_name);
    if (section_object == json.end())
        return;

    if (!section_object->is_object())
    {
        WARN("[%s] %s is not a JSON object, ignoring", path.filename().generic_string().c_str(), section_name.c_str());
        return;
    }

    for (const auto& [key, entry_json] : section_object->items())
    {
        if (!entry_json.is_object())
        {
            WARN("[%s] %s[\"%s\"] is not a JSON object, ignoring", path.filename().generic_string().c_str(), section_name.c_str(), key.c_str());
            continue;
        }

        auto platform_object = entry_json.find(platform_name);
        if (platform_object == entry_json.end())
            continue;

        if (!platform_object->is_number_integer())
        {
            WARN("[%s] %s[\"%s\"][\"%s\"] is not an integer, ignoring.", path.filename().generic_string().c_str(), section_name.c_str(), key.c_str(), platform_name.data());
            continue;
        }

        auto& item   = out[key];
        item.m_Index = platform_object->get<std::int32_t>();
        item.m_File  = path.generic_string();
    }
}

static void ParsePatches(const std::filesystem::path& path, std::string_view platform_name, const nlohmann::json& json, std::unordered_map<std::string, GameDataPatch, StringHash, std::equal_to<>>& out)
{
    auto patches = json.find("Patches");
    if (patches == json.end())
        return;

    if (!patches->is_object())
    {
        WARN("[%s] Patches is not a JSON object, ignoring", path.filename().generic_string().c_str());
        return;
    }

    for (const auto& [key, entry_json] : patches->items())
    {
        if (!entry_json.is_object())
        {
            WARN("[%s] Patches[\"%s\"] is not a JSON object, ignoring", path.filename().generic_string().c_str(), key.c_str());

            continue;
        }

        auto address_object = entry_json.find("address");
        if (address_object == entry_json.end())
        {
            WARN("[%s] Patches[\"%s\"] does not contain key \"address\", ignoring", path.filename().generic_string().c_str(), key.c_str());

            continue;
        }

        auto platform_object = entry_json.find(platform_name);
        if (platform_object == entry_json.end())
        {
            continue;
        }

        auto patch_object = platform_object->find("patch");
        if (patch_object == platform_object->end())
        {
            WARN("[%s] Patches[\"%s\"][\"%s\"] does not contain key \"patch\", ignoring", path.filename().generic_string().c_str(), key.c_str(), platform_name.data());

            continue;
        }

        if (patch_object->is_null())
        {
            WARN("[%s] Patches[\"%s\"][\"%s\"] \"patch\" key is null, ignoring", path.filename().generic_string().c_str(), key.c_str(), platform_name);

            continue;
        }

        std::string str = patch_object->dump();
        std::erase(str, '\"');

        auto patch_bytes = ParseStringToBytesVector(str);
        if (patch_bytes.empty())
        {
            WARN("[%s] Patches[\"%s\"] parsed patch byte is empty (str: %s), ignoring", path.filename().generic_string().c_str(), key.c_str(), str.c_str());

            continue;
        }

        auto& item = out[key];

        item.m_File       = path.generic_string();
        item.m_AddressKey = address_object->get<std::string>();
        item.m_StoreBytes.resize(patch_bytes.size());

        if (auto validate_object = platform_object->find("validate"); validate_object != platform_object->end())
        {
            auto validate_str = validate_object->dump();
            std::erase(validate_str, '\"');

            item.m_ValidateBytes = ParseStringToBytesVector(validate_str);
        }

        item.m_PatchBytes = std::move(patch_bytes);
    }
}

bool GameData::LoadRawTextJson(const char* content, const std::filesystem::path& path, char* error, int maxlen)
{
#ifdef PLATFORM_WINDOWS
    static constexpr std::string_view platform_name = "windows";
#else
    static constexpr std::string_view platform_name = "linux";
#endif
    std::unordered_map<std::string, GameDataAddress, StringHash, std::equal_to<>> temp_addresses{};
    std::unordered_map<std::string, GameDataOffset, StringHash, std::equal_to<>>  temp_offsets{};
    std::unordered_map<std::string, GameDataOffset, StringHash, std::equal_to<>>  temp_vtables{};
    std::unordered_map<std::string, GameDataPatch, StringHash, std::equal_to<>>   temp_patches{};

    try
    {
        auto json = nlohmann::json::parse(content, /*callback*/ nullptr, /*allow_exception*/ true, /*ignore_comments*/ true);

        ParseAddresses(path, platform_name, json, temp_addresses);
        ParseData(path, platform_name, json, "Offsets", temp_offsets);
        ParseData(path, platform_name, json, "VFuncs", temp_vtables);
        ParsePatches(path, platform_name, json, temp_patches);

        if (temp_addresses.empty() && temp_offsets.empty() && temp_vtables.empty() && temp_patches.empty())
        {
            snprintf(error, maxlen, "No valid gamedata (addresses, offsets, etc.) found for platform '%s' in %s.", platform_name.data(), path.filename().string().c_str());
            return false;
        }
    }
    catch (const std::exception& ex)
    {
        FERROR("Error when parsing json %s: %s", path.filename().c_str(), ex.what());
        return false;
    }

    auto check_duplicates = [&](const auto& temp_map, const auto& main_map, const char* type_name) -> bool {
        for (const auto& [key, _] : temp_map)
        {
            if (main_map.contains(key))
            {
                snprintf(error, maxlen, "%s '%s' already exists.", type_name, key.c_str());
                return true;
            }
        }
        return false;
    };

    if (check_duplicates(temp_offsets, m_Offsets, "Offset"))
        return false;
    if (check_duplicates(temp_vtables, m_VFuncs, "VFunc"))
        return false;
    if (check_duplicates(temp_addresses, m_Addresses, "Address"))
        return false;
    if (check_duplicates(temp_patches, m_Patches, "Patch"))
        return false;

    // validate address
    std::vector<std::string> failed_addresses = {};
#ifdef DEBUG
    std::vector<std::string> succeeded_addresses = {};
#endif
    failed_addresses.reserve(temp_addresses.size());

    for (const auto& [name, value] : temp_addresses)
    {
        if (value.m_LoadOnDemand)
            continue;

        std::uintptr_t address{};
        if (!FindAddress(temp_addresses, name, address))
        {
            failed_addresses.emplace_back(name);
        }
#ifdef DEBUG
        else
        {
            succeeded_addresses.emplace_back(name);
        }
#endif
    }

    auto format_address_list = [](const std::vector<std::string>& list) -> std::string {
        std::string result;
        if (list.empty())
            return result;

        result.reserve(list.size() * 40);

        for (size_t i = 0; i < list.size(); ++i)
        {
            result.append(" ").append(std::to_string(i + 1)).append(": ").append(list[i]).append("\n");
        }
        return result;
    };

#ifdef DEBUG
    if (!succeeded_addresses.empty())
    {
        std::string msg = format_address_list(succeeded_addresses);
        FLOG("Address resolve succeeded:\n%s", msg.c_str());
        fflush(stdout);
    }
#endif

    if (!failed_addresses.empty()) [[unlikely]]
    {
        std::string msg = format_address_list(failed_addresses);
        FERROR("Address resolve failed:\n%s", msg.c_str());
        fflush(stdout);

        snprintf(error, maxlen, "Total %zu sigs resolve failed.\n", failed_addresses.size());
        return false;
    }

    m_Offsets.merge(temp_offsets);
    m_VFuncs.merge(temp_vtables);
    m_Addresses.merge(temp_addresses);

    for (auto& [name, patch] : temp_patches)
    {
        if (!InitPatch(name, &patch))
            return false;

        m_Patches[name] = std::move(patch);
    }

    return true;
}

bool GameData::Register(const char* name)
{
    char error[256];
    return Register(name, error, sizeof(error));
}

void GameData::Unregister(const char* name)
{
    char error[256];
    if (!Unregister(name, error, sizeof(error)))
    {
        FatalError(error);
    }
}

int GameData::GetOffset(const char* name)
{
    int offset = 0;
    if (!GetOffset(name, &offset))
    {
        FatalError("Cant find offset: \"%s\"", name);
    }

    return offset;
}

int GameData::GetVFunctionIndex(const char* name)
{
    int index{};
    if (!GetVFunctionIndex(name, &index))
    {
        FatalError("Cant find vtable index: \"%s\"", name);
    }

    return index;
}

void* GameData::GetAddressInternal(const char* name)
{
    std::uintptr_t address{};

    if (!GetAddress(name, address))
    {
        FatalError("Cant find address: \"%s\"", name);
    }

    return reinterpret_cast<void*>(address);
}