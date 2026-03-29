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

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sharp.Generator.Sdk.Utils;

public static class NamedTypeSymbolExtensions
{
    public static INamedTypeSymbol? GetFinalBaseType(this INamedTypeSymbol symbol)
    {
        var currentSymbol = symbol;
        var baseSymbol    = symbol.BaseType;

        while (baseSymbol is not null && baseSymbol.Name != "Object")
        {
            currentSymbol = baseSymbol;
            baseSymbol    = currentSymbol.BaseType;
        }

        return SymbolEqualityComparer.Default.Equals(currentSymbol, symbol) ? null : currentSymbol;
    }

    public static bool IsAssignableFrom(this INamedTypeSymbol symbol, string name)
    {
        if (symbol.Name == name)
        {
            return true;
        }

        if (symbol.AllInterfaces.Any(x => x.Name == name))
        {
            return true;
        }

        var baseType = symbol.BaseType;

        while (baseType is not null)
        {
            if (baseType.Name == name)
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
}

public static class SyntaxNodeExtensions
{
    public static string GetNameSpace(this SyntaxNode syntax)
        => syntax.Parent switch
        {
            FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclaration =>
                fileScopedNamespaceDeclaration.Name.ToString(),
            NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax.Name.ToString(),
            null                                                  => string.Empty, // or whatever you want to do
            var parentSyntax                                      => parentSyntax.GetNameSpace(),
        };
}

public class MethodSymbolParam
{
    public string Name           { get; set; } = null!;
    public string Type           { get; set; } = null!;
    public string UnmanagedType  { get; set; } = null!;
    public int    Utf8Size       { get; set; }
    public bool   IsNativeObject { get; set; }
    public bool   IsRef          { get; set; }
}

public class MethodSymbolParamContext
{
    public List<MethodSymbolParam> Params     { get; }
    public string                  ReturnType { get; }

    public MethodSymbolParamContext(List<MethodSymbolParam> @params, string returnType)
    {
        Params     = @params;
        ReturnType = returnType;
    }

    public string GetParametersString(string? prefixParam = null)
    {
        var @params = Params.Select(x => $"{x.Type} {x.Name}").ToList();

        if (prefixParam is not null)
        {
            @params.Insert(0, prefixParam);
        }

        return string.Join(", ", @params);
    }

    public string GetDelegateUnmanagedPointerString(string? prefixParam = null)
    {
        var @params = Params.Select(x => x.UnmanagedType).ToList();

        var @return = ReturnType switch
        {
            // "bool"                => "byte",
            "string" or "string?" => "sbyte*",
            _                     => ReturnType,
        };

        if (@return.StartsWith("System.Span<"))
        {
            @return = @return.Replace("System.Span", "Sharp.Shared.Types.NativeSpan");
        }

        if (prefixParam is not null)
        {
            @params.Insert(0, prefixParam);
        }

        var paramStr = string.Join(", ", @params);

        if (!string.IsNullOrEmpty(paramStr))
        {
            paramStr = $"{paramStr}, ";
        }

        return $"delegate* unmanaged<{paramStr}{@return}>";
    }

    public string GetCallDelegateUnmanagedPointerArgsString(string? prefixParam = null)
    {
        var args = Params.Select(x =>
                         {
                             var arg = x.Type switch
                             {
                                 "string" => $"{x.Name}Ptr",
                                 "string?" => $"{x.Name} is null ? null : {x.Name}Ptr",

                                 // "bool"                  => $"(byte)({x.Name} ? 1 : 0)",
                                 _ when x.IsNativeObject => $"{x.Name}.GetAbsPtr()",
                                 _ when x.IsRef          => $"{x.Name}Ptr",
                                 _                       => x.Name,
                             };

                             return $"{arg}";
                         })
                         .ToList();

        if (prefixParam is not null)
        {
            args.Insert(0, prefixParam);
        }

        var argStr = string.Join(", ", args);

        return $"{argStr}";
    }

    public void BuildCallAndReturnDelegateUnmanagedPointer(CodeWriter builder, string methodName, string? prefixParam = null)
    {
        var returnStr = ReturnType switch
        {
            "void"   => "PLACEHOLDER;",
            "string" => "var __result = new string(PLACEHOLDER);",
            _        => "var __result = PLACEHOLDER;",
        };

        if (ReturnType != "void" && Params.Any(x => x.IsRef || x.Type is "string" or "string?"))
        {
            returnStr = ReturnType switch
            {
                "void"   => "PLACEHOLDER;",
                "string" => "__result = new string(PLACEHOLDER);",
                _        => "__result = PLACEHOLDER;",
            };

            builder.AppendLine($"{ReturnType} __result;");
        }

        var scopes = Params.Where(x => x.IsRef)
                           .Select(x => builder.BeginScope($"fixed ({x.UnmanagedType} {x.Name}Ptr = &{x.Name})"))
                           .ToList();

        scopes.AddRange(Params.Where(x => x.Type is "string" or "string?")
                              .Select(x => builder.BeginScope($"fixed ({x.UnmanagedType} {x.Name}Ptr = {x.Name}Bytes)")));

        builder.AppendLine(returnStr.Replace("PLACEHOLDER",
                                             $"{methodName}({GetCallDelegateUnmanagedPointerArgsString(prefixParam)})"));

        foreach (var disposable in scopes)
        {
            disposable.Dispose();
        }

        builder.AppendLine();

        // cleanupBytes
        foreach (var arg in Params.Where(x => x.Type is "string" or "string?"))
        {
            builder.AppendLine($"if ({arg.Name}Rented is not null) pool.Return({arg.Name}Rented);");
        }

        if (returnStr.Contains("__result"))
        {
            builder.AppendLine();
            builder.AppendLine("return __result;");
        }
    }

    public void BuildUtf8ConvertCode(CodeWriter builder)
    {
        var items = Params.Where(x => x.Type is "string" or "string?").ToList();

        if (items.Any())
        {
            builder.AppendLine("var pool = ArrayPool<byte>.Shared;");
        }

        foreach (var p in items)
        {
            builder.AppendLine($"byte[]? {p.Name}Rented = null;");

            var sizeCalc = p.Utf8Size > 0 
                ? $"{p.Utf8Size} + 1" 
                : $"Encoding.UTF8.GetMaxByteCount({p.Name}.Length) + 1";
            
            if (p.Type is "string?")
            {
                builder.AppendLine($"int {p.Name}Size = {p.Name} is null ? 0 : {sizeCalc};");
            }
            else
            {
                builder.AppendLine($"int {p.Name}Size = {sizeCalc};");
            }

            var allocExpr = $"{p.Name}Size <= 256 ? stackalloc byte[{p.Name}Size] : ({p.Name}Rented = pool.Rent({p.Name}Size))";
            
            if (p.Type is "string?")
            {
                builder.AppendLine($"Span<byte> {p.Name}Bytes = {p.Name} is null ? default : ({allocExpr});");
            }
            else
            {
                builder.AppendLine($"Span<byte> {p.Name}Bytes = {allocExpr};");
            }

            if (p.Type is "string?")
            {
                builder.AppendLine($"if ({p.Name} is not null)");
                builder.AppendLine("{");
            }

            builder.AppendLine($"Utf8.FromUtf16({p.Name}, {p.Name}Bytes, out _, out var {p.Name}BytesWritten);");
            
            builder.AppendLine($"{p.Name}Bytes[{p.Name}BytesWritten] = 0;");

            if (p.Type is "string?")
            {
                builder.AppendLine("}");
            }

            builder.AppendLine();
        }
    }
}

public static class MethodSymbolExtensions
{
    public static MethodSymbolParamContext GetParams(this IMethodSymbol symbol)
    {
        var items = symbol.Parameters.Select(x =>
                          {
                              var type     = x.Type.ToString();
                              var utf8Size = 0;

                              if (type is "string" or "string?")
                              {
                                  foreach (var attributeData in x.GetAttributes())
                                  {
                                      if (attributeData.AttributeClass?.Name == "NativeUtf8StringAttribute")
                                      {
                                          foreach (var arg in attributeData.ConstructorArguments)
                                          {
                                              utf8Size = (int) arg.Value!;
                                          }

                                          foreach (var arg in attributeData.NamedArguments)
                                          {
                                              if (arg.Key == "Size")
                                              {
                                                  utf8Size = (int) arg.Value.Value!;
                                              }
                                          }
                                      }
                                  }
                              }

                              var isNativeObject = x.Type is INamedTypeSymbol namedTypeSymbol
                                                   && namedTypeSymbol.IsAssignableFrom("INativeObject");

                              var isRef = x.RefKind > RefKind.None;

                              var unmanagedType = type switch
                              {
                                  "string" or "string?" => "byte*",

                                  // "bool"                => "byte",
                                  _ when isNativeObject => "nint",
                                  _                     => x.Type.ToString(),
                              };

                              if (unmanagedType.StartsWith("System.Span<"))
                              {
                                  unmanagedType = unmanagedType.Replace("System.Span", "Sharp.Shared.Types.NativeSpan");
                              }

                              if (isRef)
                              {
                                  unmanagedType = $"{unmanagedType}*";
                                  type          = $"{x.RefKind.ToString().ToLower()} {type}";
                              }

                              return new MethodSymbolParam
                              {
                                  Name = x.Name switch
                                  {
                                      "event"  => "@event",
                                      "params" => "@params",
                                      _        => x.Name,
                                  },
                                  Type           = type,
                                  UnmanagedType  = unmanagedType,
                                  Utf8Size       = utf8Size,
                                  IsNativeObject = isNativeObject,
                                  IsRef          = isRef,
                              };
                          })
                          .ToList();

        return new MethodSymbolParamContext(items, symbol.ReturnType.ToString());
    }
}