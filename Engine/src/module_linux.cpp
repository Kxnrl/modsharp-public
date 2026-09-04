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

#ifdef PLATFORM_POSIX
#define DEBUG
#    include "logging.h"
#    include "module.h"
#    include "scopetimer.h"

#    include <Zydis.h>

#    include <algorithm>
#    include <chrono>
#    include <cstring>
#    include <memory>
#    include <ranges>
#    include <unordered_set>

#    include <cxxabi.h>
#    include <elf.h>
#    include <fcntl.h>
#    include <link.h>
#    include <sys/mman.h>
#    include <sys/stat.h>
#    include <thread>
#    include <unistd.h>

namespace
{
struct AddressRange
{
    std::uintptr_t start;
    std::uintptr_t end;
};

bool IsSupportedElfFile(const std::uint8_t* bytes, std::size_t file_size)
{
    if (bytes == nullptr || file_size < sizeof(Elf64_Ehdr))
        return false;

    const auto* header = reinterpret_cast<const Elf64_Ehdr*>(bytes);
    return std::memcmp(header->e_ident, ELFMAG, SELFMAG) == 0
           && header->e_ident[EI_CLASS] == ELFCLASS64
           && header->e_ident[EI_DATA] == ELFDATA2LSB
           && header->e_shentsize >= sizeof(Elf64_Shdr);
}

bool MatchesLoadedProgramHeaders(const std::uint8_t* bytes, std::size_t file_size, const dl_phdr_info& loaded)
{
    if (!IsSupportedElfFile(bytes, file_size))
        return false;

    const auto* header = reinterpret_cast<const Elf64_Ehdr*>(bytes);
    if (header->e_phnum != loaded.dlpi_phnum || header->e_phentsize < sizeof(Elf64_Phdr)
        || header->e_phoff > file_size)
    {
        return false;
    }

    for (std::size_t i = 0; i < header->e_phnum; ++i)
    {
        if (i > (file_size - header->e_phoff) / header->e_phentsize)
            return false;

        const auto offset = header->e_phoff + i * header->e_phentsize;
        if (offset > file_size || sizeof(Elf64_Phdr) > file_size - offset)
            return false;

        Elf64_Phdr file_header{};
        std::memcpy(&file_header, bytes + offset, sizeof(file_header));
        if (std::memcmp(&file_header, &loaded.dlpi_phdr[i], sizeof(file_header)) != 0)
            return false;
    }

    return true;
}

std::vector<AddressRange> GetElfExecutableRanges(const std::uint8_t* bytes,
                                                 std::size_t         file_size,
                                                 std::uintptr_t      base_address)
{
    std::vector<AddressRange> ranges;
    if (!IsSupportedElfFile(bytes, file_size))
        return ranges;

    const auto* header = reinterpret_cast<const Elf64_Ehdr*>(bytes);
    const auto section_header_fits = [&](std::size_t index) {
        if (header->e_shoff > file_size || index > (file_size - header->e_shoff) / header->e_shentsize)
            return false;

        const auto offset = header->e_shoff + index * header->e_shentsize;
        return offset <= file_size && sizeof(Elf64_Shdr) <= file_size - offset;
    };

    const auto read_section_header = [&](std::size_t index, Elf64_Shdr& section) {
        if (!section_header_fits(index))
            return false;

        std::memcpy(&section, bytes + header->e_shoff + index * header->e_shentsize, sizeof(section));
        return true;
    };

    std::size_t section_count = header->e_shnum;
    if (section_count == 0)
    {
        Elf64_Shdr first_section{};
        if (!read_section_header(0, first_section))
            return ranges;
        section_count = first_section.sh_size;
    }

    if (section_count == 0 || !section_header_fits(section_count - 1))
        return ranges;

    ranges.reserve(section_count);
    for (std::size_t i = 0; i < section_count; ++i)
    {
        Elf64_Shdr section{};
        if (!read_section_header(i, section))
            return {};

        if ((section.sh_flags & (SHF_ALLOC | SHF_EXECINSTR)) != (SHF_ALLOC | SHF_EXECINSTR)
            || section.sh_size == 0)
        {
            continue;
        }

        if (section.sh_addr > std::numeric_limits<std::uintptr_t>::max() - base_address
            || section.sh_size > std::numeric_limits<std::uintptr_t>::max() - base_address - section.sh_addr)
        {
            return {};
        }

        ranges.push_back({base_address + section.sh_addr, base_address + section.sh_addr + section.sh_size});
    }

    std::ranges::sort(ranges, {}, &AddressRange::start);

    std::vector<AddressRange> merged_ranges;
    merged_ranges.reserve(ranges.size());
    for (const auto& range : ranges)
    {
        if (merged_ranges.empty() || merged_ranges.back().end < range.start)
        {
            merged_ranges.push_back(range);
            continue;
        }

        merged_ranges.back().end = std::max(merged_ranges.back().end, range.end);
    }

    return merged_ranges;
}

std::string Demangle(const char* mangled_name)
{
    int    status = -1;
    size_t length = 0;

    std::unique_ptr<char, void (*)(void*)> demangled_ptr(
        abi::__cxa_demangle(mangled_name, nullptr, &length, &status),
        std::free);

    return status == 0 ? std::string(demangled_ptr.get()) : mangled_name;
}

} // namespace

void CModule::GetModuleInfo(std::string_view mod)
{
    std::vector<std::pair<std::string, dl_phdr_info>> module_list;
    dl_iterate_phdr(
            [](struct dl_phdr_info* info, size_t, void* data) {
                std::string name = info->dlpi_name;

                if (name.rfind(".so") == std::string::npos)
                    return 0;

                /*if (name.find("/addons/") != std::string::npos)
                    return 0;*/

                constexpr std::string_view ROOTBIN = "/bin/linuxsteamrt64/";
                constexpr std::string_view GAMEBIN = "/csgo/bin/linuxsteamrt64/";

                bool isFromRootBin = name.find(ROOTBIN) != std::string::npos;
                bool isFromGameBin = name.find(GAMEBIN) != std::string::npos;
                if (!isFromGameBin && !isFromRootBin)
                    return 0;

                auto& modules  = *static_cast<std::vector<std::pair<std::string, dl_phdr_info>>*>(data);
                auto& mod_info = modules.emplace_back();

                mod_info.first  = name;
                mod_info.second = *info;
                return 0;
            },
            &module_list);

    const auto it = std::ranges::find_if(module_list,
                                         [&](const auto& i) {
                                             return i.first.find(mod) != std::string::npos;
                                         });

    if (it == module_list.end())
    {
        return;
    }

    const std::string_view path = it->first;
    const auto             info = it->second;

    this->_base_address = info.dlpi_addr;
    this->_module_name  = path.substr(path.find_last_of('/') + 1);

    std::size_t elf_file_size{};
    const auto  unmap_file = [&elf_file_size](const void* ptr) {
        if (ptr != nullptr)
            munmap(const_cast<void*>(ptr), elf_file_size);
    };
    std::unique_ptr<const void, decltype(unmap_file)> mapped_file(nullptr, unmap_file);

    const std::string path_string(path);
    const int         fd = open(path_string.c_str(), O_RDONLY | O_CLOEXEC);
    if (fd >= 0)
    {
        struct stat file_stat{};
        if (fstat(fd, &file_stat) == 0 && file_stat.st_size >= static_cast<off_t>(sizeof(Elf64_Ehdr)))
        {
            elf_file_size = static_cast<std::size_t>(file_stat.st_size);
            void* mapping = mmap(nullptr, elf_file_size, PROT_READ, MAP_PRIVATE, fd, 0);
            if (mapping != MAP_FAILED)
                mapped_file.reset(mapping);
        }
        close(fd);
    }

    const auto* elf_bytes      = static_cast<const std::uint8_t*>(mapped_file.get());
    const bool  has_file_image = MatchesLoadedProgramHeaders(elf_bytes, elf_file_size, info);
    const auto  executable_ranges = has_file_image
        ? GetElfExecutableRanges(elf_bytes, elf_file_size, _base_address)
        : std::vector<AddressRange>{};

    uintptr_t min_vaddr = std::numeric_limits<uintptr_t>::max();
    uintptr_t max_vaddr = 0;

    for (auto i = 0; i < info.dlpi_phnum; i++)
    {
        const auto& program_header     = info.dlpi_phdr[i];
        auto        address            = _base_address + program_header.p_vaddr;
        auto        size               = static_cast<uintptr_t>(program_header.p_memsz);
        auto        type               = program_header.p_type;
        auto        is_dynamic_section = type == PT_DYNAMIC;

        auto flags = program_header.p_flags;

        auto is_executable = (flags & PF_X) != 0;
        auto is_readable   = (flags & PF_R) != 0;
        auto is_writable   = (flags & PF_W) != 0;

        if (is_dynamic_section)
        {
            DumpExports(reinterpret_cast<void*>(address));
            continue;
        }

        if (type == PT_GNU_EH_FRAME)
        {
            _eh_frame_hdr_addr = address;
            continue;
        }

        if (type != PT_LOAD)
            continue;

        /*if (info.dlpi_phdr[i].p_paddr == 0)
            continue;*/

        min_vaddr = std::min(min_vaddr, address);
        max_vaddr = std::max(max_vaddr, address + size);

        std::uint8_t segment_flags{};
        if (is_readable)
            segment_flags |= FLAG_R;
        if (is_writable)
            segment_flags |= FLAG_W;

        const auto append_segment = [&](std::uintptr_t segment_address, std::size_t segment_size, std::uint8_t flags) {
            if (segment_size == 0)
                return;

            auto& segment   = _segments.emplace_back();
            segment.address = segment_address;
            segment.size    = segment_size;
            segment.flags   = flags;

            // Only executable bytes come from the ELF file. Other segments
            // retain their relocated runtime image.
            if ((flags & FLAG_X) != 0 && has_file_image && segment_address >= address)
            {
                const auto load_offset = segment_address - address;
                const auto initialized_size = load_offset < program_header.p_filesz
                    ? std::min<std::size_t>(segment_size, program_header.p_filesz - load_offset)
                    : 0;
                const auto file_offset = program_header.p_offset + load_offset;

                if (load_offset <= program_header.p_memsz && file_offset <= elf_file_size
                    && initialized_size <= elf_file_size - file_offset)
                {
                    segment.data.resize(segment_size);
                    if (initialized_size != 0)
                        std::memcpy(segment.data.data(), elf_bytes + file_offset, initialized_size);
                    return;
                }
            }

            const auto* data = reinterpret_cast<const std::uint8_t*>(segment_address);
            segment.data.assign(data, data + segment_size);
        };

        bool appended_executable_range = false;
        if (is_executable && !executable_ranges.empty())
        {
            const auto load_end = address + size;
            auto       cursor   = address;

            for (const auto& range : executable_ranges)
            {
                const auto overlap_start = std::max(address, range.start);
                const auto overlap_end   = std::min(load_end, range.end);
                if (overlap_start >= overlap_end)
                    continue;

                append_segment(cursor, overlap_start - cursor, segment_flags);
                append_segment(overlap_start, overlap_end - overlap_start, segment_flags | FLAG_X);
                cursor                    = overlap_end;
                appended_executable_range = true;
            }

            if (appended_executable_range)
                append_segment(cursor, load_end - cursor, segment_flags);
        }

        if (!appended_executable_range)
        {
            const auto fallback_flags = is_executable && executable_ranges.empty() ? segment_flags | FLAG_X : segment_flags;
            append_segment(address, size, fallback_flags);
        }
    }

    _size = max_vaddr - min_vaddr;

    {
        ScopedTimer timer(_module_name + "::DumpVTables");
        DumpVtables();
    }
    {
        ScopedTimer timer(_module_name + "::ParseEhFrameHeader");
        ParseEhFrameHeader();
    }
    {
        ScopedTimer timer(_module_name + "::BuildFunctionIndexAndReferences");
        BuildFunctionIndexAndReferences();
    }
}

void CModule::DumpExports(void* module_base)
{
    auto dyn = (ElfW(Dyn)*)(module_base);
    // thanks to https://stackoverflow.com/a/57099317
    auto GetNumberOfSymbolsFromGnuHash = [](ElfW(Addr) gnuHashAddress) {
        // See https://flapenguin.me/2017/05/10/elf-lookup-dt-gnu-hash/ and
        // https://sourceware.org/ml/binutils/2006-10/msg00377.html
        struct Header
        {
            uint32_t nbuckets;
            uint32_t symoffset;
            uint32_t bloom_size;
            uint32_t bloom_shift;
        };

        auto       header         = (Header*)gnuHashAddress;
        const auto bucketsAddress = gnuHashAddress + sizeof(Header) + (sizeof(std::uintptr_t) * header->bloom_size);

        // Locate the chain that handles the largest index bucket.
        uint32_t lastSymbol    = 0;
        auto     bucketAddress = (uint32_t*)bucketsAddress;
        for (uint32_t i = 0; i < header->nbuckets; ++i)
        {
            uint32_t bucket = *bucketAddress;
            if (lastSymbol < bucket)
            {
                lastSymbol = bucket;
            }
            bucketAddress++;
        }

        if (lastSymbol < header->symoffset)
        {
            return header->symoffset;
        }

        // Walk the bucket's chain to add the chain length to the total.
        const auto chainBaseAddress = bucketsAddress + (sizeof(uint32_t) * header->nbuckets);
        for (;;)
        {
            auto chainEntry = (uint32_t*)(chainBaseAddress + (lastSymbol - header->symoffset) * sizeof(uint32_t));
            lastSymbol++;

            // If the low bit is set, this entry is the end of the chain.
            if (*chainEntry & 1)
            {
                break;
            }
        }

        return lastSymbol;
    };

    ElfW(Sym) * symbols{};
    ElfW(Word) * hash_ptr{};

    char*       string_table{};
    std::size_t symbol_count{};

    while (dyn->d_tag != DT_NULL)
    {
        if (dyn->d_tag == DT_HASH)
        {
            hash_ptr     = reinterpret_cast<ElfW(Word)*>(dyn->d_un.d_ptr);
            symbol_count = hash_ptr[1];
        }
        else if (dyn->d_tag == DT_STRTAB)
        {
            string_table = reinterpret_cast<char*>(dyn->d_un.d_ptr);
        }
        else if (!symbol_count && dyn->d_tag == DT_GNU_HASH)
        {
            symbol_count = GetNumberOfSymbolsFromGnuHash(dyn->d_un.d_ptr);
        }
        else if (dyn->d_tag == DT_SYMTAB)
        {
            symbols = reinterpret_cast<ElfW(Sym)*>(dyn->d_un.d_ptr);
        }
        dyn++;
    }

    for (auto i = 0; i < symbol_count; i++)
    {
        if (!symbols[i].st_name)
        {
            continue;
        }

        if (symbols[i].st_other != 0)
        {
            continue;
        }

        auto             address = symbols[i].st_value + _base_address;
        std::string_view name    = &string_table[symbols[i].st_name];

        _exports[name.data()] = address;

        if (symbols[i].st_shndx == SHN_UNDEF || ELF64_ST_TYPE(symbols[i].st_info) != STT_FUNC)
            continue;

        const auto demangled_name = Demangle(name.data());
        AddExportSymbol(name, demangled_name, address);
    }

    if (auto it = _exports.find("CreateInterface"); it != _exports.end()) [[unlikely]]
        _createInterFaceFn = reinterpret_cast<void*>(it->second);
}

CAddress CModule::GetExportByName(std::string_view proc_name) const
{
    if (auto it = _exports.find(std::string(proc_name)); it != _exports.end())
        return it->second;
    return FindExportByDemangledName(proc_name);
}

static CAddress engine2_class_typeinfo_vtable;
static CAddress engine2_si_class_typeinfo_vtable;
static CAddress engine2_vmi_class_typeinfo_vtable;

void CModule::DumpVtables()
{
    auto get_vtable = [this](const char* name) -> CAddress {
        CAddress symbol_address = GetExportByName(name);
        if (symbol_address.IsValid())
        {
            return symbol_address.Offset(0x10);
        }
        return {};
    };

    CAddress class_typeinfo;
    CAddress si_class_typeinfo;
    CAddress vmi_class_typeinfo;

    const auto is_engine2 = _module_name.find("engine2") != std::string::npos;
    const auto is_tier0   = _module_name.find("tier0") != std::string::npos;

    if (is_engine2 || is_tier0)
    {
        class_typeinfo     = get_vtable("_ZTVN10__cxxabiv117__class_type_infoE");
        si_class_typeinfo  = get_vtable("_ZTVN10__cxxabiv120__si_class_type_infoE");
        vmi_class_typeinfo = get_vtable("_ZTVN10__cxxabiv121__vmi_class_type_infoE");

        // the game loads modules (e.g., libserver.so) with RTLD_LOCAL visibility,
        // which makes each loaded module have their own copies of C++ rtti vtables (e.g., _ZTVN10__cxxabiv117__class_type_infoE)
        // causes finding std::type_info to fail due to different vtable addresses across modules.
        //
        // for example, the std::type_info for a class (e.g., CCSPlayerPawn) has its vtable pointer pointing to the rtti vtable in
        // libengine2.so, not libserver.so. so if we compare against the rtti vtable address (e.g., _ZTVN10__cxxabiv117__class_type_infoE)
        // from libserver.so, it will fail directly because the_rtti_vtable_address_typeinfo_points_to != server_rtti_vtable_address
        //
        // or a more direct example:
        //   libengine2.so: _ZTVN10__cxxabiv117__class_type_infoE @ 0x7f1234567890
        //   libserver.so:  _ZTVN10__cxxabiv117__class_type_infoE @ 0x7f9876543210
        //   CCSPlayerPawn's type_info vtable points to 0x7f1234567890 (engine2's copy)
        //   if we check: (vtable_ptr == 0x7f9876543210) -> false, type check fails!
        //
        // to fix this issue, we only need to find the addresses of rtti vtables in engine2 and cache them for use with other modules
        //
        // note: tier0 is loaded before engine2, so for tier0 we simply just get the addresses
        if (is_engine2)
        {
            engine2_class_typeinfo_vtable     = class_typeinfo;
            engine2_si_class_typeinfo_vtable  = si_class_typeinfo;
            engine2_vmi_class_typeinfo_vtable = vmi_class_typeinfo;
        }
    }
    else
    {
        class_typeinfo     = engine2_class_typeinfo_vtable;
        si_class_typeinfo  = engine2_si_class_typeinfo_vtable;
        vmi_class_typeinfo = engine2_vmi_class_typeinfo_vtable;
    }

    if (!class_typeinfo.IsValid() || !si_class_typeinfo.IsValid() || !vmi_class_typeinfo.IsValid()) [[unlikely]]
    {
        FatalError("Failed to get typeinfo vtables");
        return;
    }

    struct TypeInfo
    {
        std::type_info*         ti;
        [[nodiscard]] uintptr_t address() const { return reinterpret_cast<uintptr_t>(ti); }
    };

    std::vector<TypeInfo> known_typeinfos;

    // originally inspired by praydog & cursey's kananlib https://github.com/cursey/kananlib/blob/main/src/RTTI.cpp
    // but made some improvements based on our usage.
    // hopefully no one copies or recodes this function in another language and claims they coded it without giving credit 😭🙏

    // find every address that points to the typeinfo vtable, used for brutefocing vtable later
    auto collect_typeinfos = [&](CAddress root_rtti_vtable) {
        auto instances = FindPtrs(root_rtti_vtable.GetPtr());
        known_typeinfos.reserve(known_typeinfos.size() + instances.size());

        for (auto xref : instances)
            known_typeinfos.emplace_back(xref.As<std::type_info*>());
    };

    collect_typeinfos(vmi_class_typeinfo);
    collect_typeinfos(si_class_typeinfo);
    collect_typeinfos(class_typeinfo);
    std::ranges::sort(known_typeinfos, {}, &TypeInfo::address);

    const auto min_ti_addr = known_typeinfos.front().address();
    const auto max_ti_addr = known_typeinfos.back().address();

    _vtables.reserve(known_typeinfos.size());

    // bruteforcing vtable
    for (const auto& segment : _segments)
    {
        if (segment.flags & FLAG_X)
            continue;

        auto scan_start = segment.address;
        auto scan_end   = scan_start + segment.size;

        for (auto current_addr = scan_start; current_addr < scan_end; current_addr += sizeof(void*))
        {
            auto ptr = *reinterpret_cast<uintptr_t*>(current_addr);

            if (ptr < min_ti_addr || ptr > max_ti_addr)
                continue;

            auto it = std::ranges::lower_bound(known_typeinfos, ptr, {}, &TypeInfo::address);

            if (it == known_typeinfos.end() || it->address() != ptr)
                continue;
            auto offset = *(std::intptr_t*)(current_addr - 0x8);
            // offset_to_top: 0 for primary vtable, negative for secondary vtables
            if (offset > 0)
                continue;

            // make it positive to behave the same as windows
            offset = -offset;

            const auto& [type_info] = *it;

            auto start_address = current_addr + 0x8;

            auto vtable = std::make_unique<VTable>(type_info, start_address, Demangle(type_info->name()), offset);

            _vtables.push_back(std::move(vtable));
        }
    }

    std::vector<const std::type_info*>        worklist;
    std::unordered_set<const std::type_info*> visited;

    for (const auto& vtable_ptr : _vtables)
    {
        VTable* start_node = vtable_ptr.get();

        worklist.clear();
        worklist.push_back(start_node->type_info);

        visited.clear();
        visited.insert(start_node->type_info);

        while (!worklist.empty())
        {
            const std::type_info* current_ti = worklist.back();
            worklist.pop_back();

            uintptr_t ti_vtable_ptr = *reinterpret_cast<const uintptr_t*>(current_ti);

            auto process_base = [&](const std::type_info* base_ti) {
                if (!visited.contains(base_ti))
                {
                    visited.insert(base_ti);
                    start_node->base_classes.emplace_back(Demangle(base_ti->name()));

                    // Type info is sufficient to continue walking even when
                    // the base class's vtable lives in another module.
                    worklist.push_back(base_ti);
                }
            };

            if (ti_vtable_ptr == si_class_typeinfo.GetPtr())
            {
                auto* si_type_info = static_cast<const __cxxabiv1::__si_class_type_info*>(current_ti);
                process_base(si_type_info->__base_type);
            }
            else if (ti_vtable_ptr == vmi_class_typeinfo.GetPtr())
            {
                auto* vmi_type_info = static_cast<const __cxxabiv1::__vmi_class_type_info*>(current_ti);
                for (auto i = 0u; i < vmi_type_info->__base_count; ++i)
                {
                    const auto& base_info = vmi_type_info->__base_info[i];
                    process_base(base_info.__base_type);
                }
            }
        }
    }
#    ifdef DEBUG
    if (_module_name.find("server") != std::string::npos)
    {
        for (const auto& vtable : _vtables)
        {
            if (vtable->demangled_name.find("CWeapon") == std::string::npos || vtable->offset != 0)
                continue;
            printf("Vtable for %s (offset: 0x%llx)\n", vtable->demangled_name.c_str(), vtable->offset);
            for (const auto& base_class : vtable->base_classes)
            {
                printf("    %s\n", base_class.c_str());
            }
        }
    }
#    endif
}

void CModule::ParseEhFrameHeader()
{
    if (_eh_frame_hdr_addr == 0)
        return;

    const auto* hdr              = reinterpret_cast<const std::uint8_t*>(_eh_frame_hdr_addr);
    const auto  version          = hdr[0];
    const auto  eh_frame_ptr_enc = hdr[1];
    const auto  fde_count_enc    = hdr[2];
    const auto  table_enc        = hdr[3];

    constexpr std::uint8_t DW_EH_PE_pcrel      = 0x10;
    constexpr std::uint8_t DW_EH_PE_datarel    = 0x30;
    constexpr std::uint8_t DW_EH_PE_sdata4     = 0x0B;
    constexpr std::uint8_t DW_EH_PE_udata4     = 0x03;
    constexpr std::uint8_t EXPECTED_EH_PTR_ENC = DW_EH_PE_pcrel | DW_EH_PE_sdata4;   // 0x1B
    constexpr std::uint8_t EXPECTED_TABLE_ENC  = DW_EH_PE_datarel | DW_EH_PE_sdata4; // 0x3B

    if (version != 1 || eh_frame_ptr_enc != EXPECTED_EH_PTR_ENC
        || fde_count_enc != DW_EH_PE_udata4 || table_enc != EXPECTED_TABLE_ENC)
    {
#    ifdef DEBUG
        printf("[%s] ParseEhFrameHeader: unsupported encoding (ver=%u, eh_enc=%#x, cnt_enc=%#x, tbl_enc=%#x)\n",
               _module_name.c_str(), version, eh_frame_ptr_enc, fde_count_enc, table_enc);
#    endif
        return;
    }

    const auto  fde_count = *reinterpret_cast<const std::uint32_t*>(hdr + 8);
    const auto* table     = reinterpret_cast<const std::int32_t*>(hdr + 12);
    const auto  hdr_addr  = _eh_frame_hdr_addr;

    const auto is_executable_address = [&](std::uintptr_t address) {
        return std::ranges::any_of(_segments, [address](const Segment& segment) {
            return (segment.flags & FLAG_X) != 0
                   && segment.address <= address && address < segment.address + segment.size;
        });
    };
    if (std::ranges::none_of(_segments, [](const Segment& segment) { return (segment.flags & FLAG_X) != 0; }))
        return;

    _eh_fde_starts.reserve(fde_count);
    _eh_fde_ends.reserve(fde_count);

    for (std::uint32_t i = 0; i < fde_count; ++i)
    {
        const auto initial_rel = table[i * 2];
        const auto fde_rel     = table[i * 2 + 1];

        const auto pc_begin = static_cast<std::uintptr_t>(
            static_cast<std::int64_t>(hdr_addr) + initial_rel);
        const auto fde_addr = static_cast<std::uintptr_t>(
            static_cast<std::int64_t>(hdr_addr) + fde_rel);

        if (!is_executable_address(pc_begin))
            continue;

        // FDE layout: [length:u32] [cie_ptr:u32] [pc_begin:sdata4] [pc_range:u32] ...
        const auto* fde_ptr    = reinterpret_cast<const std::uint8_t*>(fde_addr);
        const auto  fde_length = *reinterpret_cast<const std::uint32_t*>(fde_ptr);

        // length 0 / 0xFFFFFFFF marks the terminator; cie_ptr == 0 marks a CIE.
        if (fde_length == 0 || fde_length == 0xFFFFFFFFu)
            continue;
        const auto cie_ptr = *reinterpret_cast<const std::uint32_t*>(fde_ptr + 4);
        if (cie_ptr == 0)
            continue;

        const auto pc_begin_rel  = *reinterpret_cast<const std::int32_t*>(fde_ptr + 8);
        const auto pc_range      = *reinterpret_cast<const std::uint32_t*>(fde_ptr + 12);
        const auto pc_begin_self = static_cast<std::uintptr_t>(
            static_cast<std::int64_t>(fde_addr) + 8 + pc_begin_rel);

        // Cross-decoded pc_begin must match the index entry, else our encoding
        // assumption is wrong for this FDE - skip it.
        if (pc_begin_self != pc_begin)
            continue;

        _eh_fde_starts.push_back(pc_begin);
        // Same pc_begin can recur across fragments; keep the largest pc_range.
        auto&      slot     = _eh_fde_ends[pc_begin];
        const auto frag_end = pc_begin + pc_range;
        if (frag_end > slot)
            slot = frag_end;
    }

    std::ranges::sort(_eh_fde_starts);
    const auto [first, last] = std::ranges::unique(_eh_fde_starts);
    _eh_fde_starts.erase(first, last);

#    ifdef DEBUG
    printf("[%s] ParseEhFrameHeader: %u FDEs, %zu unique pc_begin in .text\n",
           _module_name.c_str(), fde_count, _eh_fde_starts.size());
#    endif
}

// CFG-based function-end finder. For each known start we:
//   1. BFS the forward control-flow graph (fallthrough + in-range direct
//      branches), splitting blocks on RET / INT3 / UD2 / any branch.
//   2. Take end = max(block.end). Blocks needn't be contiguous - compilers
//      split hot/cold halves of one function with padding in between.
//   3. Extend past code only reachable via paths the CFG can't follow (switch
//      jump tables, EH cleanup) by decoding forward to the next start (see
//      find_last_real_insn_end).
//   4. Union with the FDE pc_range from .eh_frame_hdr when present - an
//      independent lower bound covering tails the CFG walk misses.
//
// This replaces an older padding-based scheme that broke on GNU-ld binaries:
// their alignment NOPs land mid-function after `jmp loc_FAR`, fooling its
// run-end == 16-aligned filter. Decoding instructions sidesteps that entirely.
// Capped per function so a pathological case can't dominate runtime.
namespace
{

struct CfgBasicBlock
{
    std::uintptr_t start;
    std::uintptr_t end;
};

// Decode one instruction at `ip`, returning length on success or 0 on failure.
// Caller bounds `ip < hard_end` and ensures `ip` is in an executable segment.
[[nodiscard]] std::uint32_t cfg_decode_one(ZydisDecoder*            decoder,
                                           std::uintptr_t           ip,
                                           std::uintptr_t           hard_end,
                                           ZydisDecodedInstruction& out) noexcept
{
    if (ip >= hard_end)
        return 0;
    if (ZYAN_FAILED(ZydisDecoderDecodeInstruction(decoder, nullptr,
                                                  reinterpret_cast<const void*>(ip),
                                                  hard_end - ip, &out)))
        return 0;
    return out.length;
}

[[nodiscard]] std::vector<CfgBasicBlock> cfg_collect_blocks(ZydisDecoder*  decoder,
                                                            std::uintptr_t start,
                                                            std::uintptr_t hard_end)
{
    std::vector<CfgBasicBlock>         blocks;
    std::unordered_set<std::uintptr_t> visited;
    std::vector<std::uintptr_t>        worklist;

    worklist.push_back(start);
    visited.insert(start);

    ZydisDecodedInstruction instr{};

    while (!worklist.empty())
    {
        const auto block_start = worklist.back();
        worklist.pop_back();

        auto ip = block_start;
        while (ip < hard_end)
        {
            const auto len = cfg_decode_one(decoder, ip, hard_end, instr);
            if (len == 0)
                break;

            const auto next_ip = ip + len;

            // terminal instructions (no fallthrough)
            if (instr.meta.category == ZYDIS_CATEGORY_RET
                || instr.mnemonic == ZYDIS_MNEMONIC_INT3
                || instr.mnemonic == ZYDIS_MNEMONIC_UD2)
            {
                blocks.push_back({block_start, next_ip});
                goto next_in_worklist;
            }

            // direct branches: enqueue in-range target; for conditional also enqueue fallthrough
            if ((instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
                && (instr.meta.category == ZYDIS_CATEGORY_UNCOND_BR
                    || instr.meta.category == ZYDIS_CATEGORY_COND_BR))
            {
                const auto target = ip + len + static_cast<std::int64_t>(instr.raw.imm[0].value.s);
                blocks.push_back({block_start, next_ip});

                if (target >= start && target < hard_end && visited.insert(target).second)
                    worklist.push_back(target);
                if (instr.meta.category == ZYDIS_CATEGORY_COND_BR
                    && next_ip < hard_end && visited.insert(next_ip).second)
                    worklist.push_back(next_ip);

                goto next_in_worklist;
            }

            // indirect branches: terminate without successors (jump tables, vtable thunks)
            if (instr.meta.category == ZYDIS_CATEGORY_UNCOND_BR
                || instr.meta.category == ZYDIS_CATEGORY_COND_BR)
            {
                blocks.push_back({block_start, next_ip});
                goto next_in_worklist;
            }

            // calls and everything else: fall through, step over
            ip = next_ip;
        }

        if (ip > block_start)
            blocks.push_back({block_start, ip});

    next_in_worklist:;
    }

    std::ranges::sort(blocks, {}, &CfgBasicBlock::start);
    std::vector<CfgBasicBlock> merged;
    merged.reserve(blocks.size());
    for (const auto& b : blocks)
    {
        if (!merged.empty() && b.start <= merged.back().end)
            merged.back().end = std::max(merged.back().end, b.end);
        else
            merged.push_back(b);
    }
    return merged;
}

// Walk forward through the gap [floor, limit) and return the end of the last
// VERIFIED real-code chain. `floor` is the CFG-proven end, `limit` the next
// known start; `exec_start`/`exec_end` bound .text for the CALL witness.
//
// We decode forward (rather than scan for a known padding byte-set) so the same
// code handles mold's `cc cc ...` and GNU ld's multi-byte NOPs uniformly.
// Because x86 is dense, random bytes often decode as valid instructions, so we
// only move the boundary when a chain is backed by a structural witness:
//
//   1. Control-flow terminator (RET, unconditional JMP, UD2) - end-of-block
//      marker for switch-case fragments and EH cleanup blocks.
//   2. INT3 following a real instruction - the trap mold emits after a
//      noreturn call.
//   3. Direct CALL rel32 (E8) whose target lands in .text - vanishingly
//      unlikely by chance, and the only witness for noreturn-CALL chains
//      (assert/_Unwind_Resume) that have no in-function terminator.
//
// Plain instructions (MOV, LEA, ...) advance the cursor but don't commit on
// their own. NOP and 0x00 reset the chain without committing; a decode failure
// abandons it. Any chain still uncommitted at `limit` is dropped. The FDE
// refinement in the caller is the backstop for the rare case this misses.
[[nodiscard]] std::uintptr_t find_last_real_insn_end(ZydisDecoder*  decoder,
                                                     std::uintptr_t limit,
                                                     std::uintptr_t floor,
                                                     std::uintptr_t exec_start,
                                                     std::uintptr_t exec_end) noexcept
{
    auto                    end_of_real_code = floor;
    auto                    cur              = floor;
    bool                    chain_has_insn   = false;
    ZydisDecodedInstruction instr{};

    while (cur < limit)
    {
        const auto b = *reinterpret_cast<const std::uint8_t*>(cur);

        // 0x00: alignment slack. Resets the chain without committing.
        if (b == 0x00)
        {
            ++cur;
            chain_has_insn = false;
            continue;
        }

        // 0xCC: INT3. Commits only if a real instruction preceded it (trap
        // after a noreturn call); otherwise it's padding fill.
        if (b == 0xCC)
        {
            ++cur;
            if (chain_has_insn)
                end_of_real_code = cur;
            chain_has_insn = false;
            continue;
        }

        // Non-decodable junk: abandon the chain.
        if (ZYAN_FAILED(ZydisDecoderDecodeInstruction(decoder, nullptr,
                                                      reinterpret_cast<const void*>(cur),
                                                      limit - cur, &instr)))
        {
            ++cur;
            chain_has_insn = false;
            continue;
        }

        const auto insn_ip = cur;
        cur += instr.length;

        // Multi-byte NOP: alignment padding, resets the chain without committing.
        if (instr.mnemonic == ZYDIS_MNEMONIC_NOP)
        {
            chain_has_insn = false;
            continue;
        }

        chain_has_insn = true;

        // Control-flow terminator: commit and reset. Conditional branches do
        // NOT terminate here (the block continues at the fall-through).
        if (instr.meta.category == ZYDIS_CATEGORY_RET
            || instr.meta.category == ZYDIS_CATEGORY_UNCOND_BR
            || instr.mnemonic == ZYDIS_MNEMONIC_UD2)
        {
            end_of_real_code = cur;
            chain_has_insn   = false;
            continue;
        }

        // Direct CALL rel32 (E8) targeting .text: commit but keep the chain
        // open, since noreturn-CALL chains can stack several CALLs in a row.
        if (instr.opcode == 0xE8
            && instr.meta.category == ZYDIS_CATEGORY_CALL
            && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE))
        {
            const auto target = insn_ip + instr.length
                                + static_cast<std::int64_t>(instr.raw.imm[0].value.s);
            if (target >= exec_start && target < exec_end)
                end_of_real_code = cur;
        }
    }

    return end_of_real_code;
}

} // namespace

void CModule::BuildFunctionIndexAndReferences()
{
    std::vector<const Segment*> executable_segments;
    std::vector<const Segment*> data_segments;
    executable_segments.reserve(_segments.size());
    data_segments.reserve(_segments.size());

    std::size_t    executable_size = 0;
    std::uintptr_t min_data_addr = std::numeric_limits<std::uintptr_t>::max();
    std::uintptr_t max_data_addr = 0;

    for (const auto& seg : _segments)
    {
        if (seg.flags & FLAG_X)
        {
            executable_segments.push_back(&seg);
            executable_size += seg.size;
        }
        else
        {
            data_segments.push_back(&seg);
            min_data_addr = std::min(min_data_addr, seg.address);
            max_data_addr = std::max(max_data_addr, seg.address + seg.size);
        }
    }

    if (executable_segments.empty())
        return;

    std::ranges::sort(executable_segments, {}, &Segment::address);

    const auto find_executable_segment = [&](std::uintptr_t address) -> const Segment* {
        const auto it = std::ranges::find_if(executable_segments, [address](const Segment* segment) {
            return segment->address <= address && address < segment->address + segment->size;
        });
        return it == executable_segments.end() ? nullptr : *it;
    };

    auto is_function_pointer = [&](std::uintptr_t addr) noexcept {
        return find_executable_segment(addr) != nullptr;
    };

    auto is_data_pointer = [&](std::uintptr_t addr) noexcept {
        if (addr < min_data_addr || addr >= max_data_addr)
            return false;
        return std::ranges::any_of(data_segments, [addr](const Segment* s) noexcept {
            return s->address <= addr && addr < s->address + s->size;
        });
    };

    std::vector<std::uintptr_t> seen_functions;
    seen_functions.reserve(executable_size / 32);

    // phase1: scan data for function ptrs
    for (const Segment* seg : data_segments)
    {
        const auto seg_end = seg->address + seg->size;
        for (auto current_ptr = seg->address; current_ptr + sizeof(void*) <= seg_end; current_ptr += sizeof(void*))
        {
            const auto potential_addr = *reinterpret_cast<std::uintptr_t*>(current_ptr);

            if (is_function_pointer(potential_addr))
                seen_functions.push_back(potential_addr);
        }
    }

    // phase2: single-pass disassembly of .text to find
    // 1. function entries (E8 call targets, E9 tail-call jumps, RIP-relative LEAs)
    // 2. pointer references within .text.
    // Phase 3's CFG walk handles boundaries, so unlike the old scheme we collect no padding here.
    struct ChunkResult
    {
        std::vector<std::uintptr_t> functions;
        std::vector<ReferenceEntry> refs;
        std::vector<ReferenceEntry> jump_refs;
    };

    const auto num_threads = std::max(1u, std::thread::hardware_concurrency());
    const auto chunk_size  = std::max<std::size_t>(1, (executable_size + num_threads - 1) / num_threads);

    struct DecodeChunk
    {
        std::uintptr_t segment_end;
        std::uintptr_t decode_start;
        std::uintptr_t chunk_start;
        std::uintptr_t chunk_end;
    };

    // multithreaded solution inspired by the code snippet @angelfor3v3r gave me a long time ago.
    // to be honest i could have used yaxpeax-x86, which is the fastest decoder i have found yet (it takes about 100ms to decode libserver.so .text section
    // while zydis takes ~450ms), but i dont think it is worth the effort to replace zydis with it,
    // not to mention safetyhook also uses zydis and i use the encoder feature from zydis too.
    // hopefully no one copies or recodes this function in another language and claims they coded it without giving credit 😭🙏

    // Each chunk decodes 24 bytes early to handle boundaries landing in the
    // middle of an instruction, but never crosses an executable range boundary.
    constexpr std::size_t    warmup_bytes = 24;
    std::vector<DecodeChunk> decode_chunks;
    for (const auto* segment : executable_segments)
    {
        const auto segment_start = segment->address;
        const auto segment_end   = segment->address + segment->size;
        for (auto chunk_start = segment_start; chunk_start < segment_end;)
        {
            const auto chunk_end = std::min<std::uintptr_t>(chunk_start + chunk_size, segment_end);
            const auto warmup    = std::min<std::size_t>(warmup_bytes, chunk_start - segment_start);
            decode_chunks.push_back({segment_end, chunk_start - warmup,
                                     chunk_start, chunk_end});
            chunk_start = chunk_end;
        }
    }

    std::vector<ChunkResult> chunk_results(decode_chunks.size());

    auto disassemble_chunk = [&](std::size_t idx, const DecodeChunk& chunk) {
        ZydisDecoder decoder{};
        if (ZYAN_FAILED(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
            return;

        auto& result = chunk_results[idx];
        const auto result_capacity = chunk.chunk_end - chunk.chunk_start;
        result.functions.reserve(result_capacity / 64);
        result.refs.reserve(result_capacity / 8);
        result.jump_refs.reserve(result_capacity / 64);

        ZydisDecodedInstruction instr{};

        ZydisInstructionCategory prev_category{};
        ZydisMnemonic            prev_mnemonic{};
        bool                     has_prev = false;

        for (auto ip = chunk.decode_start; ip < chunk.chunk_end;)
        {
            if (ZYAN_FAILED(ZydisDecoderDecodeInstruction(&decoder, nullptr,
                                                          reinterpret_cast<const void*>(ip), chunk.segment_end - ip, &instr)))
            {
                ip++;
                has_prev = false;
                continue;
            }

            const auto length = instr.length;

            // only record results after warm-up phase
            if (ip >= chunk.chunk_start)
            {
                if (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE)
                {
                    // Direct CALL rel32 (E8). Gate on opcode, not the CALL
                    // category: an indirect `call [rip+disp32]` (FF /2) is also
                    // a relative CALL but its operand is the GOT slot, not the
                    // callee. Those are handled by the disp branch below.
                    if (instr.opcode == 0xE8 && instr.meta.category == ZYDIS_CATEGORY_CALL)
                    {
                        const auto target = ip + length + instr.raw.imm[0].value.s;
                        if (is_function_pointer(target))
                        {
                            result.functions.push_back(target);
                            result.refs.emplace_back(target, ip);
                        }
                    }
                    else if (instr.opcode == 0xE9 && instr.meta.category == ZYDIS_CATEGORY_UNCOND_BR)
                    {
                        const auto target = ip + length + instr.raw.imm[0].value.s;

                        if (is_function_pointer(target))
                        {
                            result.jump_refs.emplace_back(target, ip);

                            // Function entries are 16-byte aligned; this filters out
                            // most jmp-label and nullsub targets.
                            // Treat as a tail call only when preceded by stack
                            // cleanup. Stay conservative to avoid false positives;
                            // FDE seeding recovers anything we skip here.
                            if (prev_category != ZYDIS_CATEGORY_CALL && (target & 15) == 0
                                && has_prev && (prev_category == ZYDIS_CATEGORY_POP || prev_mnemonic == ZYDIS_MNEMONIC_LEAVE))
                            {
                                result.functions.push_back(target);
                            }
                        }
                    }

                    // Additive (not else-if): catches RIP-relative displacements
                    // (LEA, MOV, indirect CALL/JMP, CMP/TEST [rip+disp], ...).
                    // E8/E9 have no ModR/M displacement, so no double-count risk.
                    if (instr.raw.disp.offset != 0)
                    {
                        const auto target = ip + length + instr.raw.disp.value;

                        if (is_data_pointer(target))
                            result.refs.emplace_back(target, ip);
                        else if (is_function_pointer(target))
                            result.functions.push_back(target);
                    }
                }
            }

            prev_category = instr.meta.category;
            prev_mnemonic = instr.mnemonic;
            has_prev      = true;
            ip += length;
        }

        std::ranges::sort(result.functions);
    };

    const auto               worker_count = std::min<std::size_t>(num_threads, decode_chunks.size());
    std::vector<std::thread> threads;
    threads.reserve(worker_count);
    for (std::size_t worker = 0; worker < worker_count; ++worker)
    {
        threads.emplace_back([&, worker] {
            for (std::size_t i = worker; i < decode_chunks.size(); i += worker_count)
                disassemble_chunk(i, decode_chunks[i]);
        });
    }

    // wait for completion
    for (auto& t : threads)
        t.join();

    // merge results from each thread
    std::size_t total_funcs = seen_functions.size();
    std::size_t total_refs  = 0;
    std::size_t total_jumps = 0;

    for (const auto& r : chunk_results)
    {
        total_funcs += r.functions.size();
        total_refs += r.refs.size();
        total_jumps += r.jump_refs.size();
    }

    std::vector<ReferenceEntry> temp_refs;
    std::vector<ReferenceEntry> temp_jump_refs;
    temp_refs.reserve(total_refs);
    temp_jump_refs.reserve(total_jumps);
    seen_functions.reserve(total_funcs + _eh_fde_starts.size());

    for (auto& r : chunk_results)
    {
        // todo: C++23 .append_range(std::views::as_rvalue(r.abc));
        temp_refs.insert(temp_refs.end(), std::move_iterator(r.refs.begin()), std::move_iterator(r.refs.end()));
        temp_jump_refs.insert(temp_jump_refs.end(), std::move_iterator(r.jump_refs.begin()), std::move_iterator(r.jump_refs.end()));
        seen_functions.insert(seen_functions.end(), std::move_iterator(r.functions.begin()), std::move_iterator(r.functions.end()));
    }

    // Seed with FDE pc_begin entries: leaf functions reached only via indirect
    // dispatch or an uncaught tail-call jmp, and cold-split fragments the linker
    // put in their own FDE - all invisible to phases 1/2. ~500 extra per
    // libserver.so in practice.
    seen_functions.insert(seen_functions.end(), _eh_fde_starts.begin(), _eh_fde_starts.end());

    if (seen_functions.empty()) [[unlikely]]
        return;

    // sort merged results
    std::ranges::sort(temp_refs, std::less{}, &ReferenceEntry::source_ip);
    std::ranges::sort(temp_jump_refs, std::less{}, &ReferenceEntry::source_ip);
    std::ranges::sort(seen_functions);

    const auto [first, last] = std::ranges::unique(seen_functions);
    seen_functions.erase(first, last);

    // phase3: build function boundaries by walking the CFG from each start
    // (see the finder comment above). Parallelized over `seen_functions` slices.
    // based on the implementation in kananlib by @praydog
    // https://github.com/cursey/kananlib/blob/49fdf2d3b1db350e370beacb607d5721bc2911ad/src/Scan.cpp#L1478
    constexpr std::size_t kCfgPerFuncCap = 100'000;

    _function_entries.assign(seen_functions.size(), FunctionEntry{0, 0});

    {
        const auto               cfg_chunk = (seen_functions.size() + num_threads - 1) / num_threads;
        std::vector<std::thread> cfg_threads;
        cfg_threads.reserve(num_threads);

        auto cfg_worker = [&](std::size_t lo, std::size_t hi) {
            ZydisDecoder decoder{};
            if (ZYAN_FAILED(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
                return;

            for (std::size_t i = lo; i < hi; ++i)
            {
                const auto  start      = seen_functions[i];
                const auto* executable = find_executable_segment(start);
                if (executable == nullptr)
                    continue;

                const auto executable_start = executable->address;
                const auto executable_end   = executable->address + executable->size;
                const auto next             = (i + 1 < seen_functions.size() && seen_functions[i + 1] < executable_end) ? seen_functions[i + 1] : executable_end;
                // hard_end bounds the CFG walk; we never legitimately cross into
                // the next known function. The cap is a runtime safety net.
                const auto hard_end = std::min<std::uintptr_t>(next, start + kCfgPerFuncCap);
                if (hard_end <= start)
                    continue;

                const auto blocks = cfg_collect_blocks(&decoder, start, hard_end);

                std::uintptr_t end = start;
                if (blocks.empty())
                {
                    // Fallback: single-instruction span (decode failed at start, very rare).
                    ZydisDecodedInstruction first{};
                    const auto              len = cfg_decode_one(&decoder, start, hard_end, first);
                    end                         = start + (len ? len : 1);
                }
                else
                {
                    for (const auto& b : blocks)
                        end = std::max<std::uintptr_t>(end, b.end);
                }

                // Recover code reachable only via indirect dispatch (switch
                // tables, EH cleanup) by scanning the gap to `next`. Skipped
                // when the cap clamped hard_end, since it then lands mid-body
                // rather than in padding.
                if (next == hard_end)
                {
                    const auto extended = find_last_real_insn_end(&decoder, next, end, executable_start, executable_end);
                    if (extended > end)
                        end = extended;
                }

                // FDE refinement: union with the eh_frame_hdr pc_end, an
                // independent lower bound that catches tails the CFG walk and
                // forward scan both miss (cleanup landing pads, EH fragments).
                if (const auto fde_it = _eh_fde_ends.find(start); fde_it != _eh_fde_ends.end())
                {
                    const auto fde_end = std::min(fde_it->second, hard_end);
                    if (fde_end > end)
                        end = fde_end;
                }

                if (end > start)
                    _function_entries[i] = {start, end};
            }
        };

        for (std::uint32_t t = 0; t < num_threads; ++t)
        {
            const auto lo = static_cast<std::size_t>(t) * cfg_chunk;
            if (lo >= seen_functions.size())
                break;
            const auto hi = std::min(lo + cfg_chunk, seen_functions.size());
            cfg_threads.emplace_back(cfg_worker, lo, hi);
        }
        for (auto& th : cfg_threads)
            th.join();
    }

    // Drop empty entries (e.g. a decode failure at a seeded start).
    std::erase_if(_function_entries, [](const FunctionEntry& e) { return e.end <= e.start; });

    if (_function_entries.empty()) [[unlikely]]
        return;

    // phase4: build reference map
    _references.reserve(temp_refs.size() + temp_jump_refs.size());

    auto append_valid_refs = [&](const std::vector<ReferenceEntry>& refs, bool require_cross_function_target) {
        auto       func_it     = _function_entries.begin();
        const auto func_end_it = _function_entries.end();

        for (const auto& ref : refs)
        {
            const auto source_ip = ref.source_ip;

            while (func_it != func_end_it && func_it->end <= source_ip)
                ++func_it;

            if (func_it == func_end_it)
                break;

            if (source_ip < func_it->start)
                continue;

            if (require_cross_function_target
                && (ref.target == func_it->start
                    || !std::ranges::binary_search(_function_entries, ref.target, {}, &FunctionEntry::start)))
                continue;

            _references.push_back(ref);
        }
    };

    append_valid_refs(temp_refs, false);
    append_valid_refs(temp_jump_refs, true);

    std::ranges::sort(_references, std::less{}, &ReferenceEntry::target);
#    ifdef DEBUG
    printf("[%s] BuildFunctionIndexAndReferences: %zu function entries, %zu references\n", _module_name.c_str(), _function_entries.size(), _references.size());
#    endif
}
#endif
