using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services.Internal;

/// <summary>
///     Fast in-memory cache for player punishment states.
///     Provides O(1) lookups for mute/gag/ban checks during voice/chat events.
/// </summary>
internal class AdminOperationCache
{
    private class PunishmentState
    {
        public bool      IsMuted       { get; set; }
        public bool      IsGagged      { get; set; }
        public bool      IsBanned      { get; set; }
        public DateTime? MuteExpiresAt { get; set; }
        public DateTime? GagExpiresAt  { get; set; }
        public DateTime? BanExpiresAt  { get; set; }
    }

    private readonly Lock _sync = new ();

    private readonly PunishmentState?[]             _players = new PunishmentState?[PlayerSlot.MaxPlayerCount];
    private readonly Dictionary<SteamID, DateTime?> _bans    = new ();

    /// <summary>
    ///     Ensures a slot has an initialized state object.
    /// </summary>
    public void EnsureSlot(PlayerSlot slot)
    {
        lock (_sync)
        {
            _players[slot] ??= new PunishmentState();
        }
    }

    /// <summary>
    ///     Sets the punishment state from loaded records.
    /// </summary>
    public void SetState(PlayerSlot slot, SteamID steamId, IReadOnlyList<AdminOperationRecord> punishments)
    {
        lock (_sync)
        {
            var state = _players[slot] ?? new PunishmentState();

            // Reset state
            state.IsMuted       = false;
            state.IsGagged      = false;
            state.IsBanned      = false;
            state.MuteExpiresAt = null;
            state.GagExpiresAt  = null;
            state.BanExpiresAt  = null;

            // Single pass iteration
            for (var i = 0; i < punishments.Count; i++)
            {
                var p = punishments[i];

                if (p.IsExpired)
                {
                    continue;
                }

                switch (p.Type)
                {
                    case AdminOperationType.Mute:
                        state.IsMuted       = true;
                        state.MuteExpiresAt = p.ExpiresAt;
                        break;
                    case AdminOperationType.Gag:
                        state.IsGagged      = true;
                        state.GagExpiresAt  = p.ExpiresAt;
                        break;
                    case AdminOperationType.Ban:
                        state.IsBanned      = true;
                        state.BanExpiresAt  = p.ExpiresAt;
                        break;
                }
            }

            _players[slot] = state;

            // Update steam-level ban cache for early rejection
            if (state.IsBanned)
            {
                _bans[steamId] = state.BanExpiresAt;
            }
            else
            {
                _bans.Remove(steamId);
            }
        }
    }

    /// <summary>
    ///     Clears all punishment state for a slot.
    /// </summary>
    public void Clear(PlayerSlot slot)
    {
        lock (_sync)
        {
            _players[slot] = null;
        }
    }

    /// <summary>
    ///     Checks if a player is muted (voice blocked).
    /// </summary>
    public bool IsMuted(PlayerSlot slot)
    {
        lock (_sync)
        {
            var state = _players[slot];

            if (state is null || !state.IsMuted)
            {
                return false;
            }

            // Check expiration
            if (state.MuteExpiresAt.HasValue && state.MuteExpiresAt.Value < DateTime.UtcNow)
            {
                state.IsMuted       = false;
                state.MuteExpiresAt = null;

                return false;
            }

            return true;
        }
    }

    /// <summary>
    ///     Checks if a SteamID is banned (slot-independent).
    /// </summary>
    public bool IsBanned(SteamID steamId)
    {
        lock (_sync)
        {
            if (!_bans.TryGetValue(steamId, out var expiresAt))
            {
                return false;
            }

            if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
            {
                _bans.Remove(steamId);

                return false;
            }

            return true;
        }
    }

    /// <summary>
    ///     Checks if a player is gagged (chat blocked).
    /// </summary>
    public bool IsGagged(PlayerSlot slot)
    {
        lock (_sync)
        {
            var state = _players[slot];

            if (state is null || !state.IsGagged)
            {
                return false;
            }

            // Check expiration
            if (state.GagExpiresAt.HasValue && state.GagExpiresAt.Value < DateTime.UtcNow)
            {
                state.IsGagged     = false;
                state.GagExpiresAt = null;

                return false;
            }

            return true;
        }
    }

    /// <summary>
    ///     Sets the muted state for a player.
    /// </summary>
    public void SetMuted(PlayerSlot slot, bool muted, DateTime? expiresAt = null)
    {
        lock (_sync)
        {
            var state = _players[slot];

            if (state is null)
            {
                if (!muted)
                {
                    return; // No need to create state just to set false
                }

                state          = new PunishmentState();
                _players[slot] = state;
            }

            state.IsMuted       = muted;
            state.MuteExpiresAt = muted ? expiresAt : null;
        }
    }

    /// <summary>
    ///     Sets the gagged state for a player.
    /// </summary>
    public void SetGagged(PlayerSlot slot, bool gagged, DateTime? expiresAt = null)
    {
        lock (_sync)
        {
            var state = _players[slot];

            if (state is null)
            {
                if (!gagged)
                {
                    return;
                }

                state          = new PunishmentState();
                _players[slot] = state;
            }

            state.IsGagged     = gagged;
            state.GagExpiresAt = gagged ? expiresAt : null;
        }
    }

    /// <summary>
    ///     Sets the banned state for a player.
    /// </summary>
    public void SetBanned(SteamID steamId, bool banned, DateTime? expiresAt = null)
    {
        lock (_sync)
        {
            if (banned)
            {
                _bans[steamId] = expiresAt;
            }
            else
            {
                _bans.Remove(steamId);
            }
        }
    }
}
