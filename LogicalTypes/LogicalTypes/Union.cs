using Rem.Core.Attributes;
using Rem.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rem.Core.ComponentModel.Logical;

using TypeFlags2 = Union.TypeFlags2;

/// <summary>
/// Static functionality for working with union types.
/// </summary>
public static class Union
{
    /// <summary>
    /// Represents the wrapped type of a union of two types.
    /// </summary>
    [Flags]
    public enum TypeFlags2
    {
        /// <summary>
        /// Indicates that the union is the default value.
        /// </summary>
        /// <remarks>
        /// If working with reference types or nullable value types, this case can be interpreted
        /// as <see langword="null"/>.
        /// </remarks>
        Default = 0,

        /// <summary>
        /// Indicates that the union wraps a value of the first type.
        /// </summary>
        T1 = 1,

        /// <summary>
        /// Indicates that the union wraps a value of the second type.
        /// </summary>
        T2 = 2,

        /// <summary>
        /// Indicates that the union wraps a value of both the first type and the second type.
        /// </summary>
        Both = T1 | T2,
    }
}

/// <summary>
/// Represents a union of two types.
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public readonly struct Union<T1, T2> : IDefaultableStruct
    where T1 : notnull
    where T2 : notnull
{
    #region Properties And Fields
    /// <summary>
    /// Type flags indicating which of <typeparamref name="T1"/> and <typeparamref name="T2"/> are value types.
    /// </summary>
    private static readonly TypeFlags2 ValueTypeFlags;

    /// <inheritdoc/>
    public bool IsDefault => _typeFlags == 0;

    /// <summary>
    /// Gets the value of type <typeparamref name="T1"/> wrapped in the current union instance, or
    /// <see langword="null"/> if the current instance does not wrap a value of type <typeparamref name="T1"/>.
    /// </summary>
    public T1? AsT1 => IsT1() ? UnsafeAsT1 : default;

    /// <summary>
    /// Gets the value of type <typeparamref name="T1"/> wrapped in the current union instance, or the default value
    /// of type <typeparamref name="T1"/> if the current instance does not wrap a value of
    /// type <typeparamref name="T1"/>.
    /// </summary>
    [MaybeNull] public T1 AsT1OrDefault => IsT1() ? UnsafeAsT1 : default;

    /// <summary>
    /// Gets the value of type <typeparamref name="T2"/> wrapped in the current union instance, or the default value
    /// of type <typeparamref name="T2"/> if the current instance does not wrap a value of
    /// type <typeparamref name="T2"/>.
    /// </summary>
    [MaybeNull] public T2 AsT2OrDefault => IsT2() ? UnsafeAsT2 : default;

    /// <summary>
    /// Gets the value of type <typeparamref name="T2"/> wrapped in the current union instance, or
    /// <see langword="null"/> if the current instance does not wrap a value of type <typeparamref name="T2"/>.
    /// </summary>
    public T2? AsT2 => IsT2() ? UnsafeAsT2 : default;

    /// <summary>
    /// Gets the value wrapped in this union typed as an <see cref="object"/>.
    /// </summary>
    [MaybeDefaultIfInstanceDefault]
    public object AsObject
        => IsDefault
            ? null!
            : ValueTypeFlag switch
            {
                TypeFlags2.T1 => Wrapping.UnwrapValueType<T1>(_wrappedValue),
                TypeFlags2.T2 => Wrapping.UnwrapValueType<T2>(_wrappedValue),
                _ => _wrappedValue, // Is just a reference type
            };

    [DoesNotReturnIfInstanceDefault]
    public Type GetWrappedType() => ValueTypeFlag switch
    {
        // Value types are wrapped in an object, so getting the type of the value wrapped in this instance would be
        // incorrect if it is a value type
        // A value type cannot be more specific than the type itself (since value types are sealed), so just return
        // the type in that case
        TypeFlags2.T1 => typeof(T1),
        TypeFlags2.T2 => typeof(T2),

        _ => _wrappedValue.GetType(),
    };

    /// <summary>
    /// Unwraps the <typeparamref name="T1"/> value wrapped in this instance, handling value types correctly.
    /// </summary>
    /// <remarks>
    /// This property should only ever be accessed if this instance wraps a <typeparamref name="T1"/> value, or else
    /// the behavior is undefined.
    /// </remarks>
    private T1 UnsafeAsT1 => ValueTypeFlag switch
    {
        // This wraps a T2 and T2 is a value type
        // Assume that T1 is a reference type (in order for the instance to wrap a value of both T1 and T2) and
        // unwrap the value as the reference
        TypeFlags2.T2 => Wrapping.UnwrapValueTypeAsReferenceType<T2, T1>(_wrappedValue),

        // T1 is a value type
        TypeFlags2.T1 => Wrapping.UnwrapValueType<T1>(_wrappedValue),

        // T1 is a reference type, and either T2 is a reference type or this does not wrap a T2
        // In either case unwrap as a reference type
        _ => Wrapping.UnwrapReferenceType<T1>(_wrappedValue),
    };

    /// <summary>
    /// Unwraps the <typeparamref name="T2"/> value wrapped in this instance, handling value types correctly.
    /// </summary>
    /// <remarks>
    /// This property should only ever be accessed if this instance wraps a <typeparamref name="T2"/> value, or else
    /// the behavior is undefined.
    /// </remarks>
    private T2 UnsafeAsT2 => ValueTypeFlag switch
    {
        // This wraps a T1 and T1 is a value type
        // Assume that T2 is a reference type (in order for the instance to wrap a value of both T1 and T2) and
        // unwrap the value as the reference
        TypeFlags2.T1 => Wrapping.UnwrapValueTypeAsReferenceType<T1, T2>(_wrappedValue),

        // T2 is a value type
        TypeFlags2.T2 => Wrapping.UnwrapValueType<T2>(_wrappedValue),

        // T2 is a reference type, and either T1 is a reference type or this does not wrap a T1
        _ => Wrapping.UnwrapReferenceType<T2>(_wrappedValue),
    };

    /// <summary>
    /// Gets the sole type flag describing the value wrapped in this instance that indicates a value type, or
    /// <see cref="TypeFlags2.Default"/> if this instance wraps a reference type.
    /// </summary>
    /// <remarks>
    /// If this property returns anything other than <see cref="TypeFlags2.Default"/>, the value wrapped in this type
    /// is a value type and needs to be unwrapped before being treated as an object.
    /// </remarks>
    [NamedEnum] private TypeFlags2 ValueTypeFlag => _typeFlags & ValueTypeFlags;

    /// <summary>
    /// Flags describing the value wrapped in the union with respect to <typeparamref name="T1"/>
    /// and <typeparamref name="T2"/>.
    /// </summary>
    [NamedEnum] public TypeFlags2 TypeFlags => _typeFlags;
    [NamedEnum] private readonly TypeFlags2 _typeFlags;

    [MaybeDefaultIfInstanceDefault] private readonly object _wrappedValue;
    #endregion

    #region Constructors And Factory Methods
    static Union()
    {
        ValueTypeFlags = default;
        if (typeof(T1).IsValueType) ValueTypeFlags |= TypeFlags2.T1;
        if (typeof(T2).IsValueType) ValueTypeFlags |= TypeFlags2.T2;
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="Union{T1, T2}"/> struct wrapping the value passed in.
    /// </summary>
    /// <param name="Value"></param>
    public Union(T1 Value)
    {
        Throw.IfArgNull(Value, nameof(Value));

        _typeFlags = TypeFlags2.T1;

        // T1 is a value type
        // Need to wrap as T1 value
        if (Enums.HasFlag(ValueTypeFlags, TypeFlags2.T1))
        {
            _wrappedValue = Wrapping.WrapValueType(Value);
            if (Value is T2) _typeFlags |= TypeFlags2.T2;
        }
        else // T1 is a reference type
        {
            // T2 is a value type and the value passed in is T2 as well as T1
            // Need to wrap as T2 value
            if (Enums.HasFlag(ValueTypeFlags, TypeFlags2.T2) && Value is T2 t2Instance)
            {
                _wrappedValue = Wrapping.WrapValueType(t2Instance);
                _typeFlags |= TypeFlags2.T2;
            }

            // T1 is not an instance of a value type tracked by the union
            // No need to wrap
            else
            {
                _wrappedValue = Value;
                if (Value is T2) _typeFlags |= TypeFlags2.T2;
            }
        }
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="Union{T1, T2}"/> struct wrapping the value passed in.
    /// </summary>
    /// <param name="Value"></param>
    public Union(T2 Value)
    {
        Throw.IfArgNull(Value, nameof(Value));

        _typeFlags = TypeFlags2.T2;

        // T2 is a value type
        // Need to wrap as T2 value
        if (Enums.HasFlag(ValueTypeFlags, TypeFlags2.T2))
        {
            _wrappedValue = Wrapping.WrapValueType(Value);
            if (Value is T1) _typeFlags |= TypeFlags2.T1;
        }
        else // T2 is a reference type
        {
            // T1 is a value type and the value passed in is T1 as well as T2
            // Need to wrap as T1 value
            if (Enums.HasFlag(ValueTypeFlags, TypeFlags2.T1) && Value is T1 t1Instance)
            {
                _wrappedValue = Wrapping.WrapValueType(t1Instance);
                _typeFlags |= TypeFlags2.T1;
            }

            // T1 is not an instance of a value type tracked by the union
            // No need to wrap
            else
            {
                _wrappedValue = Value;
                if (Value is T1) _typeFlags |= TypeFlags2.T1;
            }
        }
    }

    /// <summary>
    /// Creates a new <see cref="Union{T1, T2}"/> wrapping the instance passed in.
    /// </summary>
    /// <typeparam name="TChild"></typeparam>
    /// <param name="Value"></param>
    /// <returns></returns>
    public static Union<T1, T2> FromChild<TChild>(TChild Value) where TChild : T1, T2
    {
        Throw.IfArgNull(Value, nameof(Value));

        if (Enums.HasFlag(ValueTypeFlags, TypeFlags2.T1))
        {
            return new(Wrapping.WrapValueType(Value), TypeFlags2.Both);
        }
        else if (Enums.HasFlag(ValueTypeFlags, TypeFlags2.T2))
        {
            return new(Wrapping.WrapValueType(Value), TypeFlags2.Both);
        }
        else return new(Value, TypeFlags2.Both);
    }

    private Union(object WrappedValue, [NamedEnum] TypeFlags2 TypeFlags)
    {
        _wrappedValue = WrappedValue;
        _typeFlags = TypeFlags;
    }
    #endregion

    #region Methods
    #region Stored Type Information
    #region Cast
    /// <summary>
    /// Casts the current union instance to type <typeparamref name="T1"/>.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NullReferenceException">
    /// The current union instance is the default, and <typeparamref name="T1"/> is a value type.
    /// </exception>
    /// <exception cref="InvalidCastException">The cast was invalid.</exception>
    [return: MaybeDefaultIfInstanceDefault]
    public T1 CastToT1()
    {
        if (IsDefault)
        {
            return Enums.HasFlag(ValueTypeFlags, TypeFlags2.T1)
                    ? throw new NullReferenceException(
                        $"Cannot cast default union to instance of value type '{typeof(T1)}'.")
                    : default!; // This is null since T1 is a reference type
        }

        else if (IsT1()) return UnsafeAsT1;

        else // Invalid cast
        {
            // Try to be as specific about what went wrong with the type conversion as possible
            var wrappedType = GetWrappedType();
            var wrappedTypeDescriptor = wrappedType == typeof(T2)
                                            ? $"type '{typeof(T2)}'"
                                            : $"'{typeof(T2)}' subtype '{wrappedType}'";
            throw new InvalidCastException($"Cannot cast instance of {wrappedTypeDescriptor} to type '{typeof(T1)}'.");
        }
    }

    /// <summary>
    /// Casts the current union instance to type <typeparamref name="T2"/>.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NullReferenceException">
    /// The current union instance is the default, and <typeparamref name="T2"/> is a value type.
    /// </exception>
    /// <exception cref="InvalidCastException">The cast was invalid.</exception>
    [return: MaybeDefaultIfInstanceDefault]
    public T2 CastToT2()
    {
        if (IsDefault)
        {
            return Enums.HasFlag(ValueTypeFlags, TypeFlags2.T2)
                    ? throw new NullReferenceException(
                        $"Cannot cast default union to instance of value type '{typeof(T2)}'.")
                    : default!; // This is null since T2 is a reference type
        }

        else if (IsT2()) return UnsafeAsT2;

        else // Invalid cast
        {
            // Try to be as specific about what went wrong with the type conversion as possible
            var wrappedType = GetWrappedType();
            var wrappedTypeDescriptor = wrappedType == typeof(T1)
                                            ? $"type '{typeof(T1)}'"
                                            : $"'{typeof(T1)}' subtype '{wrappedType}'";
            throw new InvalidCastException($"Cannot cast instance of {wrappedTypeDescriptor} to type '{typeof(T2)}'.");
        }
    }
    #endregion

    #region Is
    /// <summary>
    /// Gets whether or not this union wraps an instance of <typeparamref name="T1"/>, returning the
    /// <typeparamref name="T1"/> instance in an <see langword="out"/> parameter if so.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool IsT1([MaybeNullWhen(false), MaybeDefaultWhen(false)] out T1 value)
    {
        if (IsT1())
        {
            value = UnsafeAsT1;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Gets whether or not this union wraps an instance of <typeparamref name="T1"/>.
    /// </summary>
    public bool IsT1() => Enums.HasFlag(_typeFlags, TypeFlags2.T1);

    /// <summary>
    /// Gets whether or not this union wraps an instance of <typeparamref name="T2"/>, returning the
    /// <typeparamref name="T2"/> instance in an <see langword="out"/> parameter if so.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool IsT2([MaybeNullWhen(false), MaybeDefaultWhen(false)] out T2 value)
    {
        if (IsT2())
        {
            value = UnsafeAsT2;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Gets whether or not this union wraps an instance of <typeparamref name="T2"/>.
    /// </summary>
    public bool IsT2() => Enums.HasFlag(_typeFlags, TypeFlags2.T2);
    #endregion
    #endregion
    #endregion
}
