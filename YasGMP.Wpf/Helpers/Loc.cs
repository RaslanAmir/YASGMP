using System.Windows;

namespace YasGMP.Wpf.Helpers;

internal static class Loc
{
    public static string S(string key, string fallback)
    {
        try
        {
            var app = Application.Current;
            if (app?.Resources.Contains(key) == true)
            {
                if (app.Resources[key] is string s && !string.IsNullOrWhiteSpace(s))
                {
                    return s;
                }
            }
        }
        catch
        {
            // ignore lookup failures and fall back
        }

        return fallback;
    }
}

