// File: Helpers/ObjectMapper.cs
using System;
using System.Linq;
using System.Reflection;

namespace YasGMP.Helpers
{
    /// <summary>
    /// Lightweight reflection-based mapper for copying matching public properties
    /// from a source object to a new instance created by the generic MapTo method.
    /// </summary>
    public static class ObjectMapper
    {
        /// <summary>
        /// Maps public instance properties from <paramref name="source"/> onto a new <typeparamref name="TTarget"/> instance.
        /// Property names are matched case-insensitively. Compatible types are assigned directly; otherwise an attempt
        /// is made using <see cref="Convert.ChangeType(object, Type)"/>.
        /// </summary>
        /// <typeparam name="TTarget">Destination type with a public parameterless constructor.</typeparam>
        /// <param name="source">Source instance. Must not be <c>null</c>.</param>
        /// <returns>A new, populated instance of TTarget.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
        public static TTarget MapTo<TTarget>(this object source)
            where TTarget : class, new()
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var target = new TTarget();

            var sProps = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var tProps = typeof(TTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var tp in tProps.Where(p => p.CanWrite))
            {
                var sp = sProps.FirstOrDefault(p => p.Name.Equals(tp.Name, StringComparison.OrdinalIgnoreCase));
                if (sp is null)
                    continue;

                var val = sp.GetValue(source);
                if (val is null)
                    continue;

                if (tp.PropertyType.IsAssignableFrom(sp.PropertyType))
                {
                    tp.SetValue(target, val);
                }
                else
                {
                    try
                    {
                        var conv = Convert.ChangeType(val, tp.PropertyType);
                        tp.SetValue(target, conv);
                    }
                    catch
                    {
                        // Ignore inexact/inconvertible fields, preserving partial mapping safely.
                    }
                }
            }

            return target;
        }
    }
}
