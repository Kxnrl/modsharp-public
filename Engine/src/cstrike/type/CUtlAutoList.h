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

#ifndef CSTRIKE_TYPE_CUTLAUTOLIST_H
#define CSTRIKE_TYPE_CUTLAUTOLIST_H

#include <cstdint>

// Source 2 intrusive doubly-linked list node, embedded inside T at a fixed offset from its base.
// For CCSBaseScript the node sits at +0x08:
//   +0x00  void*  vtable (T's own first vtable, e.g. IEntityListener)
//   +0x08  void*  vtable (CUtlAutoList)    <-- node starts here
//   +0x10  T*     m_pNext                  (points to next T*, not to the node)
//   +0x18  T*     m_pPrev
// next/prev store the T* base address (not the node address).
template <typename T>
struct CUtlAutoListNode
{
    void* m_pVTable; // CUtlAutoList<T> vtable
    T*    m_pNext;   // next T* (toward head / older entries)
    T*    m_pPrev;   // prev T* (toward tail / newer entries)
};

#endif
