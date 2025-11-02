using System;

namespace Sharp.Modules.ClientPreferences.Core;

internal class DatabaseUnavailableException : Exception
{
    public DatabaseUnavailableException(string message) : base(message)
    {
    }
}
