// ==============================================================================
//  File: Converters/PropertyOrFallbackConverter.cs
//  Project: YasGMP
//  Summary:
//      Resilient XAML value converter that extracts the first available value
//      from a list of candidate property/field/method names (with dotted paths),
//      using reflection with caching for performance. Designed to tolerate model
//      evolution across versions (renames, shape changes) without breaking XAML.
//
//      ✔ Case-insensitive property/field lookup
//      ✔ Supports dotted paths: "Parent.Name" or "Meta.Info.Code"
//      ✔ Understands parameterless getters: "GetCode" or candidate "Code" → "GetCode"
//      ✔ Works with IDictionary<string, object> and IReadOnlyDictionary<string, object>
//      ✔ Safely formats value types, DateTime, enums using InvariantCulture
//      ✔ Thread-safe reflection accessor cache (per-type, per-token)
//      ✔ One-way only (ConvertBack throws)
// ==============================================================================

#nullable enable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.Maui.Controls;

namespace YasGMP.Converters
{
    /// <summary>
    /// Converts an object to a string (or object) by trying several candidate member names in order,
    /// returning the first non-empty result.
    /// <para>
    /// Pass the candidates via <see cref="IValueConverter.Convert(object?, Type, object?, CultureInfo)"/>'s
    /// <c>parameter</c> as a <c>|</c>-separated list. Each candidate may be a simple name
    /// (e.g. <c>Code</c>) or a dotted path (e.g. <c>Parent.Name</c>).
    /// </para>
    /// <para>
    /// Example XAML:
    /// <code language="xml">
    /// &lt;Label
    ///   Text="{Binding ., Converter={StaticResource PropOrFallback},
    ///          ConverterParameter=Code|CompanyCode|ContractorCode|SupplierCode|InternalCode}" /&gt;
    /// </code>
    /// </para>
    /// <para>
    /// The converter tolerates model changes across versions and avoids XAML binding warnings such as
    /// XFC0045 by resolving at runtime. It first tries properties, then fields, then parameterless
    /// methods with the exact name, and finally a <c>Get&lt;Name&gt;</c> method (e.g., <c>GetCode</c>).
    /// </para>
    /// <b>One-way only:</b> <see cref="ConvertBack(object?, Type, object?, CultureInfo)"/> is not supported.
    /// </summary>
    public sealed class PropertyOrFallbackConverter : IValueConverter
    {
        /// <summary>Cache key for a member accessor (Type + token).</summary>
        private readonly record struct AccessKey(Type Type, string Token);

        /// <summary>
        /// Compiled accessors cache: (Type, token) → accessor delegate.
        /// Thread-safe and bounded by process lifetime.
        /// </summary>
        private static readonly ConcurrentDictionary<AccessKey, Func<object, object?>> s_accessorCache = new();

        /// <summary>
        /// Converts <paramref name="value"/> by attempting to evaluate the first non-empty candidate specified
        /// in <paramref name="parameter"/> (pipe-delimited list of names/paths).
        /// </summary>
        /// <param name="value">The bound object instance (may be <c>null</c>).</param>
        /// <param name="targetType">The desired target type (ignored).</param>
        /// <param name="parameter">
        /// A <see cref="string"/> containing candidate names separated by <c>|</c>, e.g. <c>"Code|CompanyCode"</c>.
        /// Each candidate can be a dotted path like <c>"Parent.Name"</c>.
        /// </param>
        /// <param name="culture">Culture (used when formatting numbers/dates; InvariantCulture is default).</param>
        /// <returns>
        /// The first non-empty formatted value if found; otherwise <see cref="string.Empty"/>.
        /// </returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return string.Empty;

            if (parameter is not string paramString || string.IsNullOrWhiteSpace(paramString))
                return string.Empty;

            // Split candidates by '|' and trim each
            var candidates = paramString.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (candidates.Length == 0)
                return string.Empty;

            // If binding is directly to a primitive/string, remember it as a last-resort fallback
            string? directString = FormatValue(value, culture);

            foreach (var candidate in candidates)
            {
                if (TryResolvePath(value, candidate, out var resolved))
                {
                    var formatted = FormatValue(resolved, culture);
                    if (!string.IsNullOrWhiteSpace(formatted))
                        return formatted!;
                }
            }

            return string.IsNullOrWhiteSpace(directString) ? string.Empty : directString;
        }

        /// <summary>Not supported (one-way only).</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException("PropertyOrFallbackConverter is one-way only.");

        // ======================================================================
        //                            Helper methods
        // ======================================================================

        /// <summary>
        /// Formats any value to a string using invariant culture where possible.
        /// Returns <c>null</c> when the value is <c>null</c>.
        /// </summary>
        private static string? FormatValue(object? value, CultureInfo culture)
        {
            if (value is null) return null;

            if (value is string s) return s;

            // Preferred path: IFormattable with invariant culture to keep UI stable across locales.
            if (value is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString();
        }

        /// <summary>
        /// Resolves a dotted path (e.g. "Parent.Name") or a single token against an object graph.
        /// </summary>
        private static bool TryResolvePath(object root, string path, out object? result)
        {
            result = root;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var tokens = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var token in tokens)
            {
                if (result is null) return false;

                if (!TryGetMemberValue(result, token, out result))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to get a single member value (property/field/dictionary/parameterless method) from <paramref name="instance"/>.
        /// </summary>
        private static bool TryGetMemberValue(object instance, string token, out object? value)
        {
            value = null;

            // IDictionary<string, object> support
            if (TryGetFromDictionary(instance, token, out value))
                return true;

            // Cached accessor (property/field/method)
            var key = new AccessKey(instance.GetType(), token);
            var accessor = s_accessorCache.GetOrAdd(key, CreateAccessor);

            if (accessor == EmptyAccessor)
                return false;

            value = accessor(instance);
            return true;
        }

        /// <summary>Tries dictionary-like reads by key (string).</summary>
        private static bool TryGetFromDictionary(object instance, string token, out object? value)
        {
            value = null;

            switch (instance)
            {
                case IDictionary dict:
                    if (dict.Contains(token))
                    {
                        value = dict[token];
                        return true;
                    }
                    break;

                case IDictionary<string, object> dictGeneric:
                    if (dictGeneric.TryGetValue(token, out value))
                        return true;
                    break;

                case IReadOnlyDictionary<string, object> roDict:
                    if (roDict.TryGetValue(token, out value))
                        return true;
                    break;
            }

            return false;
        }

        /// <summary>Represents a no-op accessor used to cache negative lookups.</summary>
        private static readonly Func<object, object?> EmptyAccessor = _ => null;

        /// <summary>Builds (and returns) an accessor delegate for the given (Type, token) pair.</summary>
        private static Func<object, object?> CreateAccessor(AccessKey key)
        {
            var type  = key.Type;
            var token = key.Token;

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;

            // 1) Property
            var prop = type.GetProperty(token, Flags);
            if (prop is not null && prop.CanRead)
            {
                return obj => { try { return prop.GetValue(obj); } catch { return null; } };
            }

            // 2) Field
            var field = type.GetField(token, Flags);
            if (field is not null)
            {
                return obj => { try { return field.GetValue(obj); } catch { return null; } };
            }

            // 3) Parameterless method with the same name
            var method = type.GetMethod(token, Flags, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (IsUsableGetter(method))
            {
                return obj => { try { return method!.Invoke(obj, null); } catch { return null; } };
            }

            // 4) Fallback to "Get{Token}" pattern (e.g., Code → GetCode)
            var getName   = "Get" + token;
            var getMethod = type.GetMethod(getName, Flags, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (IsUsableGetter(getMethod))
            {
                return obj => { try { return getMethod!.Invoke(obj, null); } catch { return null; } };
            }

            // Negative cache to avoid repeated reflection on misses
            return EmptyAccessor;
        }

        /// <summary>
        /// Determines whether a reflected method is a usable parameterless getter (non-void, non-async).
        /// </summary>
        private static bool IsUsableGetter(MethodInfo? method)
        {
            if (method is null) return false;
            if (method.ReturnType == typeof(void)) return false;
            if (typeof(System.Threading.Tasks.Task).IsAssignableFrom(method.ReturnType)) return false;
            if (method.GetParameters().Length != 0) return false;
            return true;
        }
    }
}
