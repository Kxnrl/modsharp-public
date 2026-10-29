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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Sharp.Core.Logging;

internal sealed class NonDisposingLogger : ILogger
{
    private readonly ILogger _logger;

    public NonDisposingLogger(ILogger logger)
        => _logger = logger;

    public void Write(LogEvent logEvent)
        => _logger.Write(logEvent);

    public bool IsEnabled(LogEventLevel level)
        => _logger.IsEnabled(level);

    public ILogger ForContext(ILogEventEnricher enricher)
        => Wrap(_logger.ForContext(enricher));

    public ILogger ForContext(IEnumerable<ILogEventEnricher> enrichers)
        => Wrap(_logger.ForContext(enrichers));

    public ILogger ForContext(string propertyName, object? value, bool destructureObjects = false)
        => Wrap(_logger.ForContext(propertyName, value, destructureObjects));

    public ILogger ForContext<TSource>()
        => Wrap(_logger.ForContext<TSource>());

    public ILogger ForContext(Type source)
        => Wrap(_logger.ForContext(source));

    public bool BindMessageTemplate(string                     messageTemplate,
        object?[]?                                             propertyValues,
        [NotNullWhen(true)] out MessageTemplate?               parsedTemplate,
        [NotNullWhen(true)] out IEnumerable<LogEventProperty>? boundProperties)
        => _logger.BindMessageTemplate(messageTemplate, propertyValues, out parsedTemplate, out boundProperties);

    public bool BindProperty(string?              propertyName,
        object?                                   value,
        bool                                      destructureObjects,
        [NotNullWhen(true)] out LogEventProperty? property)
        => _logger.BindProperty(propertyName, value, destructureObjects, out property);

    private ILogger Wrap(ILogger logger)
        => ReferenceEquals(logger, _logger) ? this : logger;
}
