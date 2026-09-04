/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 */

#ifndef MS_ROOT_SYMBOL_NAME_H
#define MS_ROOT_SYMBOL_NAME_H

#include <string>
#include <string_view>

namespace symbol_name
{
// Produces a stable C++-style spelling suitable for cross-ABI lookup.
[[nodiscard]] std::string NormalizeFunctionSignature(std::string_view signature);

// Extracts the scoped function name from a normalized or demangled signature.
[[nodiscard]] std::string_view QualifiedFunctionName(std::string_view signature);
} // namespace symbol_name

#endif
