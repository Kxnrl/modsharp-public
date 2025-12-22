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
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Sharp.Core.Helpers;

internal interface IProcessLock : IDisposable
{
    bool IsAcquired { get; }
}

internal static class ProcessLock
{
    public static IProcessLock CreateByRaw(string raw)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        return Create(key);
    }

    public static IProcessLock Create(string key)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new WindowsMutexLock(key)
            : new UnixFileLock(key);
    }

    private sealed class WindowsMutexLock : IProcessLock
    {
        private Mutex? _mutex;

        public bool IsAcquired { get; }

        public WindowsMutexLock(string key)
        {
            _mutex = new Mutex(true, $"Global\\modsharp_{key}", out var success);

            IsAcquired = success;
        }

        public void Dispose()
        {
            if (_mutex is null)
            {
                return;
            }

            try
            {
                _mutex.ReleaseMutex();
                _mutex.Close();
            }
            finally
            {
                _mutex = null;
            }
        }
    }

    private sealed class UnixFileLock : IProcessLock
    {
        private FileStream? _lockFile;

        public bool IsAcquired { get; }

        public UnixFileLock(string key)
        {
            var lockPath = Path.Combine(Path.GetTempPath(), $"modsharp_{key}.lock");

            _lockFile = new FileStream(lockPath,
                                       FileMode.OpenOrCreate,
                                       FileAccess.ReadWrite,
                                       FileShare.None,
                                       1,
                                       FileOptions.DeleteOnClose);

            IsAcquired = true;
        }

        public void Dispose()
        {
            try
            {
                _lockFile?.Dispose();
            }
            finally
            {
                _lockFile = null;
            }
        }
    }
}
