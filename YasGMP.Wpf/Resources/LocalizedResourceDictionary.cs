using System.Collections;
using System.Globalization;
using System.Resources;
using System.Windows;

namespace YasGMP.Wpf.Resources;

/// <summary>
/// Resource dictionary that automatically reloads language-specific resources when the
/// <see cref="LocalizationManager"/> switches cultures.
/// </summary>
public class LocalizedResourceDictionary : ResourceDictionary
{
    /// <summary>
    /// Initializes a new instance of the LocalizedResourceDictionary class.
    /// </summary>
    public LocalizedResourceDictionary() => LocalizationManager.Register(this);

    internal void UpdateResources(ResourceManager resourceManager, CultureInfo culture)
    {
        Clear();

        var resourceSet = resourceManager.GetResourceSet(culture, true, true)
                         ?? resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);

        if (resourceSet is null)
        {
            return;
        }

        foreach (DictionaryEntry entry in resourceSet)
        {
            if (entry.Key is string key)
            {
                this[key] = entry.Value ?? string.Empty;
            }
        }
    }
}
