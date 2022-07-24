using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Rem.Core.ComponentModel.Logical;

/// <summary>
/// A class containing methods for wrapping and unwrapping objects for packaging in logical types.
/// </summary>
/// <remarks>
/// Many of the operations in this class are fundamentally unsafe and should only be used internally.
/// No type constraints are placed on types, as the information is stored internally by the library and cached for
/// easy access and so that the logical types can support both values and references.
/// </remarks>
internal static class Wrapping
{
    /// <summary>
    /// Wraps an object of a value type in a wrapper that can be packaged into a logical type.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object WrapValueType<TValue>(TValue value)
        where TValue : notnull
        => new ValueTypeWrapper<TValue>(value);

    /// <summary>
    /// Unwraps a wrapper produced by <see cref="WrapValueType{TValue}(TValue)"/>, and types the result as an instance
    /// of <typeparamref name="TReference"/>.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TReference"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TReference UnwrapValueTypeAsReferenceType<TValue, TReference>(object obj)
        where TValue : notnull
        where TReference : notnull
    {
        object boxed = UnwrapValueType<TValue>(obj);
        return Unsafe.As<object, TReference>(ref boxed)!;
    }

    /// <summary>
    /// Unwraps a wrapper produced by <see cref="WrapValueType{TValue}(TValue)"/>.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue UnwrapValueType<TValue>(object value)
        where TValue : notnull
        => Unsafe.As<ValueTypeWrapper<TValue>>(value).Value;

    /// <summary>
    /// Unsafely casts a reference type value to an instance of the underlying type.
    /// </summary>
    /// <typeparam name="TReference"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TReference UnwrapReferenceType<TReference>(object value)
        where TReference : notnull
        => Unsafe.As<object, TReference>(ref value);
}
