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

using System;
using System.Collections.Generic;
using System.Linq;
using Sharp.Modules.ClientPreferences.Core.Models;
using Sharp.Modules.ClientPreferences.Shared;

namespace Sharp.Modules.ClientPreferences.Core;

internal class CookieBucket
{
    private Dictionary<string, CookieItem> Cookies { get; }
    public  bool                           Dirty   { get; private set; }

    public CookieBucket(Dictionary<string, CookieItem> cookies)
    {
        Cookies = cookies;
        Dirty   = false;
    }

    public bool Delete(string key)
    {
        if (!Cookies.Remove(key))
        {
            return false;
        }

        Dirty = true;

        return true;
    }

    public CookieItem? Get(string key)
        => Cookies.GetValueOrDefault(key);

    public void Set(string key, CookieItem item)
    {
        Cookies[key] = item;
        Dirty        = true;
    }

    public CookieModel[] GetModels()
        =>
        [
            .. Cookies.Select(x => x.Value.Type switch
            {
                CookieValueType.Number => new CookieModel
                {
                    Key = x.Key, Type = x.Value.Type, Number = x.Value.GetNumber(),
                },
                CookieValueType.Double => new CookieModel
                {
                    Key = x.Key, Type = x.Value.Type, Double = x.Value.GetDouble(),
                },
                CookieValueType.String => new CookieModel
                {
                    Key = x.Key, Type = x.Value.Type, String = x.Value.GetString(),
                },
                _ => throw new TypeAccessException($"Invalid cookie type {x.Value.Type}"),
            }),
        ];
}
