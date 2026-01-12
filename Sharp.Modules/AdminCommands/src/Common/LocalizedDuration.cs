using System.Globalization;
using Sharp.Modules.LocalizerManager.Shared;

namespace Sharp.Modules.AdminCommands.Common;

internal readonly struct LocalizedDuration : IFormattable
{
    private readonly TimeSpan?          _duration;
    private readonly ILocalizerManager? _localizer;

    public LocalizedDuration(TimeSpan? duration, ILocalizerManager? localizer)
    {
        _duration  = duration;
        _localizer = localizer;
    }

    public override string ToString()
        => ToString(null, CultureInfo.CurrentCulture);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var culture = formatProvider as CultureInfo ?? CultureInfo.CurrentCulture;

        if (!_duration.HasValue)
        {
            return _localizer?.Format(culture, "Admin.Duration.Permanent")
                   ?? "permanently";
        }

        var duration = _duration.Value;

        if (duration.TotalDays >= 1)
        {
            var days = (int) duration.TotalDays;

            return _localizer?.Format(culture, "Admin.Duration.Days", days)
                   ?? $"for {days} day(s)";
        }

        if (duration.TotalHours >= 1)
        {
            var hours = (int) duration.TotalHours;

            return _localizer?.Format(culture, "Admin.Duration.Hours", hours)
                   ?? $"for {hours} hour(s)";
        }

        var minutes = Math.Max((int) duration.TotalMinutes, 1);

        return _localizer?.Format(culture, "Admin.Duration.Minutes", minutes)
               ?? $"for {minutes} minute(s)";
    }
}
