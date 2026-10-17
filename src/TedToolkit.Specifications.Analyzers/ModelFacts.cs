// -----------------------------------------------------------------------
// <copyright file="ModelFacts.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TedToolkit.Specifications.Analyzers;

/// <summary>
/// Recognizes framework types and resolves models used by generated member selections.
/// </summary>
internal static class ModelFacts
{
    /// <summary>
    /// Tests a symbol's metadata name and framework namespace.
    /// </summary>
    /// <param name="symbol">The candidate type symbol.</param>
    /// <param name="name">The expected metadata name.</param>
    /// <returns>Whether the symbol identifies the requested framework type.</returns>
    internal static bool IsFramework(INamedTypeSymbol? symbol, string name)
    {
        return symbol?.OriginalDefinition.MetadataName == name
            && symbol.ContainingNamespace.ToDisplayString() == "TedToolkit.Specifications";
    }

    /// <summary>
    /// Removes a nullable value-type wrapper.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The nullable argument or the original type.</returns>
    internal static ITypeSymbol Unwrap(ITypeSymbol type)
    {
        return type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, } nullable
            ? nullable.TypeArguments[0]
            : type;
    }

    /// <summary>
    /// Classifies the scalar operations supported by a selected type.
    /// </summary>
    /// <param name="type">The selected CLR type.</param>
    /// <returns>A text, scalar, or ordered category, or null for unsupported SQL values.</returns>
    internal static string? ValueCategory(ITypeSymbol type)
    {
        type = Unwrap(type);
        if (type.TypeKind == TypeKind.Enum)
        {
            return "Scalar";
        }

        return type.SpecialType switch
        {
            SpecialType.System_String => "String",
            SpecialType.System_Boolean => "Scalar",
            SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double => "Ordered",
            _ when type.ContainingNamespace?.ToDisplayString() == "System" => type.Name switch
            {
                "Guid" => "Scalar",
                "DateOnly" or "TimeOnly" or "DateTime" or "DateTimeOffset" => "Ordered",
                _ => null,
            },
            _ => null,
        };
    }

    /// <summary>
    /// Checks known ordering restrictions while permitting runtime comparison through open CLR types.
    /// </summary>
    /// <param name="type">The selected CLR type.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <returns>Whether ordering is supported or cannot be ruled out statically.</returns>
    internal static bool SupportsOrdering(ITypeSymbol type, Compilation compilation)
    {
        type = Unwrap(type);
        var category = ValueCategory(type);
        if (type is IArrayTypeSymbol || category is "String" or "Scalar")
        {
            return false;
        }

        if (category == "Ordered" || !type.IsSealed)
        {
            return true;
        }

        var comparable = compilation.GetTypeByMetadataName("System.IComparable");
        var generic = compilation.GetTypeByMetadataName("System.IComparable`1");
        return comparable is null || generic is null
            || compilation.ClassifyCommonConversion(type, comparable).IsImplicit
            || compilation.ClassifyCommonConversion(type, generic.Construct(type)).IsImplicit;
    }

    /// <summary>
    /// Enumerates public instance properties, including inherited declarations.
    /// </summary>
    /// <param name="entity">The selected model type.</param>
    /// <returns>Distinct property symbols, starting with the nearest declarations.</returns>
    internal static IEnumerable<IPropertySymbol> Properties(INamedTypeSymbol entity)
    {
        return BaseTypes(entity)
            .Concat(entity.TypeKind == TypeKind.Interface ? entity.AllInterfaces : [])
            .SelectMany(type => type.GetMembers().OfType<IPropertySymbol>())
            .Where(property => !property.IsStatic && property.DeclaredAccessibility == Accessibility.Public);
    }

    /// <summary>
    /// Builds a stable generated identifier from the fully qualified model identity.
    /// </summary>
    /// <param name="entity">The selected model type.</param>
    /// <returns>A model name with a deterministic hash suffix.</returns>
    internal static string GeneratedName(INamedTypeSymbol entity)
    {
        var identity = entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var hash = 14695981039346656037UL;
        foreach (var character in identity)
        {
            hash = unchecked((hash ^ character) * 1099511628211UL);
        }

        return entity.Name + "SelectionExtensions_" + hash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tests whether a closed model can be referenced by generated source.
    /// </summary>
    /// <param name="entity">The selected model type.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <returns>Whether the model is accessible and supports generated member selection.</returns>
    internal static bool IsAccessibleEntity(INamedTypeSymbol entity, Compilation compilation)
    {
        return !entity.IsUnboundGenericType && !entity.IsStatic && !entity.IsRefLikeType && !entity.IsFileLocal
            && !ContainsTypeParameter(entity) && entity.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface
            && compilation.IsSymbolAccessibleWithin(entity, compilation.Assembly);
    }

    /// <summary>
    /// Detects anonymous models that cannot be named in generated extension signatures.
    /// </summary>
    /// <param name="type">The selected model or type argument.</param>
    /// <returns>Whether any part of the type is anonymous.</returns>
    internal static bool ContainsAnonymousType(ITypeSymbol type)
    {
        return (type is IArrayTypeSymbol array && ContainsAnonymousType(array.ElementType))
            || (type is INamedTypeSymbol named && (named.IsAnonymousType || named.TypeArguments.Any(ContainsAnonymousType)
                || (named.ContainingType is { } containing && ContainsAnonymousType(containing))));
    }

    private static bool ContainsTypeParameter(ITypeSymbol type)
    {
        return type is ITypeParameterSymbol
            || (type is IArrayTypeSymbol array && ContainsTypeParameter(array.ElementType))
            || (type is INamedTypeSymbol named && (named.TypeArguments.Any(ContainsTypeParameter)
                || (named.ContainingType is { } containing && ContainsTypeParameter(containing))));
    }

    /// <summary>
    /// Tests whether an existing public instance member prevents extension-property lookup.
    /// </summary>
    /// <param name="name">The model property name.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <returns>Whether the name belongs to the selection API or System.Object.</returns>
    internal static bool IsSelectionMember(string name, Compilation compilation)
    {
        return BaseTypes(compilation.GetTypeByMetadataName("TedToolkit.Specifications.Selection`2"))
            .SelectMany(type => type.GetMembers(name))
            .Any(member => !member.IsStatic && member.DeclaredAccessibility == Accessibility.Public);
    }

    /// <summary>
    /// Finds the typed selection base of a framework selection.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The constructed selection base, or null.</returns>
    internal static INamedTypeSymbol? SelectionType(ITypeSymbol? type)
    {
        return BaseTypes(type).FirstOrDefault(current => IsFramework(current, "Selection`2"));
    }

    /// <summary>
    /// Tests whether a type derives from a framework specification.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>Whether the type represents a specification.</returns>
    internal static bool IsSpecification(ITypeSymbol? type)
    {
        return BaseTypes(type).Any(current => IsFramework(current, "Specification`1"));
    }

    private static IEnumerable<INamedTypeSymbol> BaseTypes(ITypeSymbol? type)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }

    /// <summary>
    /// Gets a selection's current value type.
    /// </summary>
    /// <param name="type">The selection type to inspect.</param>
    /// <returns>The current value type, or null.</returns>
    internal static ITypeSymbol? OperandType(ITypeSymbol? type)
    {
        return SelectionType(type)?.TypeArguments[1];
    }

    /// <summary>
    /// Resolve unresolved generated property chains from the original syntax. Generators never inspect their own output.
    /// </summary>
    /// <param name="model">The original source's semantic model.</param>
    /// <param name="expression">The selection expression or unresolved member chain.</param>
    /// <param name="token">The analysis cancellation token.</param>
    /// <param name="depth">The recursion guard for source inference.</param>
    /// <returns>The selected model or value type, or null when inference cannot resolve it.</returns>
    internal static ITypeSymbol? SelectedType(SemanticModel model, ExpressionSyntax expression, CancellationToken token, int depth = 0)
    {
        if (depth > 128)
        {
            return null;
        }

        token.ThrowIfCancellationRequested();
        var expressionType = model.GetTypeInfo(expression, token).Type;
        var typed = OperandType(expressionType);
        if (typed is { TypeKind: not (TypeKind.Error or TypeKind.TypeParameter), })
        {
            return typed;
        }

        if (expressionType is { TypeKind: not (TypeKind.Error or TypeKind.TypeParameter), } && SelectionType(expressionType) is null)
        {
            return null;
        }

        return expression switch
        {
            ParenthesizedExpressionSyntax wrapped => SelectedType(model, wrapped.Expression, token, depth + 1),
            PostfixUnaryExpressionSyntax suppressed when suppressed.IsKind(SyntaxKind.SuppressNullableWarningExpression)
                => SelectedType(model, suppressed.Operand, token, depth + 1),
            ConditionalExpressionSyntax conditional
                => CommonSelectedType(model, [conditional.WhenTrue, conditional.WhenFalse,], depth, token),
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression)
                => CommonSelectedType(model, [binary.Left, binary.Right,], depth, token),
            SwitchExpressionSyntax selection => CommonSelectedType(model, selection.Arms.Select(arm => arm.Expression), depth, token),
            ConditionalAccessExpressionSyntax conditional => SelectedType(model, conditional.WhenNotNull, token, depth + 1),
            MemberBindingExpressionSyntax binding => MemberType(SelectedType(model,
                binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>()
                    .First(conditional => conditional.WhenNotNull.Span.Contains(binding.Span)).Expression, token, depth + 1),
                binding.Name.Identifier.ValueText),
            MemberAccessExpressionSyntax member => MemberType(SelectedType(model, member.Expression, token, depth + 1),
                member.Name.Identifier.ValueText),
            InvocationExpressionSyntax call => InvocationType(model, call, depth, token),
            IdentifierNameSyntax identifier => LocalType(model, identifier, depth, token),
            _ => null,
        };
    }

    /// <summary>
    /// Resolves the selected model whose generated properties a pattern inspects.
    /// </summary>
    /// <param name="model">The original semantic model.</param>
    /// <param name="pattern">The pattern receiving the selected value.</param>
    /// <param name="token">The analysis cancellation token.</param>
    /// <param name="depth">The recursion guard shared with expression inference.</param>
    /// <returns>The selected model, or null for an ordinary candidate pattern.</returns>
    internal static ITypeSymbol? PatternInputType(SemanticModel model, PatternSyntax pattern, CancellationToken token, int depth = 0)
    {
        if (depth > 128)
        {
            return null;
        }

        token.ThrowIfCancellationRequested();
        return pattern.Parent switch
        {
            IsPatternExpressionSyntax expression => SelectedType(model, expression.Expression, token, depth + 1),
            SwitchExpressionArmSyntax { Parent: SwitchExpressionSyntax expression, }
                => SelectedType(model, expression.GoverningExpression, token, depth + 1),
            CasePatternSwitchLabelSyntax label => SelectedType(model,
                label.Ancestors().OfType<SwitchStatementSyntax>().First().Expression, token, depth + 1),
            SubpatternSyntax { ExpressionColon: { } colon, } subpattern => PatternMemberType(
                PatternInputType(model, subpattern.Ancestors().OfType<RecursivePatternSyntax>().First(), token, depth + 1),
                colon.Expression),
            PatternSyntax parent => PatternInputType(model, parent, token, depth + 1),
            _ => null,
        };
    }

    private static ITypeSymbol? CommonSelectedType(
        SemanticModel model, IEnumerable<ExpressionSyntax> branches, int depth, CancellationToken token)
    {
        ITypeSymbol? result = null;
        foreach (var branch in branches)
        {
            var type = SelectedType(model, branch, token, depth + 1);
            if (type is null)
            {
                if (!branch.IsKind(SyntaxKind.NullLiteralExpression) && !branch.IsKind(SyntaxKind.DefaultLiteralExpression)
                    && branch is not ThrowExpressionSyntax)
                {
                    return null;
                }

                continue;
            }

            if (result is not null && !SymbolEqualityComparer.Default.Equals(result, type))
            {
                return null;
            }

            result = type;
        }

        return result;
    }

    private static ITypeSymbol? InvocationType(SemanticModel model, InvocationExpressionSyntax call, int depth, CancellationToken token)
    {
        var info = model.GetSymbolInfo(call, token);
        var returnType = model.GetTypeInfo(call, token).Type;
        var parameter = returnType as ITypeParameterSymbol ?? OperandType(returnType) as ITypeParameterSymbol;
        var method = info.Symbol as IMethodSymbol
            ?? (info.CandidateSymbols.Length == 1 ? info.CandidateSymbols[0] as IMethodSymbol : null)
            ?? parameter?.ContainingSymbol as IMethodSymbol;
        if (method is null)
        {
            return null;
        }

        if (method.Name == "For" && IsFramework(method.ContainingType, "Selection"))
        {
            return method.TypeArguments[0];
        }

        var definition = (method.ReducedFrom ?? method).OriginalDefinition;
        if (definition.ReturnType.TypeKind != TypeKind.TypeParameter && SelectionType(definition.ReturnType) is null)
        {
            return null;
        }

        if (definition.IsExtensionMethod && call.Expression is MemberAccessExpressionSyntax member
            && SymbolEqualityComparer.Default.Equals(definition.Parameters[0].Type, definition.ReturnType)
            && SelectedType(model, member.Expression, token, depth + 1) is { } receiver)
        {
            return receiver;
        }

        for (var index = 0; index < call.ArgumentList.Arguments.Count; index++)
        {
            var argument = call.ArgumentList.Arguments[index];
            var position = method.ReducedFrom is null ? index : index + 1;
            var argumentParameter = argument.NameColon is { } named
                ? definition.Parameters.FirstOrDefault(candidate => candidate.Name == named.Name.Identifier.ValueText)
                : definition.Parameters.ElementAtOrDefault(position);
            if (SymbolEqualityComparer.Default.Equals(argumentParameter?.Type, definition.ReturnType))
            {
                return SelectedType(model, argument.Expression, token, depth + 1);
            }
        }

        return null;
    }

    private static ITypeSymbol? LocalType(SemanticModel model, IdentifierNameSyntax identifier, int depth, CancellationToken token)
    {
        if (identifier.Ancestors().OfType<SubpatternSyntax>().FirstOrDefault() is { ExpressionColon: { } colon, } subpattern
            && colon.Expression.Span.Contains(identifier.Span))
        {
            return MemberType(PatternInputType(model,
                subpattern.Ancestors().OfType<RecursivePatternSyntax>().First(), token, depth + 1), identifier.Identifier.ValueText);
        }

        if (model.GetSymbolInfo(identifier, token).Symbol is not ILocalSymbol local)
        {
            return null;
        }

        return local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token) switch
        {
            VariableDeclaratorSyntax { Initializer: { } initializer, } => SelectedType(model, initializer.Value, token, depth + 1),
            SingleVariableDesignationSyntax { Parent: PatternSyntax pattern, } => PatternInputType(model, pattern, token, depth + 1),
            _ => null,
        };
    }

    private static ITypeSymbol? PatternMemberType(ITypeSymbol? parent, ExpressionSyntax path)
    {
        var names = new Stack<string>();
        while (path is MemberAccessExpressionSyntax member)
        {
            names.Push(member.Name.Identifier.ValueText);
            path = member.Expression;
        }

        if (path is not IdentifierNameSyntax identifier)
        {
            return null;
        }

        parent = MemberType(parent, identifier.Identifier.ValueText);
        foreach (var name in names)
        {
            parent = MemberType(parent, name);
        }

        return parent;
    }

    private static ITypeSymbol? MemberType(ITypeSymbol? parent, string name)
    {
        return parent is INamedTypeSymbol named ? Properties(named).FirstOrDefault(property => property.Name == name)?.Type : null;
    }

    /// <summary>
    /// Detects source getters whose value depends on executable code.
    /// </summary>
    /// <param name="property">The selected property.</param>
    /// <returns>Whether a source getter has a body or expression.</returns>
    internal static bool IsComputed(IPropertySymbol property)
    {
        return property.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax() is PropertyDeclarationSyntax syntax
                && (syntax.ExpressionBody is not null || syntax.AccessorList?.Accessors.Any(accessor =>
                    accessor.Keyword.ValueText == "get" && (accessor.Body is not null || accessor.ExpressionBody is not null)) == true));
    }
}