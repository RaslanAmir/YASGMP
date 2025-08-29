using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using YasGMP.Services.Logging;

namespace YasGMP.Services.Diagnostics
{
    /// <summary>
    /// UI Instrumentation – attaches lightweight event handlers to common controls as pages appear
    /// and emits detailed <see cref="ILogService"/> traces (button taps, text edits, picker changes…).
    /// Sensitive text inputs are masked.
    /// </summary>
    public sealed class UiInstrumentationService
    {
        private readonly ILogService _log;

        /// <summary>Creates a new UI instrumentation service.</summary>
        public UiInstrumentationService(ILogService log) => _log = log;

        /// <summary>
        /// Call once (after DI is ready), e.g. from <c>App</c> ctor.
        /// Wires shell navigation and application page-appearance hooks.
        /// </summary>
        public void Initialize(Application app)
        {
            try { _ = _log.InfoAsync("UI.Init", "UI instrumentation initialized"); } catch { /* non-fatal */ }

            if (Shell.Current is not null)
            {
                // Use Shell.Current.CurrentPage in MAUI (ShellNavigationState has no .Content).
                Shell.Current.Navigated += (_, __) =>
                {
                    var page = Shell.Current?.CurrentPage;
                    if (page is not null) AttachHandlers(page);
                };
            }

            app.PageAppearing += (_, p) =>
            {
                if (p is Page page) AttachHandlers(page);
            };
        }

        private void AttachHandlers(Page page)
        {
            try
            {
                TraverseAndHook(page);
                _ = _log.TraceAsync("UI.Nav", "PageAppeared", page.GetType().FullName ?? "Unknown");
            }
            catch (Exception ex)
            {
                _ = _log.ErrorAsync("UI.Hook", ex, "Failed to attach handlers");
            }
        }

        /// <summary>
        /// Depth-first traversal across typical MAUI visual tree shapes.
        /// Notes:
        /// • GestureRecognizers are on <see cref="View"/> (not <see cref="VisualElement"/>).
        /// • Many Children collections are <c>IList&lt;IView&gt;</c> – cast to <see cref="Element"/> before recursion.
        /// </summary>
        private void TraverseAndHook(Element root)
        {
            if (root is null) return;

            switch (root)
            {
                case Button b:
                    b.Clicked -= OnButtonClicked;
                    b.Clicked += OnButtonClicked;
                    break;

                case Entry e:
                    e.TextChanged -= OnEntryTextChanged;
                    e.TextChanged += OnEntryTextChanged;
                    e.Completed   -= OnEntryCompleted;
                    e.Completed   += OnEntryCompleted;
                    break;

                case Editor ed:
                    ed.TextChanged -= OnEditorTextChanged;
                    ed.TextChanged += OnEditorTextChanged;
                    break;

                case Picker pk:
                    pk.SelectedIndexChanged -= OnPickerChanged;
                    pk.SelectedIndexChanged += OnPickerChanged;
                    break;

                case Switch sw:
                    sw.Toggled -= OnSwitchToggled;
                    sw.Toggled += OnSwitchToggled;
                    break;

                case CheckBox cb:
                    cb.CheckedChanged -= OnCheckChanged;
                    cb.CheckedChanged += OnCheckChanged;
                    break;

                case RadioButton rb:
                    rb.CheckedChanged -= OnRadioChanged;
                    rb.CheckedChanged += OnRadioChanged;
                    break;

                case SearchBar sb:
                    sb.SearchButtonPressed -= OnSearchPressed;
                    sb.SearchButtonPressed += OnSearchPressed;
                    sb.TextChanged         -= OnSearchTextChanged;
                    sb.TextChanged         += OnSearchTextChanged;
                    break;

                // Gesture recognizers live on View (not VisualElement).
                case View v:
                    foreach (var gr in v.GestureRecognizers.OfType<TapGestureRecognizer>())
                    {
                        gr.Tapped -= OnTapped;
                        gr.Tapped += OnTapped;
                    }
                    break;
            }

            // Recurse into common containers (IView → Element before recursion)
            if (root is ContentPage cp && cp.Content is Element cpChild)
            {
                TraverseAndHook(cpChild);
            }
            else if (root is ContentView cv && cv.Content is Element cvChild)
            {
                TraverseAndHook(cvChild);
            }
            else if (root is ScrollView sv && sv.Content is Element svChild)
            {
                TraverseAndHook(svChild);
            }
            else if (root is Layout layout)
            {
                foreach (var v in layout.Children)
                    if (v is Element el) TraverseAndHook(el);
            }
            else if (root is Grid grid)
            {
                foreach (var v in grid.Children)
                    if (v is Element el) TraverseAndHook(el);
            }
        }

        // ===== Handlers =====

        private void OnButtonClicked(object? sender, EventArgs e)
        {
            if (sender is not Button b) return;
            var label = string.IsNullOrWhiteSpace(b.Text) ? b.AutomationId ?? "(Button)" : b.Text;
            _ = _log.TraceAsync("UI.Button", "Clicked", label, new Dictionary<string, object>
            {
                ["page"]  = b.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]    = b.AutomationId ?? string.Empty,
                ["class"] = b.GetType().Name
            });
        }

        private void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry) return;
            var masked = entry.IsPassword ? Mask(e.NewTextValue) : e.NewTextValue;
            _ = _log.TraceAsync("UI.Entry", "TextChanged", masked ?? string.Empty, new Dictionary<string, object>
            {
                ["page"]  = entry.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]    = entry.AutomationId ?? string.Empty,
                ["class"] = entry.GetType().Name
            });
        }

        private void OnEntryCompleted(object? sender, EventArgs e)
        {
            if (sender is not Entry entry) return;
            _ = _log.TraceAsync("UI.Entry", "Completed", "(Enter)", new Dictionary<string, object>
            {
                ["page"] = entry.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]   = entry.AutomationId ?? string.Empty
            });
        }

        private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not Editor ed) return;
            var sample = e.NewTextValue is null ? string.Empty
                        : e.NewTextValue.Length <= 120 ? e.NewTextValue
                        : e.NewTextValue.Substring(0, 120) + $" (+{e.NewTextValue.Length - 120} more chars)";
            _ = _log.TraceAsync("UI.Editor", "TextChanged", sample, new Dictionary<string, object>
            {
                ["page"] = ed.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]   = ed.AutomationId ?? string.Empty
            });
        }

        private void OnPickerChanged(object? sender, EventArgs e)
        {
            if (sender is not Picker pk) return;
            var value = pk.SelectedItem?.ToString() ?? $"Index={pk.SelectedIndex}";
            _ = _log.TraceAsync("UI.Picker", "SelectedIndexChanged", value, new Dictionary<string, object>
            {
                ["page"] = pk.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]   = pk.AutomationId ?? string.Empty
            });
        }

        private void OnSwitchToggled(object? sender, ToggledEventArgs e)
        {
            if (sender is not Switch sw) return;
            _ = _log.TraceAsync("UI.Switch", "Toggled", e.Value ? "On" : "Off", new Dictionary<string, object>
            {
                ["page"] = sw.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]   = sw.AutomationId ?? string.Empty
            });
        }

        private void OnCheckChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (sender is not CheckBox cb) return;
            _ = _log.TraceAsync("UI.CheckBox", "CheckedChanged", e.Value ? "Checked" : "Unchecked", new Dictionary<string, object>
            {
                ["page"] = cb.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]   = cb.AutomationId ?? string.Empty
            });
        }

        private void OnRadioChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (sender is not RadioButton rb) return;
            _ = _log.TraceAsync("UI.RadioButton", "CheckedChanged", e.Value ? "Checked" : "Unchecked", new Dictionary<string, object>
            {
                ["page"] = rb.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]   = rb.AutomationId ?? string.Empty
            });
        }

        private void OnSearchPressed(object? sender, EventArgs e)
        {
            if (sender is not SearchBar sb) return;
            _ = _log.TraceAsync("UI.SearchBar", "SearchPressed", sb.Text ?? string.Empty, new Dictionary<string, object>
            {
                ["page"] = sb.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]   = sb.AutomationId ?? string.Empty
            });
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not SearchBar sb) return;
            _ = _log.TraceAsync("UI.SearchBar", "TextChanged", e.NewTextValue ?? string.Empty, new Dictionary<string, object>
            {
                ["page"] = sb.GetContainingPage()?.GetType().FullName ?? string.Empty,
                ["id"]   = sb.AutomationId ?? string.Empty
            });
        }

        private void OnTapped(object? sender, EventArgs e)
        {
            _ = _log.TraceAsync("UI.Gesture", "Tapped", "(tap)");
        }

        private static string Mask(string? s) =>
            string.IsNullOrEmpty(s) ? string.Empty : new string('•', Math.Min(12, s.Length));
    }

    internal static class ElementExtensions
    {
        /// <summary>Returns the <see cref="Page"/> that contains <paramref name="e"/> (or <c>null</c> if none).</summary>
        public static Page? GetContainingPage(this Element e)
        {
            while (e is not null && e is not Page) e = e.Parent;
            return e as Page;
        }
    }
}
