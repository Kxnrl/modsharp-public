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

#ifndef CSTRIKE_INTERFACE_SCRIPTVM_H
#define CSTRIKE_INTERFACE_SCRIPTVM_H

class CCSGOVScriptGameSystem;
class HSCRIPT;

struct ScriptVariant_t;

class IScriptVM
{
public:
    virtual ~IScriptVM() = default;

    virtual bool CreateVM(bool bLoadDebugLibrary) = 0;
    virtual void DestroyVM()                      = 0;

private:
    virtual void Method003() = 0;
    virtual void Method004() = 0;
    virtual void Method005() = 0;
    virtual void Method006() = 0;
    virtual void Method007() = 0;
    virtual void Method008() = 0;
    virtual void Method009() = 0;
    virtual void Method010() = 0;

    virtual void Method011() = 0;
    virtual void Method012() = 0;
    virtual void Method013() = 0;
    virtual void Method014() = 0;
    virtual void Method015() = 0;
    virtual void Method016() = 0;
    virtual void Method017() = 0;
    virtual void Method018() = 0;
    virtual void Method019() = 0;
    virtual void Method020() = 0;

    virtual void Method021() = 0;
    virtual void Method022() = 0;
    virtual void Method023() = 0;
    virtual void Method024() = 0;
    virtual void Method025() = 0;
    virtual void Method026() = 0;
    virtual void Method027() = 0;
    virtual void Method028() = 0;
    virtual void Method029() = 0;
    virtual void Method030() = 0;

    virtual void Method031() = 0;

public:
    virtual bool SetValue(HSCRIPT* hScope, const char* pszKey, const char* pszValue)         = 0;
    virtual bool SetValue(HSCRIPT* hScope, const char* pszKey, const ScriptVariant_t& value) = 0;
    virtual bool SetValue(HSCRIPT* hScope, int nIndex, const ScriptVariant_t& value)         = 0;
};

#endif
