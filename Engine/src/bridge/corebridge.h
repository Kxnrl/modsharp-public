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

#ifndef MS_COREBRIDGE_H
#define MS_COREBRIDGE_H

// 很多不可变的Object可以直接走这个拿

class CoreBridge
{
public:
    class IFileSystem*                    FullFileSystem;
    class ISteamApiProxy*                 SteamApi;
    class CSharpKeyValuesHelper*          KeyValuesHelper;
    class IMemAlloc*                      MemAlloc;
    class CSharpKeyValues3Helper*         KeyValues3Helper;
    class IEngineServer*                  EngineServer;
};

void InitCoreBridge();

#endif