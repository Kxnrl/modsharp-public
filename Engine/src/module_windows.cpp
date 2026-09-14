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

#ifdef PLATFORM_WINDOWS

#    ifndef NOMINMAX
#        define NOMINMAX
#    endif

#    include "logging.h"
#    include "memory/zydis_utility.h"
#    include "module.h"
#    include "scopetimer.h"

#    include <algorithm>
#    include <array>
#    include <cinttypes>
#    include <cstring>
#    include <format>
#    include <fstream>
#    include <iterator>
#    include <ranges>
#    include <thread>

#    include <windows.h>
#    include <dbghelp.h>
#    include <winternl.h>

#    ifdef __clang__
// clang-cl compat: MSVC pre-defines _ThrowInfo as a compiler built-in type.
// Newer MSVC CRT headers (ehdata_forceinclude.h) define ThrowInfo at line 178
// but reference _ThrowInfo at line 187. clang-cl lacks this built-in, so we
// use a macro to map _ThrowInfo -> ThrowInfo before the header is parsed.
#        define _ThrowInfo ThrowInfo
#    endif
#    include <rttidata.h>
#    ifdef __clang__
#        undef _ThrowInfo
#    endif
#    include <vcruntime.h>

namespace
{
[[nodiscard]] bool IsExecutableAddress(const std::vector<CModule::Segment>& segments,
                                       std::uintptr_t                       address) noexcept
{
    return std::ranges::any_of(segments, [address](const CModule::Segment& segment) {
        return (segment.flags & FLAG_X) != 0
               && segment.address <= address && address < segment.address + segment.size;
    });
}

[[nodiscard]] bool DecodeInstruction(std::uintptr_t           ip,
                                     std::uintptr_t           limit,
                                     ZydisDecodedInstruction& instr,
                                     ZydisDecoderContext&     context) noexcept
{
    if (ip >= limit)
        return false;

    const auto available = std::min<std::size_t>(limit - ip, ZYDIS_MAX_INSTRUCTION_LENGTH);
    return ZYAN_SUCCESS(ZydisDecoderDecodeInstruction(&ZydisUtility::DefaultDecoder,
                                                      &context,
                                                      reinterpret_cast<const void*>(ip),
                                                      available, &instr));
}

[[nodiscard]] bool DecodeOperands(const ZydisDecoderContext&     context,
                                  const ZydisDecodedInstruction& instr,
                                  ZydisDecodedOperand*           operands) noexcept
{
    if (instr.operand_count_visible == 0)
        return true;

    return ZYAN_SUCCESS(ZydisDecoderDecodeOperands(&ZydisUtility::DefaultDecoder,
                                                   &context, &instr, operands,
                                                   instr.operand_count_visible));
}

[[nodiscard]] bool IsZeroPadding(std::uintptr_t address, std::uintptr_t limit) noexcept
{
    constexpr std::size_t padding_probe_size = 8;
    constexpr std::array<std::uint8_t, padding_probe_size> zero_padding{};

    if (address >= limit)
        return false;

    const auto probe_size = std::min<std::size_t>(padding_probe_size, limit - address);
    return std::memcmp(reinterpret_cast<const void*>(address), zero_padding.data(),
                       probe_size) == 0;
}

[[nodiscard]] std::uintptr_t RecoverFunctionEnd(const std::vector<CModule::Segment>& segments,
                                                std::uintptr_t                       start,
                                                std::uintptr_t                       hard_end,
                                                const std::vector<std::uintptr_t>&   authoritative_starts,
                                                const std::vector<std::uintptr_t>&   accepted_starts)
{
    if (start >= hard_end)
        return 0;

    const auto first_byte = *reinterpret_cast<const std::uint8_t*>(start);
    if (first_byte == 0xCC || IsZeroPadding(start, hard_end))
        return 0;

    auto                    ip                 = start;
    auto                    required_end       = start;
    bool                    last_was_exec_call = false;
    ZydisDecoderContext     context{};
    ZydisDecodedInstruction instr{};
    ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT]{};

    while (ip < hard_end)
    {
        if (IsZeroPadding(ip, hard_end))
        {
            if (ip < required_end)
            {
                ++ip;
                last_was_exec_call = false;
                continue;
            }
            return last_was_exec_call ? ip : 0;
        }

        if (!DecodeInstruction(ip, hard_end, instr, context))
            return 0;
        if (ip == start && instr.mnemonic == ZYDIS_MNEMONIC_NOP)
            return 0;

        const auto next_ip = ip + instr.length;

        const auto is_relative = (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE) != 0;
        const auto needs_operands = is_relative
                                    && ((instr.opcode == 0xE8
                                         && instr.meta.category == ZYDIS_CATEGORY_CALL)
                                        || instr.meta.category == ZYDIS_CATEGORY_UNCOND_BR
                                        || instr.meta.category == ZYDIS_CATEGORY_COND_BR);
        if (needs_operands && !DecodeOperands(context, instr, operands))
            return 0;

        bool in_exec_call = false;
        if (instr.opcode == 0xE8
            && instr.meta.category == ZYDIS_CATEGORY_CALL
            && is_relative)
        {
            const auto target = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
            in_exec_call      = IsExecutableAddress(segments, target);
        }

        if (is_relative
            && (instr.meta.category == ZYDIS_CATEGORY_UNCOND_BR
                || instr.meta.category == ZYDIS_CATEGORY_COND_BR)
            && operands[0].type == ZYDIS_OPERAND_TYPE_IMMEDIATE)
        {
            const auto target   = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
            const auto in_range = start <= target && target < hard_end;

            if (instr.meta.category == ZYDIS_CATEGORY_COND_BR)
            {
                if (in_range)
                    required_end = std::max(required_end, target + 1);
                else if (!std::ranges::binary_search(authoritative_starts, target)
                         && !std::ranges::binary_search(accepted_starts, target,
                                                        std::ranges::greater{}))
                    return 0;

                last_was_exec_call = false;
                ip                 = next_ip;
                continue;
            }

            if (in_range)
            {
                if (target > ip)
                {
                    required_end       = std::max(required_end, target + 1);
                    last_was_exec_call = false;
                    ip                 = target;
                    continue;
                }

                if (next_ip >= required_end)
                    return next_ip;
                last_was_exec_call = false;
                ip                 = next_ip;
                continue;
            }

            if (instr.opcode != 0xE9 || !IsExecutableAddress(segments, target))
                return 0;
            if (next_ip >= required_end)
                return next_ip;
            last_was_exec_call = false;
            ip                 = next_ip;
            continue;
        }

        if (instr.meta.category == ZYDIS_CATEGORY_RET
            || instr.meta.category == ZYDIS_CATEGORY_UNCOND_BR
            || instr.mnemonic == ZYDIS_MNEMONIC_INT3
            || instr.mnemonic == ZYDIS_MNEMONIC_UD2)
        {
            if (next_ip >= required_end)
                return next_ip;
        }

        last_was_exec_call = in_exec_call;
        ip                 = next_ip;
    }

    return last_was_exec_call ? ip : 0;
}

std::string Undecorate(const char* decorated_name, DWORD flags)
{
    std::array<char, 16384> buffer{};
    if (UnDecorateSymbolName(decorated_name, buffer.data(), static_cast<DWORD>(buffer.size()), flags) == 0)
        return {};
    return buffer.data();
}
} // namespace

void CModule::GetModuleInfo(std::string_view mod)
{
    HMODULE handle = GetModuleHandleA(mod.data());
    if (!handle)
        return;

    _module_name.resize(MAX_PATH);
    auto actual_size = GetModuleFileNameA(handle, _module_name.data(), MAX_PATH);
    _module_name.resize(actual_size);

    const std::string module_path = _module_name;
    _module_name = _module_name.substr(_module_name.find_last_of('\\') + 1);

    _base_address = reinterpret_cast<uintptr_t>(handle);

    const auto dosHeader = reinterpret_cast<PIMAGE_DOS_HEADER>(handle);

    if (dosHeader->e_magic != IMAGE_DOS_SIGNATURE)
        return;

    const auto bytes = reinterpret_cast<uint8_t*>(handle);

    const auto ntHeader = reinterpret_cast<PIMAGE_NT_HEADERS>(bytes + dosHeader->e_lfanew);

    if (ntHeader->Signature != IMAGE_NT_SIGNATURE)
        return;

    auto section = IMAGE_FIRST_SECTION(ntHeader);

    std::ifstream module_file(module_path, std::ios::binary);
    std::vector<uint8_t> file_image((std::istreambuf_iterator<char>(module_file)), std::istreambuf_iterator<char>());

    _size = ntHeader->OptionalHeader.SizeOfImage;

    for (auto i = 0u; i < ntHeader->FileHeader.NumberOfSections; i++, section++)
    {
        const auto isExecutable = (section->Characteristics & IMAGE_SCN_MEM_EXECUTE) != 0;
        const auto isReadable   = (section->Characteristics & IMAGE_SCN_MEM_READ) != 0;
        const auto isWritable   = (section->Characteristics & IMAGE_SCN_MEM_WRITE) != 0;

        const auto start = this->_base_address + section->VirtualAddress;
        const auto size  = section->Misc.VirtualSize;

        auto& segment   = _segments.emplace_back();
        segment.address = start;
        segment.size    = size;
        if (isExecutable)
            segment.flags |= FLAG_X;
        if (isReadable)
            segment.flags |= FLAG_R;
        if (isWritable)
            segment.flags |= FLAG_W;

        const auto* runtime_data = reinterpret_cast<const uint8_t*>(start);
        segment.data.assign(runtime_data, runtime_data + size);

        if (isExecutable && !file_image.empty())
        {
            const auto raw_offset = static_cast<std::size_t>(section->PointerToRawData);
            const auto raw_size   = static_cast<std::size_t>(section->SizeOfRawData);
            if (raw_offset <= file_image.size() && raw_size <= file_image.size() - raw_offset)
            {
                segment.data.assign(size, 0);
                const auto copy_size = std::min<std::size_t>(size, raw_size);
                std::copy_n(file_image.data() + raw_offset, copy_size, segment.data.data());
            }
        }
    }

    DumpExports(handle);
    _createInterFaceFn = GetExportByName("CreateInterface");

    {
        ScopedTimer timer(_module_name + "::DumpVTables");
        DumpVtables();
    }
    {
        ScopedTimer timer(_module_name + "::BuildFunctionIndexAndReferences");
        BuildFunctionIndexAndReferences();
    }
}

void CModule::DumpExports(void* module_base)
{
    const auto base       = reinterpret_cast<uintptr_t>(module_base);
    const auto dos_header = reinterpret_cast<PIMAGE_DOS_HEADER>(base);
    const auto nt_header  = reinterpret_cast<PIMAGE_NT_HEADERS>(base + dos_header->e_lfanew);
    const auto& directory = nt_header->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];

    if (directory.VirtualAddress == 0 || directory.Size < sizeof(IMAGE_EXPORT_DIRECTORY))
        return;

    const auto exports = reinterpret_cast<PIMAGE_EXPORT_DIRECTORY>(base + directory.VirtualAddress);
    const auto names   = reinterpret_cast<const DWORD*>(base + exports->AddressOfNames);

    for (DWORD i = 0; i < exports->NumberOfNames; ++i)
    {
        const auto decorated_name = reinterpret_cast<const char*>(base + names[i]);
        // Resolve through GetProcAddress so forwarded exports keep their real target address.
        const auto address = reinterpret_cast<uintptr_t>(GetProcAddress(reinterpret_cast<HMODULE>(module_base), decorated_name));
        if (address == 0)
            continue;

        auto signature = std::string(decorated_name);
        if (decorated_name[0] == '?')
        {
            constexpr auto flags = UNDNAME_NO_MS_KEYWORDS
                                   | UNDNAME_NO_FUNCTION_RETURNS
                                   | UNDNAME_NO_ACCESS_SPECIFIERS;
            if (auto demangled = Undecorate(decorated_name, flags); !demangled.empty())
                signature = std::move(demangled);
        }

        AddExportSymbol(decorated_name, signature, address);
    }
}

void CModule::BuildFunctionIndexAndReferences()
{
    // from praydog https://github.com/cursey/kananlib/blob/7a99a94cea3dbcbd46b54885bd3d04f1d242e21a/src/Scan.cpp#L1329-L1344
    _function_entries.clear();
    _references.clear();

    const auto dos_header = reinterpret_cast<PIMAGE_DOS_HEADER>(_base_address);
    const auto nt_header  = reinterpret_cast<PIMAGE_NT_HEADERS>(_base_address + dos_header->e_lfanew);

    const auto section_count = std::min<std::size_t>(nt_header->FileHeader.NumberOfSections,
                                                     _segments.size());
    if (!std::ranges::any_of(_segments, [](const Segment& segment) {
            return (segment.flags & FLAG_X) != 0 && segment.size != 0;
        }))
        return;

    const auto is_in_text_section = [&](std::uintptr_t address) noexcept {
        return IsExecutableAddress(_segments, address);
    };
    const auto is_in_data_section = [this](std::uintptr_t address) noexcept {
        return std::ranges::any_of(_segments, [address](const Segment& segment) {
            return (segment.flags & FLAG_X) == 0
                   && segment.address <= address && address < segment.address + segment.size;
        });
    };

    const auto directory = &nt_header->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION];

    const auto rva  = directory->VirtualAddress;
    const auto size = directory->Size;

    const auto directory_ptr = reinterpret_cast<PIMAGE_RUNTIME_FUNCTION_ENTRY>(_base_address + rva);

    const auto entries = size / sizeof(IMAGE_RUNTIME_FUNCTION_ENTRY);

    if (entries <= 0)
    {
        FERROR("No exception directory entries was found for %s", _module_name.c_str());
    }

    _function_entries.reserve(entries);

    // https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170
    struct UNWIND_INFO
    {
        uint8_t Version : 3;
        uint8_t Flags : 5;
        uint8_t SizeOfProlog;
        uint8_t CountOfCodes;
        uint8_t FrameRegister : 4;
        uint8_t FrameOffset : 4;
    };

    for (size_t i = 0; i < entries;)
    {
        auto start_exception = &directory_ptr[i];
        auto start_address   = start_exception->BeginAddress;
        auto end_address     = start_exception->EndAddress;

        size_t next_i = i + 1;
        while (next_i < entries && directory_ptr[next_i].BeginAddress == end_address)
        {
            // checking UNW_FLAG_CHAININFO flag in next entry's unwind data
            // if that flag is set, meaning the next entry belongs to current entry, we should merge it
            // otherwise we treat it as a new function
            auto next_unwind_rva = directory_ptr[next_i].UnwindData;
            auto next_unwind_ptr = reinterpret_cast<UNWIND_INFO*>(_base_address + next_unwind_rva);

            if ((next_unwind_ptr->Flags & UNW_FLAG_CHAININFO) == 0) break;

            // flag is set, merge
            end_address = directory_ptr[next_i].EndAddress;
            next_i++;
        }

        auto& entry = _function_entries.emplace_back();
        entry.start = _base_address + start_address;
        entry.end   = _base_address + end_address;

        i = next_i;
    }

    std::ranges::sort(_function_entries,
                      [](const FunctionEntry& a, const FunctionEntry& b) {
                          return a.start < b.start;
                      });

    const auto authoritative_entries = _function_entries;

    std::vector<std::uintptr_t> authoritative_starts;
    authoritative_starts.reserve(authoritative_entries.size());
    for (const auto& entry : authoritative_entries)
        authoritative_starts.push_back(entry.start);

    const auto find_authoritative = [&](std::uintptr_t address) -> const FunctionEntry* {
        const auto it = std::ranges::upper_bound(authoritative_entries, address, {}, &FunctionEntry::start);
        if (it == authoritative_entries.begin())
            return nullptr;
        const auto candidate = std::prev(it);
        return address < candidate->end ? &*candidate : nullptr;
    };

    std::vector<std::uintptr_t> supplemental_starts;
    const auto is_supplemental_candidate = [&](std::uintptr_t address) {
        if (!is_in_text_section(address))
            return false;
        if (find_authoritative(address) != nullptr)
            return false;
        return true;
    };

    const auto section_name_is = [](const IMAGE_SECTION_HEADER* header, std::string_view expected) {
        std::array<char, IMAGE_SIZEOF_SHORT_NAME + 1> name{};
        std::memcpy(name.data(), header->Name, IMAGE_SIZEOF_SHORT_NAME);
        return std::string_view(name.data()) == expected;
    };

    auto section = IMAGE_FIRST_SECTION(nt_header);
    for (std::size_t i = 0; i < section_count; ++i, ++section)
    {
        if (!section_name_is(section, ".rdata"))
            continue;

        const auto& segment   = _segments[i];
        const auto  scan_size = std::min(segment.size, segment.data.size());
        for (std::size_t offset = 0; offset + sizeof(std::uintptr_t) <= scan_size;
             offset += sizeof(std::uintptr_t))
        {
            std::uintptr_t potential{};
            std::memcpy(&potential, segment.data.data() + offset, sizeof(potential));
            if (is_supplemental_candidate(potential))
                supplemental_starts.push_back(potential);
        }
    }

    std::ranges::sort(supplemental_starts);
    supplemental_starts.erase(std::ranges::unique(supplemental_starts).begin(),
                              supplemental_starts.end());

    constexpr std::size_t max_supplemental_size = 100'000;

    const auto build_supplemental_entries = [&] {
        std::vector<std::uintptr_t> accepted_starts;
        accepted_starts.reserve(supplemental_starts.size());

        std::vector<FunctionEntry> result;
        result.reserve(supplemental_starts.size());

        for (auto candidate = supplemental_starts.rbegin(); candidate != supplemental_starts.rend(); ++candidate)
        {
            const auto start      = *candidate;
            const auto executable = std::ranges::find_if(_segments, [start](const Segment& segment) {
                return (segment.flags & FLAG_X) != 0
                       && segment.address <= start && start < segment.address + segment.size;
            });
            if (executable == _segments.end())
                continue;

            auto hard_end = executable->address + executable->size;
            if (hard_end - start > max_supplemental_size)
                hard_end = start + max_supplemental_size;

            const auto next_authoritative = std::ranges::upper_bound(authoritative_starts, start);
            if (next_authoritative != authoritative_starts.end()
                && *next_authoritative < hard_end)
                hard_end = *next_authoritative;

            if (!accepted_starts.empty() && accepted_starts.back() < hard_end)
                hard_end = accepted_starts.back();

            if (const auto end = RecoverFunctionEnd(_segments, start, hard_end,
                                                    authoritative_starts, accepted_starts);
                end != 0)
            {
                result.push_back({start, end});
                accepted_starts.push_back(start);
            }
        }

        std::ranges::sort(result, {}, &FunctionEntry::start);
        return result;
    };

    struct ScannedReference
    {
        ReferenceEntry reference;
        std::uintptr_t range_start;
    };

    // multithreaded solution inspired by the code snippet @angelfor3v3r gave me a long time ago.
    // to be honest i could have used yaxpeax-x86, which is the fastest decoder i have found yet (it takes about 100ms to decode the entire .text section
    // in libserver.so while zydis takes ~450ms), but i dont think it is worth the effort to replace zydis with it,
    // not to mention safetyhook also uses zydis and i use the encoder feature from zydis too.
    // hopefully no one copies or recodes this function in another language and claims they coded it without giving credit 😭🙏

    const auto max_discovery_threads = std::max(1u, std::thread::hardware_concurrency());
    std::vector<std::vector<std::uintptr_t>> candidate_results(max_discovery_threads);
    std::vector<std::vector<ScannedReference>> reference_results(max_discovery_threads);
    std::vector<std::vector<ScannedReference>> jump_reference_results(max_discovery_threads);
    std::vector<std::thread>                    worker_threads;
    worker_threads.reserve(max_discovery_threads);

    const auto discover_from_ranges = [&](const std::vector<FunctionEntry>& ranges,
                                          bool collect_candidates) {
        if (ranges.empty())
            return;

        const auto num_threads = std::min<std::size_t>(max_discovery_threads, ranges.size());
        worker_threads.clear();
        for (std::size_t i = 0; i < num_threads; ++i)
            candidate_results[i].clear();

        const auto scan_chunk = [&](std::size_t idx, std::size_t start_idx, std::size_t end_idx) {
            auto& local_candidates = candidate_results[idx];
            auto& local_refs       = reference_results[idx];
            auto& local_jumps      = jump_reference_results[idx];
            local_candidates.reserve((end_idx - start_idx) * 2);
            local_refs.reserve(local_refs.size() + (end_idx - start_idx) * 10);
            local_jumps.reserve(local_jumps.size() + (end_idx - start_idx));

            ZydisDecoderContext     context{};
            ZydisDecodedInstruction instr{};
            ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT]{};

            for (std::size_t i = start_idx; i < end_idx; ++i)
            {
                const auto& entry = ranges[i];

                std::uintptr_t jump_table_start{};
                for (auto ip = entry.start; ip < entry.end;)
                {
                    if (jump_table_start != 0 && ip >= jump_table_start)
                        break;

                    if (!DecodeInstruction(ip, entry.end, instr, context))
                    {
                        ++ip;
                        continue;
                    }

                    const auto next_ip = ip + instr.length;
                    const auto is_relative = (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE) != 0;
                    const auto needs_operands = is_relative
                                                || (jump_table_start == 0
                                                    && instr.operand_count_visible > 1
                                                    && (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                                                        || instr.mnemonic == ZYDIS_MNEMONIC_MOVSXD));
                    if (needs_operands && !DecodeOperands(context, instr, operands))
                    {
                        ip = next_ip;
                        continue;
                    }

                    if (is_relative)
                    {
                        if (instr.opcode == 0xE8
                            && instr.meta.category == ZYDIS_CATEGORY_CALL)
                        {
                            const auto target = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
                            if (is_in_text_section(target))
                            {
                                local_refs.push_back({{target, ip}, entry.start});
                                if (collect_candidates && find_authoritative(target) == nullptr)
                                    local_candidates.push_back(target);
                            }
                        }
                        else if (instr.opcode == 0xE9
                                 && instr.meta.category == ZYDIS_CATEGORY_UNCOND_BR)
                        {
                            const auto target = ZydisUtility::GetAbsoluteAddress(instr, operands[0], ip);
                            if (is_in_text_section(target))
                            {
                                local_jumps.push_back({{target, ip}, entry.start});
                                if (collect_candidates
                                    && (target < entry.start || target >= entry.end)
                                    && find_authoritative(target) == nullptr)
                                    local_candidates.push_back(target);
                            }
                        }

                        for (ZyanU8 operand_idx = 0; operand_idx < instr.operand_count_visible; ++operand_idx)
                        {
                            if (operands[operand_idx].type != ZYDIS_OPERAND_TYPE_MEMORY)
                                continue;

                            const auto target = ZydisUtility::GetAbsoluteAddress(instr, operands[operand_idx], ip);
                            if (is_in_data_section(target))
                                local_refs.push_back({{target, ip}, entry.start});
                        }
                    }
                    else if (jump_table_start == 0
                             && instr.operand_count_visible > 1
                             && (instr.mnemonic == ZYDIS_MNEMONIC_MOV
                                 || instr.mnemonic == ZYDIS_MNEMONIC_MOVSXD))
                    {
                        const auto& src = operands[1];
                        if (src.type == ZYDIS_OPERAND_TYPE_MEMORY
                            && src.mem.index != ZYDIS_REGISTER_NONE
                            && (src.mem.segment == ZYDIS_REGISTER_DS
                                || src.mem.segment == ZYDIS_REGISTER_SS)
                            && src.mem.scale != 0 && src.mem.disp.value > 0)
                        {
                            const auto target = src.mem.disp.value + _base_address;
                            if (is_in_text_section(target))
                                jump_table_start = target;
                        }
                    }

                    ip = next_ip;
                }
            }
        };

        for (std::size_t i = 0; i < num_threads; ++i)
        {
            const auto start_idx = ranges.size() * i / num_threads;
            const auto end_idx   = ranges.size() * (i + 1) / num_threads;
            worker_threads.emplace_back(scan_chunk, i, start_idx, end_idx);
        }

        for (auto& thread : worker_threads)
            thread.join();

        if (!collect_candidates)
            return;

        std::size_t candidate_count{};
        for (std::size_t i = 0; i < num_threads; ++i)
        {
            std::ranges::sort(candidate_results[i]);
            candidate_results[i].erase(std::ranges::unique(candidate_results[i]).begin(),
                                       candidate_results[i].end());
            candidate_count += candidate_results[i].size();
        }
        supplemental_starts.reserve(supplemental_starts.size() + candidate_count);

        for (std::size_t i = 0; i < num_threads; ++i)
        {
            for (const auto candidate : candidate_results[i])
                supplemental_starts.push_back(candidate);
        }

        std::ranges::sort(supplemental_starts);
        supplemental_starts.erase(std::ranges::unique(supplemental_starts).begin(),
                                  supplemental_starts.end());
    };

    auto supplemental_entries = build_supplemental_entries();

    std::vector<FunctionEntry> ranges_to_scan = authoritative_entries;
    ranges_to_scan.insert(ranges_to_scan.end(), supplemental_entries.begin(), supplemental_entries.end());
    std::ranges::sort(ranges_to_scan, {}, &FunctionEntry::start);

    std::vector<std::pair<std::uintptr_t, std::uintptr_t>> scanned_ranges;
    scanned_ranges.reserve(ranges_to_scan.size() + supplemental_starts.size());

    std::size_t           discovery_rounds{};
    constexpr std::size_t max_discovery_rounds = 64;
    while (!ranges_to_scan.empty() && discovery_rounds < max_discovery_rounds)
    {
        const auto previous_candidate_count = supplemental_starts.size();
        discover_from_ranges(ranges_to_scan, true);

        ++discovery_rounds;
        if (supplemental_starts.size() == previous_candidate_count)
        {
            ranges_to_scan.clear();
            break;
        }

        for (const auto& entry : ranges_to_scan)
            scanned_ranges.emplace_back(entry.start, entry.end);
        std::ranges::sort(scanned_ranges);
        scanned_ranges.erase(std::ranges::unique(scanned_ranges).begin(), scanned_ranges.end());

        supplemental_entries = build_supplemental_entries();

        ranges_to_scan.clear();
        for (const auto& entry : supplemental_entries)
        {
            if (!std::ranges::binary_search(scanned_ranges,
                                            std::pair{entry.start, entry.end}))
                ranges_to_scan.push_back(entry);
        }
    }
    if (!ranges_to_scan.empty())
    {
        FERROR("Function discovery reached the round limit for %s", _module_name.c_str());
        discover_from_ranges(ranges_to_scan, false);
    }

    _function_entries = authoritative_entries;
    _function_entries.insert(_function_entries.end(), supplemental_entries.begin(), supplemental_entries.end());
    std::ranges::sort(_function_entries, {}, &FunctionEntry::start);

    if (_function_entries.empty())
        return;

    const auto is_function_entry = [this](std::uintptr_t address) noexcept {
        return std::ranges::binary_search(_function_entries, address, {}, &FunctionEntry::start);
    };

    std::size_t cached_reference_count{};
    for (std::size_t i = 0; i < max_discovery_threads; ++i)
        cached_reference_count += reference_results[i].size() + jump_reference_results[i].size();
    _references.reserve(cached_reference_count);

    const auto find_final_entry = [&](std::uintptr_t start) -> const FunctionEntry* {
        const auto entry = std::ranges::lower_bound(_function_entries, start, {}, &FunctionEntry::start);
        if (entry == _function_entries.end() || entry->start != start)
            return nullptr;
        return &*entry;
    };

    const auto append_cached_references = [&](const std::vector<ScannedReference>& cached,
                                              bool                                 is_jump) {
        const FunctionEntry* final_entry{};
        std::uintptr_t       range_start{};

        for (const auto& scanned : cached)
        {
            if (final_entry == nullptr || range_start != scanned.range_start)
            {
                range_start = scanned.range_start;
                final_entry = find_final_entry(range_start);
            }
            if (final_entry == nullptr || scanned.reference.source_ip >= final_entry->end)
                continue;
            if (is_jump
                && (scanned.reference.target == range_start
                    || !is_function_entry(scanned.reference.target)))
                continue;

            _references.push_back(scanned.reference);
        }
    };

    for (std::size_t i = 0; i < max_discovery_threads; ++i)
    {
        append_cached_references(reference_results[i], false);
        append_cached_references(jump_reference_results[i], true);
    }

    std::ranges::sort(_references, [](const ReferenceEntry& lhs, const ReferenceEntry& rhs) {
        return lhs.target < rhs.target
               || (lhs.target == rhs.target && lhs.source_ip < rhs.source_ip);
    });
    const auto [unique_first, unique_last] = std::ranges::unique(
        _references, {}, [](const ReferenceEntry& entry) {
            return std::pair{entry.target, entry.source_ip};
        });
    _references.erase(unique_first, unique_last);

#    ifdef DEBUG
    FLOG("BuildFunctionIndexAndReferences: %zu pdata, %zu supplemental, %zu candidates, "
         "%zu rounds, %zu references",
         authoritative_entries.size(), supplemental_entries.size(), supplemental_starts.size(),
         discovery_rounds, _references.size());
#    endif
}

void CModule::DumpVtables()
{
    // originally inspired by praydog & cursey's kananlib https://github.com/cursey/kananlib/blob/main/src/RTTI.cpp
    // but made some improvements based on our usage.
    // hopefully no one copies or recodes this function in another language and claims they coded it without giving credit 😭🙏

    constexpr auto type_info_type_descriptor_name = ".?AVtype_info@@";

    auto type_descriptor_address = FindString(type_info_type_descriptor_name, false);
    if (!type_descriptor_address.IsValid())
    {
        FERROR("Failed to find type descriptor address for \"%s\" in module %s", type_info_type_descriptor_name, _module_name.c_str());
        return;
    }

    auto type_info = type_descriptor_address.Offset(-0x10).Dereference();

    const auto type_info_xrefs = FindPtrs(type_info);
    _vtables.reserve(type_info_xrefs.size());

    std::vector<uint32_t> valid_type_rvas;
    valid_type_rvas.reserve(type_info_xrefs.size());

    for (auto xref : type_info_xrefs) valid_type_rvas.push_back(static_cast<uint32_t>(xref.GetPtr() - _base_address));

    // sort for binary search
    std::ranges::sort(valid_type_rvas);

    for (const auto& segment : _segments)
    {
        if (segment.flags & (FLAG_X | FLAG_W)) continue;

        auto start_addr = segment.address;
        auto end_addr   = start_addr + segment.size;

        auto is_in_current_segment = [&](uintptr_t ptr) {
            return start_addr <= ptr && ptr < end_addr;
        };

        for (uintptr_t ptr = start_addr; ptr < end_addr - sizeof(void*); ptr += sizeof(void*))
        {
            uintptr_t potential_col_ptr = *reinterpret_cast<uintptr_t*>(ptr);

            // check for alignment, struct _s_RTTICompleteObjectLocator aligns to 4 bytes
            if ((potential_col_ptr & 3) != 0) continue;

            if (!is_in_current_segment(potential_col_ptr)) continue;

            auto col = reinterpret_cast<_s_RTTICompleteObjectLocator*>(potential_col_ptr);

            // 0 --> RTTI Class Hierarchy Descriptor
            // 1 --> RTTI Complete Object Locator
            if (col->signature != 1) continue;

            if (std::ranges::binary_search(valid_type_rvas, col->pTypeDescriptor))
            {
                uintptr_t vtable_start = ptr + sizeof(void*);
                auto      ti           = reinterpret_cast<std::type_info*>(_base_address + col->pTypeDescriptor);

                auto node = std::make_unique<VTable>(ti, vtable_start, ti->name(), col->offset, col);

                _vtables.push_back(std::move(node));
            }
        }
    }

    for (const auto& vtable : _vtables)
    {
        auto locator = vtable->object_locator;

        auto hierarchy_descriptor = reinterpret_cast<_s_RTTIClassHierarchyDescriptor*>(_base_address + locator->pClassDescriptor);
        auto base_class_array     = reinterpret_cast<int32_t*>(_base_address + hierarchy_descriptor->pBaseClassArray);

        // starts at 1 to skip the class itself
        for (uint32_t i = 1; i < hierarchy_descriptor->numBaseClasses; i++)
        {
            auto base_class_descriptor = reinterpret_cast<_s_RTTIBaseClassDescriptor*>(_base_address + base_class_array[i]);
            auto base_class_ti         = reinterpret_cast<std::type_info*>(_base_address + base_class_descriptor->pTypeDescriptor);

            vtable->base_classes.emplace_back(base_class_ti->name());
        }
    }

#    ifdef DEBUG
    if (_module_name.find("server") != std::string::npos)
    {
        for (const auto& vtable : _vtables)
        {
            if (vtable->demangled_name.find("CWeapon") == std::string::npos) continue;
            printf("Vtable for %s (offset: 0x%llx)\n", vtable->demangled_name.c_str(), vtable->offset);
            for (const auto& base_class : vtable->base_classes)
            {
                printf("    %s\n", base_class.c_str());
            }
        }
    }
#    endif
}

CAddress CModule::GetExportByName(std::string_view proc_name) const
{
    const std::string raw_name(proc_name);
    if (const auto address = GetProcAddress(reinterpret_cast<HMODULE>(_base_address), raw_name.c_str()))
        return address;
    return FindExportByDemangledName(proc_name);
}

#endif
