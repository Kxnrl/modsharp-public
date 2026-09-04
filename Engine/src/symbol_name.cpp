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

#include "symbol_name.h"

#include <algorithm>
#include <cctype>
#include <string>
#include <unordered_set>
#include <utility>
#include <vector>

namespace
{
using Tokens = std::vector<std::string>;

bool IsIdentifierCharacter(char value)
{
    const auto character = static_cast<unsigned char>(value);
    return std::isalnum(character) != 0 || value == '_' || value == '$' || value == '`';
}

bool IsWord(std::string_view token)
{
    return !token.empty() && IsIdentifierCharacter(token.front());
}

Tokens Tokenize(std::string_view input)
{
    Tokens tokens;

    for (std::size_t i = 0; i < input.size();)
    {
        const auto character = static_cast<unsigned char>(input[i]);
        if (std::isspace(character) != 0)
        {
            ++i;
            continue;
        }

        if (IsIdentifierCharacter(input[i]))
        {
            const auto start = i++;
            while (i < input.size() && IsIdentifierCharacter(input[i]))
                ++i;
            tokens.emplace_back(input.substr(start, i - start));
            continue;
        }

        if (input.substr(i).starts_with("::") || input.substr(i).starts_with("&&")
            || input.substr(i).starts_with("->"))
        {
            tokens.emplace_back(input.substr(i, 2));
            i += 2;
            continue;
        }

        if (input.substr(i).starts_with("..."))
        {
            tokens.emplace_back("...");
            i += 3;
            continue;
        }

        tokens.emplace_back(1, input[i++]);
    }

    return tokens;
}

void RemoveIgnoredTokens(Tokens& tokens)
{
    static const std::unordered_set<std::string_view> ignored{
        "class",       "struct",      "enum",        "union",       "__cdecl",
        "__stdcall",   "__thiscall",  "__fastcall",  "__vectorcall", "__clrcall",
        "__ptr64",     "__unaligned", "__restrict",  "__restrict__", "__w64",
        "public",      "protected",   "private",      "static",       "virtual",
        "explicit",    "friend",      "inline",       "constexpr",    "consteval",
    };

    std::erase_if(tokens, [](const std::string& token) {
        return ignored.contains(token);
    });

    // DbgHelp emits access labels as `public:` before member signatures. The
    // label word is removed above, leaving only its colon.
    if (!tokens.empty() && tokens.front() == ":")
        tokens.erase(tokens.begin());
}

void NormalizeMicrosoftIntegers(Tokens& tokens)
{
    for (std::size_t i = 0; i < tokens.size(); ++i)
    {
        if (tokens[i] == "__int64")
        {
            tokens[i] = "long";
            tokens.insert(tokens.begin() + static_cast<std::ptrdiff_t>(i + 1), "long");
            ++i;
        }
        else if (tokens[i] == "__int32")
        {
            tokens[i] = "int";
        }
        else if (tokens[i] == "__int16")
        {
            tokens[i] = "short";
        }
        else if (tokens[i] == "__int8")
        {
            tokens[i] = "char";
            if (i == 0 || (tokens[i - 1] != "signed" && tokens[i - 1] != "unsigned"))
            {
                tokens.insert(tokens.begin() + static_cast<std::ptrdiff_t>(i), "signed");
                ++i;
            }
        }
    }
}

void RemoveInlineStdNamespaces(Tokens& tokens)
{
    for (std::size_t i = 0; i + 3 < tokens.size();)
    {
        const bool inline_namespace = tokens[i] == "std" && tokens[i + 1] == "::"
                                      && (tokens[i + 2] == "__cxx11" || tokens[i + 2] == "__1")
                                      && tokens[i + 3] == "::";
        if (!inline_namespace)
        {
            ++i;
            continue;
        }

        tokens.erase(tokens.begin() + static_cast<std::ptrdiff_t>(i + 1),
                     tokens.begin() + static_cast<std::ptrdiff_t>(i + 3));
    }
}

bool IsTypePrimaryEnd(std::string_view token)
{
    return IsWord(token) || token == ">" || token == "]";
}

std::size_t FindTypePrimaryStart(const Tokens& tokens, std::size_t end)
{
    std::size_t start          = end;
    int         template_depth = 0;

    while (start != 0)
    {
        const auto& previous = tokens[start - 1];

        if (previous == ">")
        {
            ++template_depth;
            --start;
            continue;
        }
        if (previous == "<" && template_depth != 0)
        {
            --template_depth;
            --start;
            continue;
        }
        if (template_depth != 0)
        {
            --start;
            continue;
        }

        if (previous == "," || previous == "(" || previous == "[" || previous == "*"
            || previous == "&" || previous == "&&" || previous == "=")
        {
            break;
        }

        --start;
    }

    return start;
}

void NormalizeCvQualifiers(Tokens& tokens)
{
    for (std::size_t i = 1; i < tokens.size(); ++i)
    {
        if ((tokens[i] != "const" && tokens[i] != "volatile") || !IsTypePrimaryEnd(tokens[i - 1]))
            continue;

        const auto start     = FindTypePrimaryStart(tokens, i);
        auto       qualifier = std::move(tokens[i]);
        tokens.erase(tokens.begin() + static_cast<std::ptrdiff_t>(i));

        auto insertion = start;
        if (qualifier == "volatile" && insertion < tokens.size() && tokens[insertion] == "const")
            ++insertion;
        tokens.insert(tokens.begin() + static_cast<std::ptrdiff_t>(insertion), std::move(qualifier));
    }

    for (std::size_t i = 1; i < tokens.size(); ++i)
    {
        if (tokens[i - 1] == "volatile" && tokens[i] == "const")
            std::swap(tokens[i - 1], tokens[i]);
    }
}

void NormalizeEmptyParameterLists(Tokens& tokens)
{
    for (std::size_t i = 0; i + 2 < tokens.size(); ++i)
    {
        if (tokens[i] == "(" && tokens[i + 1] == "void" && tokens[i + 2] == ")")
            tokens.erase(tokens.begin() + static_cast<std::ptrdiff_t>(i + 1));
    }
}

std::string Serialize(const Tokens& tokens)
{
    std::string result;

    for (std::size_t i = 0; i < tokens.size(); ++i)
    {
        const auto& token = tokens[i];
        if (token == ",")
        {
            result += ", ";
            continue;
        }

        const bool needs_space = !result.empty() && IsWord(token)
                                 && (IsWord(tokens[i - 1]) || tokens[i - 1] == ">" || tokens[i - 1] == ")"
                                     || tokens[i - 1] == "]"
                                     || ((token == "const" || token == "volatile")
                                         && (tokens[i - 1] == "*" || tokens[i - 1] == "&"
                                             || tokens[i - 1] == "&&")));
        if (needs_space)
            result.push_back(' ');
        result += token;
    }

    return result;
}
} // namespace

namespace symbol_name
{
std::string NormalizeFunctionSignature(std::string_view signature)
{
    auto tokens = Tokenize(signature);
    RemoveIgnoredTokens(tokens);
    NormalizeMicrosoftIntegers(tokens);
    RemoveInlineStdNamespaces(tokens);
    NormalizeCvQualifiers(tokens);
    NormalizeEmptyParameterLists(tokens);
    return Serialize(tokens);
}

std::string_view QualifiedFunctionName(std::string_view signature)
{
    std::size_t parenthesis_depth = 0;
    bool        found_closing     = false;

    for (std::size_t i = signature.size(); i != 0; --i)
    {
        switch (signature[i - 1])
        {
            case ')':
                ++parenthesis_depth;
                found_closing = true;
                break;
            case '(':
                if (parenthesis_depth != 0 && --parenthesis_depth == 0)
                    return signature.substr(0, i - 1);
                break;
        }
    }

    return found_closing ? std::string_view{} : signature;
}
} // namespace symbol_name
