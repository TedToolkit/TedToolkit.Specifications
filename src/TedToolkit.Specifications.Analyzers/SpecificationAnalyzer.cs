// -----------------------------------------------------------------------
// <copyright file="SpecificationAnalyzer.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TedToolkit.Specifications.Analyzers;

/// <summary>
/// Checks typed selections, specification construction, and SQL portability.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SpecificationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(
                Diagnostics.Accessor, Diagnostics.Incomplete, Diagnostics.NullCheck, Diagnostics.ObjectMethod,
                Diagnostics.PropertyType, Diagnostics.Property, Diagnostics.StringValue, Diagnostics.Implementation,
                Diagnostics.UnsupportedOperation, Diagnostics.NumberValue, Diagnostics.NullPattern, Diagnostics.ReferenceEquality,
                Diagnostics.Computed, Diagnostics.CandidateRequired, Diagnostics.MemberCollision);
        }
    }

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeConstruction, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzeBinary, OperationKind.Binary);
        context.RegisterOperationAction(AnalyzePattern, OperationKind.IsPattern, OperationKind.IsType,
            OperationKind.Switch, OperationKind.SwitchExpression);
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeMember, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeCondition, SyntaxKind.IfStatement, SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement, SyntaxKind.ForStatement, SyntaxKind.ConditionalExpression,
            SyntaxKind.CatchFilterClause, SyntaxKind.WhenClause);
    }

    private static void AnalyzeCondition(SyntaxNodeAnalysisContext context)
    {
        ExpressionSyntax? condition = context.Node switch
        {
            IfStatementSyntax statement => statement.Condition,
            WhileStatementSyntax statement => statement.Condition,
            DoStatementSyntax statement => statement.Condition,
            ForStatementSyntax statement => statement.Condition,
            ConditionalExpressionSyntax expression => expression.Condition,
            CatchFilterClauseSyntax filter => filter.FilterExpression,
            WhenClauseSyntax clause => clause.Condition,
            _ => null,
        };
        if (condition is null
            || !ModelFacts.IsSpecification(context.SemanticModel.GetTypeInfo(condition, context.CancellationToken).Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CandidateRequired, condition.GetLocation()));
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Compilation.AssemblyName == "TedToolkit.Specifications")
        {
            return;
        }

        var call = (IInvocationOperation)context.Operation;
        var method = call.TargetMethod;
        var receiver = call.Instance is { } instance ? Unwrap(instance) : null;
        var valueType = ModelFacts.OperandType(receiver?.Type);
        if (IsObjectMethod(method)
            && (IsDsl(receiver?.Type) || (method.Name is "Equals" or "ReferenceEquals"
                && call.Arguments.Any(argument => IsDsl(Unwrap(argument.Value).Type)))))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ObjectMethod, call.Syntax.GetLocation(), method.Name));
        }

        if (valueType is not null)
        {
            var name = method.Name;
            var scalar = name is "Compare" or "EqualTo" or "NotEqualTo" or "In" or "GreaterThan" or "GreaterThanOrEqual"
                or "LessThan" or "LessThanOrEqual" or "Contains" or "StartsWith" or "EndsWith";
            var ordered = name is "GreaterThan" or "GreaterThanOrEqual" or "LessThan" or "LessThanOrEqual"
                || (name == "Compare" && call.Arguments.Any(argument => argument.Parameter?.Name == "operation"
                    && argument.Value.ConstantValue is { HasValue: true, Value: int operation, } && operation is >= 2 and <= 5));
            var text = name is "Contains" or "StartsWith" or "EndsWith";
            var category = ModelFacts.ValueCategory(valueType);
            if (scalar && valueType.TypeKind is not (TypeKind.TypeParameter or TypeKind.Error))
            {
                if (category is null && HasSql(context.Compilation))
                {
                    context.ReportDiagnostic(
                    Diagnostic.Create(Diagnostics.PropertyType, call.Syntax.GetLocation(), valueType.Name, valueType));
                }
                else if ((ordered && !ModelFacts.SupportsOrdering(valueType, context.Compilation)) || (text && category != "String"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedOperation, call.Syntax.GetLocation(), name));
                }
            }

            if (name is "IsNull" or "IsNotNull" && valueType.IsValueType && IsRootSelection(receiver)
                && SymbolEqualityComparer.Default.Equals(ModelFacts.Unwrap(valueType), valueType))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NullCheck, call.Syntax.GetLocation(), valueType.Name));
            }

            if (scalar)
            {
                foreach (var argument in call.Arguments)
                {
                    CheckConstant(context, argument.Value);
                    if (text && argument.Value.ConstantValue is { HasValue: true, Value: null, })
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NullPattern, argument.Syntax.GetLocation()));
                    }
                }
            }
        }
    }

    private static bool IsObjectMethod(IMethodSymbol method)
    {
        for (IMethodSymbol? current = method; current is not null; current = current.OverriddenMethod)
        {
            if (current.ContainingType.SpecialType == SpecialType.System_Object
                && current.Name is "Equals" or "ReferenceEquals" or "GetHashCode" or "ToString" or "GetType")
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSql(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("TedToolkit.Specifications.Sql.SqlTranslator") is not null;
    }

    private static bool IsDsl(ITypeSymbol? type)
    {
        return ModelFacts.SelectionType(type) is not null || ModelFacts.IsSpecification(type);
    }

    private static IOperation Unwrap(IOperation value)
    {
        return value is IConversionOperation { OperatorMethod: null, } conversion ? Unwrap(conversion.Operand) : value;
    }

    private static bool IsRootSelection(IOperation? value)
    {
        return value is not null && Unwrap(value) is IInvocationOperation call
            && call.TargetMethod.Name == "For" && ModelFacts.IsFramework(call.TargetMethod.ContainingType, "Selection");
    }

    private static void AnalyzeConstruction(OperationAnalysisContext context)
    {
        if (context.Compilation.AssemblyName == "TedToolkit.Specifications"
            || !ModelFacts.IsFramework(((IObjectCreationOperation)context.Operation).Type as INamedTypeSymbol, "MemberSelection`3"))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.Accessor, context.Operation.Syntax.GetLocation()));
    }

    private static void AnalyzePattern(OperationAnalysisContext context)
    {
        if (context.Compilation.AssemblyName == "TedToolkit.Specifications")
        {
            return;
        }

        var referenceTest = context.Operation switch
        {
            IIsTypeOperation operation => IsDsl(Unwrap(operation.ValueOperand).Type),
            IIsPatternOperation operation => IsDsl(Unwrap(operation.Value).Type) && IsNullPattern(operation.Pattern),
            ISwitchExpressionOperation expression => IsDsl(Unwrap(expression.Value).Type)
                && expression.Arms.Any(arm => IsNullPattern(arm.Pattern)),
            ISwitchOperation statement => IsDsl(Unwrap(statement.Value).Type)
                && statement.Cases.SelectMany(branch => branch.Clauses).Any(IsNullClause),
            _ => false,
        };
        if (!referenceTest)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ReferenceEquality, context.Operation.Syntax.GetLocation()));
    }

    private static bool IsNullClause(ICaseClauseOperation clause)
    {
        return clause switch
        {
            IPatternCaseClauseOperation pattern => IsNullPattern(pattern.Pattern),
            ISingleValueCaseClauseOperation constant => constant.Value.ConstantValue is { HasValue: true, Value: null, },
            _ => false,
        };
    }

    private static bool IsNullPattern(IPatternOperation pattern)
    {
        return pattern switch
        {
            IConstantPatternOperation constant => constant.Value.ConstantValue is { HasValue: true, Value: null, },
            INegatedPatternOperation negated => IsNullPattern(negated.Pattern),
            ITypePatternOperation => true,
            IDeclarationPatternOperation declaration => !declaration.MatchesNull,
            IBinaryPatternOperation binary => IsNullPattern(binary.LeftPattern) || IsNullPattern(binary.RightPattern),
            IRecursivePatternOperation recursive => (recursive.DeconstructionSubpatterns.IsEmpty && recursive.PropertySubpatterns.IsEmpty)
                || recursive.PropertySubpatterns.Any(property => IsDsl(property.Pattern.InputType) && IsNullPattern(property.Pattern)),
            _ => false,
        };
    }

    private static void AnalyzeBinary(OperationAnalysisContext context)
    {
        if (context.Compilation.AssemblyName == "TedToolkit.Specifications")
        {
            return;
        }

        var binary = (IBinaryOperation)context.Operation;
        var left = Unwrap(binary.LeftOperand).Type;
        var right = Unwrap(binary.RightOperand).Type;
        if (binary.Type?.SpecialType == SpecialType.System_Boolean
            && binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals
            && (IsDsl(left) || IsDsl(right)))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ReferenceEquality, binary.Syntax.GetLocation()));
            return;
        }

        var valueType = ModelFacts.OperandType(left) ?? ModelFacts.OperandType(right);
        if (valueType is null || valueType.TypeKind is TypeKind.TypeParameter or TypeKind.Error)
        {
            return;
        }

        var ordered = binary.OperatorKind is BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual
            or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual;
        var equality = binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals;
        if (!ordered && !equality)
        {
            return;
        }

        if (ModelFacts.ValueCategory(valueType) is null && HasSql(context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.PropertyType, binary.Syntax.GetLocation(), valueType.Name, valueType));
        }
        else if (ordered && !ModelFacts.SupportsOrdering(valueType, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedOperation, binary.Syntax.GetLocation(), binary.OperatorKind));
        }

        CheckConstant(context, binary.LeftOperand);
        CheckConstant(context, binary.RightOperand);
    }

    private static void CheckConstant(in OperationAnalysisContext context, IOperation value)
    {
        if (HasSql(context.Compilation)
            && ((value.ConstantValue is { HasValue: true, Value: double number, } && (double.IsNaN(number) || double.IsInfinity(number)))
                || (value.ConstantValue is { HasValue: true, Value: float single, } && (float.IsNaN(single) || float.IsInfinity(single)))))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NumberValue, value.Syntax.GetLocation()));
            return;
        }

        if (!value.ConstantValue.HasValue)
        {
            foreach (var child in ConstantElements(value))
            {
                CheckConstant(context, child);
            }
        }

        if (!HasSql(context.Compilation) || !value.ConstantValue.HasValue || value.ConstantValue.Value is not string text)
        {
            return;
        }

        for (var index = 0; index < text.Length; index++)
        {
            var invalidHigh = char.IsHighSurrogate(text[index])
                && (index + 1 == text.Length || !char.IsLowSurrogate(text[index + 1]));
            var invalidLow = char.IsLowSurrogate(text[index])
                && (index == 0 || !char.IsHighSurrogate(text[index - 1]));
            if (text[index] == '\0' || invalidHigh || invalidLow)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.StringValue, value.Syntax.GetLocation()));
                return;
            }
        }
    }

    private static IEnumerable<IOperation> ConstantElements(IOperation value)
    {
        return value switch
        {
            IConversionOperation { OperatorMethod: null, } conversion => [conversion.Operand,],
            IArrayCreationOperation { Initializer: { } initializer, } => [initializer,],
            IArrayInitializerOperation initializer => initializer.ElementValues,
            ICollectionExpressionOperation collection => collection.Elements,
            ISpreadOperation spread => [spread.Operand,],
            IObjectCreationOperation { Initializer: { } initializer, Type: INamedTypeSymbol type, }
                when type.ContainingNamespace.ToDisplayString() == "System.Collections.Generic"
                    && type.OriginalDefinition.MetadataName is "List`1" or "HashSet`1"
                => initializer.Initializers.OfType<IInvocationOperation>()
                    .SelectMany(call => call.Arguments.Select(argument => argument.Value)),
            _ => [],
        };
    }

    private static void AnalyzeMember(SyntaxNodeAnalysisContext context)
    {
        var member = (MemberAccessExpressionSyntax)context.Node;
        var receiverType = context.SemanticModel.GetTypeInfo(member.Expression, context.CancellationToken).Type;
        var symbol = context.SemanticModel.GetSymbolInfo(member, context.CancellationToken).Symbol;
        if (ModelFacts.SelectionType(receiverType) is not null && symbol is null
            && member.Name.Identifier.ValueText is "IsSatisfiedBy" or "And" or "Or" or "Not")
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.Incomplete, member.Name.GetLocation()));
            return;
        }

        if (ModelFacts.SelectedType(
            context.SemanticModel,
            member.Expression,
            context.CancellationToken) is not INamedTypeSymbol entity)
        {
            return;
        }

        var property = ModelFacts.Properties(entity).FirstOrDefault(candidate => candidate.Name == member.Name.Identifier.ValueText);
        if (property is null)
        {
            return;
        }

        if (symbol is null)
        {
            var diagnostic = ModelFacts.IsSelectionMember(property.Name, context.Compilation)
                ? Diagnostics.MemberCollision
                : Diagnostics.Property;
            context.ReportDiagnostic(Diagnostic.Create(diagnostic, member.Name.GetLocation(), property.Name));
        }
        else if (symbol is IPropertySymbol selected && ModelFacts.SelectionType(selected.Type) is not null
            && HasSql(context.Compilation) && ModelFacts.IsComputed(property))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.Computed, member.Name.GetLocation(), property.Name));
        }
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.ContainingAssembly.Name == "TedToolkit.Specifications")
        {
            return;
        }

        if (ModelFacts.SelectionType(type.BaseType) is null && !ModelFacts.IsSpecification(type.BaseType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.Implementation, type.Locations.FirstOrDefault(), type.Name));
    }
}