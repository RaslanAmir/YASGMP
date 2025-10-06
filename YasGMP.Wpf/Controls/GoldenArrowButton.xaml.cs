using System;
using System.Windows;
using System.Windows.Controls;
using YasGMP.Common;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Controls;

public partial class GoldenArrowButton : Button
{
    private const string DefaultToolTip = "Open the related record.";
    private const string DefaultAutomationName = "Golden arrow navigation";
    private const string DefaultAutomationId = "Toolbar.Button.GoldenArrow";

    public static readonly DependencyProperty ToolTipKeyProperty = DependencyProperty.Register(
        nameof(ToolTipKey),
        typeof(string),
        typeof(GoldenArrowButton),
        new PropertyMetadata("Module.Toolbar.Button.GoldenArrow.ToolTip", OnLocalizationPropertyChanged));

    public static readonly DependencyProperty ToolTipFallbackProperty = DependencyProperty.Register(
        nameof(ToolTipFallback),
        typeof(string),
        typeof(GoldenArrowButton),
        new PropertyMetadata(DefaultToolTip, OnLocalizationPropertyChanged));

    public static readonly DependencyProperty AutomationNameKeyProperty = DependencyProperty.Register(
        nameof(AutomationNameKey),
        typeof(string),
        typeof(GoldenArrowButton),
        new PropertyMetadata("Module.Toolbar.Button.GoldenArrow.AutomationName", OnLocalizationPropertyChanged));

    public static readonly DependencyProperty AutomationNameFallbackProperty = DependencyProperty.Register(
        nameof(AutomationNameFallback),
        typeof(string),
        typeof(GoldenArrowButton),
        new PropertyMetadata(DefaultAutomationName, OnLocalizationPropertyChanged));

    public static readonly DependencyProperty AutomationIdKeyProperty = DependencyProperty.Register(
        nameof(AutomationIdKey),
        typeof(string),
        typeof(GoldenArrowButton),
        new PropertyMetadata("Module.Toolbar.Button.GoldenArrow.AutomationId", OnLocalizationPropertyChanged));

    public static readonly DependencyProperty AutomationIdProperty = DependencyProperty.Register(
        nameof(AutomationId),
        typeof(string),
        typeof(GoldenArrowButton),
        new PropertyMetadata(DefaultAutomationId, OnLocalizationPropertyChanged));

    private static readonly DependencyPropertyKey ResolvedToolTipPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ResolvedToolTip),
        typeof(string),
        typeof(GoldenArrowButton),
        new PropertyMetadata(DefaultToolTip));

    public static readonly DependencyProperty ResolvedToolTipProperty = ResolvedToolTipPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey ResolvedAutomationNamePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ResolvedAutomationName),
        typeof(string),
        typeof(GoldenArrowButton),
        new PropertyMetadata(DefaultAutomationName));

    public static readonly DependencyProperty ResolvedAutomationNameProperty = ResolvedAutomationNamePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey ResolvedAutomationIdPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ResolvedAutomationId),
        typeof(string),
        typeof(GoldenArrowButton),
        new PropertyMetadata(DefaultAutomationId));

    public static readonly DependencyProperty ResolvedAutomationIdProperty = ResolvedAutomationIdPropertyKey.DependencyProperty;

    private ILocalizationService? _localization;

    public GoldenArrowButton()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        UpdateLocalizedValues();
    }

    public string? ToolTipKey
    {
        get => (string?)GetValue(ToolTipKeyProperty);
        set => SetValue(ToolTipKeyProperty, value);
    }

    public string? ToolTipFallback
    {
        get => (string?)GetValue(ToolTipFallbackProperty);
        set => SetValue(ToolTipFallbackProperty, value);
    }

    public string? AutomationNameKey
    {
        get => (string?)GetValue(AutomationNameKeyProperty);
        set => SetValue(AutomationNameKeyProperty, value);
    }

    public string? AutomationNameFallback
    {
        get => (string?)GetValue(AutomationNameFallbackProperty);
        set => SetValue(AutomationNameFallbackProperty, value);
    }

    public string? AutomationIdKey
    {
        get => (string?)GetValue(AutomationIdKeyProperty);
        set => SetValue(AutomationIdKeyProperty, value);
    }

    public string? AutomationId
    {
        get => (string?)GetValue(AutomationIdProperty);
        set => SetValue(AutomationIdProperty, value);
    }

    public string? ResolvedToolTip => (string?)GetValue(ResolvedToolTipProperty);

    public string ResolvedAutomationName => (string)GetValue(ResolvedAutomationNameProperty);

    public string ResolvedAutomationId => (string)GetValue(ResolvedAutomationIdProperty);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachLocalizationService();
        UpdateLocalizedValues();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachLocalizationService();
    }

    private void AttachLocalizationService()
    {
        if (_localization is not null)
        {
            return;
        }

        _localization = ServiceLocator.GetService<ILocalizationService>();
        if (_localization is not null)
        {
            _localization.LanguageChanged += OnLanguageChanged;
        }
    }

    private void DetachLocalizationService()
    {
        if (_localization is null)
        {
            return;
        }

        _localization.LanguageChanged -= OnLanguageChanged;
        _localization = null;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(UpdateLocalizedValues);
            return;
        }

        UpdateLocalizedValues();
    }

    private static void OnLocalizationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GoldenArrowButton button)
        {
            button.UpdateLocalizedValues();
        }
    }

    private void UpdateLocalizedValues()
    {
        var localization = _localization ?? ServiceLocator.GetService<ILocalizationService>();

        var resolvedToolTip = ResolveLocalizedString(localization, ToolTipKey, ToolTipFallback, DefaultToolTip);
        var resolvedAutomationName = ResolveLocalizedString(localization, AutomationNameKey, AutomationNameFallback, DefaultAutomationName);
        var resolvedAutomationId = ResolveLocalizedString(localization, AutomationIdKey, AutomationId, DefaultAutomationId);

        if (string.IsNullOrWhiteSpace(resolvedAutomationName))
        {
            resolvedAutomationName = resolvedToolTip ?? DefaultAutomationName;
        }

        if (string.IsNullOrWhiteSpace(resolvedAutomationId))
        {
            resolvedAutomationId = resolvedAutomationName ?? DefaultAutomationId;
        }

        SetValue(ResolvedToolTipPropertyKey, resolvedToolTip);
        SetValue(ResolvedAutomationNamePropertyKey, resolvedAutomationName);
        SetValue(ResolvedAutomationIdPropertyKey, resolvedAutomationId);
    }

    private static string? ResolveLocalizedString(
        ILocalizationService? localization,
        string? key,
        string? fallback,
        string defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(key) && localization is not null)
        {
            var localized = localization.GetString(key);
            if (!string.IsNullOrWhiteSpace(localized) && !string.Equals(localized, key, StringComparison.Ordinal))
            {
                return localized;
            }
        }

        if (fallback is not null)
        {
            return fallback;
        }

        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        return defaultValue;
    }
}
