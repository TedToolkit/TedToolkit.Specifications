// -----------------------------------------------------------------------
// <copyright file="SpecificationGenerator.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using TedToolkit.RoslynHelper;
using TedToolkit.RoslynHelper.Syntaxes;

using static TedToolkit.RoslynHelper.SourceComposer;
using static TedToolkit.RoslynHelper.SourceComposer<TedToolkit.Specifications.Analyzers.SpecificationGenerator>;

namespace TedToolkit.Specifications.Analyzers;

/// <summary>
/// Generates one level of direct member access for each selected type used in source.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SpecificationGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entities = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax or SubpatternSyntax
                || (node is GenericNameSyntax name
                && ((name.Identifier.ValueText == "For" && name.Arity == 1)
                    || (name.Identifier.ValueText == "Selection" && name.Arity == 2))),
            static (syntax, token) =>
            {
                if (syntax.Node is MemberAccessExpressionSyntax member)
                {
                    return ModelFacts.SelectedType(syntax.SemanticModel, member.Expression, token) as INamedTypeSymbol;
                }

                if (syntax.Node is MemberBindingExpressionSyntax binding)
                {
                    var conditional = binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>()
                        .First(expression => expression.WhenNotNull.Span.Contains(binding.Span));
                    return ModelFacts.SelectedType(syntax.SemanticModel, conditional.Expression, token) as INamedTypeSymbol;
                }

                if (syntax.Node is SubpatternSyntax subpattern)
                {
                    return ModelFacts.PatternInputType(syntax.SemanticModel,
                        subpattern.Ancestors().OfType<RecursivePatternSyntax>().First(), token) as INamedTypeSymbol;
                }

                return syntax.SemanticModel.GetSymbolInfo(syntax.Node, token).Symbol switch
                {
                    IMethodSymbol method when method.Name == "For" && ModelFacts.IsFramework(method.ContainingType, "Selection")
                        => method.TypeArguments[0] as INamedTypeSymbol,
                    INamedTypeSymbol named when ModelFacts.SelectionType(named) is { } selection
                        => selection.TypeArguments[1] as INamedTypeSymbol,
                    _ => null,
                };
            }).Where(
                static entity => entity?.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface).Collect();
        context.RegisterSourceOutput(entities.Combine(context.CompilationProvider), static (production, input) =>
        {
            var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var entity in input.Left.Where(entity => entity is not null && seen.Add(entity)))
            {
                Generate(production, entity!, input.Right);
            }
        });
    }

    private static void Generate(in SourceProductionContext context, INamedTypeSymbol entity, Compilation compilation)
    {
        if (ModelFacts.ContainsAnonymousType(entity))
        {
            return;
        }

        if (compilation is CSharpCompilation { LanguageVersion: < LanguageVersion.CSharp14, })
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.Language, entity.Locations.FirstOrDefault()));
            return;
        }

        if (!ModelFacts.IsAccessibleEntity(entity, compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.Entity, entity.Locations.FirstOrDefault(), entity.Name));
            return;
        }

        var properties = new SortedDictionary<string, IPropertySymbol>(StringComparer.Ordinal);
        var encountered = new Dictionary<string, IPropertySymbol>(StringComparer.Ordinal);
        foreach (var property in ModelFacts.Properties(entity))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (property.IsIndexer)
            {
                continue;
            }

            if (encountered.TryGetValue(property.Name, out var nearer))
            {
                var overridden = nearer.OverriddenProperty;
                while (overridden is not null && !SymbolEqualityComparer.Default.Equals(overridden, property))
                {
                    overridden = overridden
                    .OverriddenProperty;
                }

                if (overridden is null)
                {
                    context.ReportDiagnostic(
                    Diagnostic.Create(Diagnostics.HiddenProperty, nearer.Locations.FirstOrDefault(), property.Name));
                }

                continue;
            }

            encountered.Add(property.Name, property);
            if (property.ReturnsByRef || property.ReturnsByRefReadonly || property.Type.IsRefLikeType
                || property.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer or TypeKind.Error
                || property.GetMethod is null || !compilation.IsSymbolAccessibleWithin(property.GetMethod, compilation.Assembly))
            {
                continue;
            }

            properties.Add(property.Name, property);
        }

        if (properties.Count == 0)
        {
            return;
        }

        var entityType = SourceType(entity, compilation);
        var candidate = compilation.GetTypeByMetadataName("TedToolkit.Specifications.Selection`2")!.TypeParameters[0];
        var declaration = Class(ModelFacts.GeneratedName(entity)).Static.Internal;
        declaration.AddRootDescription(Summary("Provides one level of generated member selection."));
        var receiver = Runtime(compilation, "Selection`2", candidate, entity);
        var extension = new Extension(new Parameter(receiver, "selection")).AddTypeParameter(new TypeParameter("TCandidate"));
        var index = 0;
        foreach (var property in properties.Values)
        {
            var valueType = property.Type;
            var selectionType = Runtime(compilation, "Selection`2", candidate, valueType);
            var implementation = Runtime(compilation, "MemberSelection`3", candidate, entity, valueType);
            var getterName = "GetMember" + index++;
            var parent = "value".ToSimpleName().SuppressNullableWarning();
            var target = SymbolEqualityComparer.Default.Equals(property.ContainingType, entity)
                ? (IExpression)parent
                : new CastExpression(SourceType(property.ContainingType, compilation), parent).Wrap;
            declaration.AddMember(Method(getterName, new ReturnType(SourceType(valueType, compilation))).Private.Static
                .AddParameter(new Parameter(entityType, "value"))
                .AddStatement(target.Sub(property.Name.ToValidIdentifier()).Return));
            extension.AddMember(Property(selectionType, property.Name).Public
                .AddRootDescription(Summary("Selects this member while preserving the root candidate type."))
                .AddAccessor(new Accessor(AccessorType.GET).AddStatement(new ObjectCreationExpression(implementation)
                    .AddArguments("selection".ToSimpleName(), property.Name.ToLiteral(), getterName.ToSimpleName()).Return)));
        }

        declaration.AddMember(extension);

        // Selection is invariant. Oblivious reference annotations let one accessor serve both nullable
        // and non-nullable parent selections; TryGetValue retains null propagation at runtime.
        var file = new SourceFile() { DisableWarnings = false, NullableContext = NullableContextOptions.Warnings, };
        file.AddNameSpace(NameSpace("TedToolkit.Specifications").AddMember(declaration));
        file.Generate(context, ModelFacts.GeneratedName(entity));
    }

    private static DataType Runtime(Compilation compilation, string name, params ITypeSymbol[] arguments)
    {
        var symbol = compilation.GetTypeByMetadataName("TedToolkit.Specifications." + name)!.Construct(arguments);
        return SourceType(symbol, compilation);
    }

    private static DataType SourceType(ITypeSymbol type, Compilation compilation)
    {
        if (type.TypeKind == TypeKind.Dynamic)
        {
            return DataType.Object;
        }

        if (type is INamedTypeSymbol { IsNativeIntegerType: true, })
        {
            return new(type.SpecialType == SpecialType.System_IntPtr ? "nint" : "nuint");
        }

        if (type is IArrayTypeSymbol array)
        {
            var element = SourceType(array.ElementType, compilation);

            // RoslynHelper's single array flag cannot preserve ranked or jagged array types.
            return new(new CustomExpression((ref SourceBuilder builder) =>
            {
                element.ToCode(ref builder);
                builder.Append('[');
                builder.Append(',', array.Rank - 1);
                builder.Append(']');
            }));
        }

        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, } nullable)
        {
            return SourceType(nullable.TypeArguments[0], compilation).Null;
        }

        if (type is INamedTypeSymbol { IsTupleType: true, } tuple)
        {
            var expression = new TupleExpression();
            foreach (var element in tuple.TupleElements)
            {
                expression.AddItem(SourceType(element.Type, compilation), element.Name);
            }

            return new(expression);
        }

        if (type is not INamedTypeSymbol { SpecialType: SpecialType.None, } named)
        {
            return DataType.FromSymbol(type.WithNullableAnnotation(NullableAnnotation.None), compilation);
        }

        var name = named.Name.ToValidIdentifier().ToSimpleName()
            .Generic(named.TypeArguments.Select(argument => SourceType(argument, compilation)).ToArray());
        if (named.ContainingType is { } containing)
        {
            return new(new MemberAccessExpression(SourceType(containing, compilation).Type, name));
        }

        IExpression qualified = name;
        for (var current = named.ContainingNamespace; !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            qualified = new MemberAccessExpression(current.Name.ToValidIdentifier().ToSimpleName(), qualified);
        }

        return new(new AliasExpression(named.GetAlias(compilation), qualified));
    }

    private static DescriptionSummary Summary(string text)
    {
        return new(new DescriptionText(text));
    }
}