using System;
using System.Windows;

namespace YasGMP.Wpf.Resources;

/// <summary>
/// Resource dictionary that automatically reloads language-specific resources when the
/// <see cref="LocalizationManager"/> switches cultures.
/// </summary>
public class LocalizedResourceDictionary : ResourceDictionary
{
    public LocalizedResourceDictionary()
    {
        LocalizationManager.Register(this);
    }

    internal void ApplySource(Uri source)
    {
        if (!Equals(Source, source))
        {
            Source = source;
        }
    }
}
