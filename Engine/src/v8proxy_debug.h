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

#ifndef MS_ROOT_V8PROXY_DEBUG_H
#define MS_ROOT_V8PROXY_DEBUG_H

#include <v8.h>

#include <memory>

// taken from https://github.com/v8/v8/blob/11.9.169.6/src/debug/debug-interface.h
namespace v8
{
namespace debug
{
class Location
{
public:
    Location(int lineNumber, int columnNumber);
    Location();
    int  GetLineNumber();
    int  GetColumnNumber();
    bool IsEmpty() const;

private:
    int  line_number_;
    int  column_number_;
    bool is_empty_;
};

class ScopeIterator
{
public:
#ifdef PLATFORM_WINDOWS
    static std::unique_ptr<ScopeIterator> CreateForFunction(v8::Isolate* isolate, v8::Local<v8::Function> func);
    static std::unique_ptr<ScopeIterator> CreateForGeneratorObject(v8::Isolate* isolate, v8::Local<v8::Object> generator);
#else
    static ScopeIterator* CreateForFunction(v8::Isolate* isolate, v8::Local<v8::Function> func);
    static ScopeIterator* CreateForGeneratorObject(v8::Isolate* isolate, v8::Local<v8::Object> generator);
#endif

    ScopeIterator()                                = default;
    virtual ~ScopeIterator()                       = default;
    ScopeIterator(const ScopeIterator&)            = delete;
    ScopeIterator& operator=(const ScopeIterator&) = delete;

    enum ScopeType
    {
        ScopeTypeGlobal = 0,
        ScopeTypeLocal,
        ScopeTypeWith,
        ScopeTypeClosure,
        ScopeTypeCatch,
        ScopeTypeBlock,
        ScopeTypeScript,
        ScopeTypeEval,
        ScopeTypeModule,
        ScopeTypeWasmExpressionStack
    };

    virtual bool                  Done()                = 0;
    virtual void                  Advance()             = 0;
    virtual ScopeType             GetType()             = 0;
    virtual v8::Local<v8::Object> GetScopeObject()      = 0;
    virtual v8::Local<v8::Value>  GetFunctionDebugName() = 0;
    virtual int                   GetScriptId()         = 0;
    virtual bool                  HasLocationInfo()     = 0;
    virtual Location              GetStartLocation()    = 0;
    virtual Location              GetEndLocation()      = 0;
    virtual bool                  SetVariableValue(v8::Local<v8::String> name, v8::Local<v8::Value> value) = 0;
};
} // namespace debug
} // namespace v8

#endif
