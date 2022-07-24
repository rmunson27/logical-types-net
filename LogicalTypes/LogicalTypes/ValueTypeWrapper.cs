using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rem.Core.ComponentModel.Logical;

/// <summary>
/// Wraps an instance of a value type.
/// </summary>
/// <remarks>
/// This class does not constrain its type parameter, but should only be used internally on value types.
/// </remarks>
/// <typeparam name="T"></typeparam>
internal sealed class ValueTypeWrapper<T>
{
    /// <summary>
    /// The value wrapped in this instance.
    /// </summary>
    public readonly T Value;

    /// <summary>
    /// Creates a new <see cref="ValueTypeWrapper{T}"/> wrapping the supplied value.
    /// </summary>
    /// <param name="Value"></param>
    public ValueTypeWrapper(T Value) { this.Value = Value; }
}
