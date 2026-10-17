// -----------------------------------------------------------------------
// <copyright file="SqlValue.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql;

/// <summary>
/// Classifies and validates values at the SQL translation boundary.
/// </summary>
internal static class SqlValue
{
    /// <summary>
    /// Classifies a supported CLR type for SQL storage and comparison.
    /// </summary>
    /// <param name = "type">The CLR type to classify.</param>
    /// <returns>The supported SQL storage category.</returns>
    /// <exception cref="NotSupportedException">The CLR type has no supported SQL representation.</exception>
    internal static SqlValueKind Kind(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string))
        {
            return SqlValueKind.STRING;
        }

        if (type == typeof(bool))
        {
            return SqlValueKind.BOOLEAN;
        }

        if (type == typeof(decimal) || type == typeof(ulong))
        {
            return SqlValueKind.DECIMAL;
        }

        if (type == typeof(float) || type == typeof(double))
        {
            return SqlValueKind.FLOATING_POINT;
        }

        if (type == typeof(Guid))
        {
            return SqlValueKind.GUID;
        }

        if (type == typeof(DateOnly))
        {
            return SqlValueKind.DATE;
        }

        if (type == typeof(TimeOnly))
        {
            return SqlValueKind.TIME;
        }

        if (type == typeof(DateTime))
        {
            return SqlValueKind.DATE_TIME;
        }

        if (type == typeof(DateTimeOffset))
        {
            return SqlValueKind.DATE_TIME_OFFSET;
        }

        if (type.IsEnum)
        {
            _ = Kind(Enum.GetUnderlyingType(type));
            return SqlValueKind.ENUM;
        }

        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long))
        {
            return SqlValueKind.INTEGER;
        }

        throw new NotSupportedException($"Scalar type '{type}' is not supported by the specification grammar.");
    }

    /// <summary>
    /// Rejects scalar values that cannot be represented portably in SQL.
    /// </summary>
    /// <typeparam name = "TValue">The selected value type.</typeparam>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <exception cref="ArgumentException">The value is non-finite or contains unrepresentable SQL text.</exception>
    internal static void Validate<TValue>(TValue value)
    {
        if (value is string text)
        {
            ValidateText(text);
        }

        if ((value is not double number || double.IsFinite(number)) && (value is not float single || float.IsFinite(single)))
        {
            return;
        }

        throw new ArgumentException("Specification floating-point values must be finite.");
    }

    /// <summary>
    /// Rejects zero characters and malformed UTF-16 in SQL text.
    /// </summary>
    /// <param name = "text">The text to validate.</param>
    /// <exception cref="ArgumentException">The value is non-finite or contains unrepresentable SQL text.</exception>
    internal static void ValidateText(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\0')
            {
                throw new ArgumentException("Specification strings cannot contain zero characters.");
            }

            var invalidHigh = char.IsHighSurrogate(text[index]) && (index + 1 == text.Length || !char.IsLowSurrogate(text[index + 1]));
            var invalidLow = char.IsLowSurrogate(text[index]) && (index == 0 || !char.IsHighSurrogate(text[index - 1]));
            if (invalidHigh || invalidLow)
            {
                throw new ArgumentException("Specification strings must contain valid Unicode scalar values.");
            }
        }
    }
}