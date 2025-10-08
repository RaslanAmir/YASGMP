using System;
using System.Globalization;
using Xunit;
using YasGMP.Wpf.Resources;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class InspectorFieldTests
{
    [Fact]
    public void Create_ComposesAutomationIdFromSanitizedTokens()
    {
        var field = InspectorField.Create(
            moduleKey: "Quality & Compliance",
            moduleTitle: "Quality & Compliance",
            recordKey: "API Key #5",
            recordTitle: "API Key #5",
            label: "Status / State",
            value: "Masked");

        var expectedModule = AutomationIdSanitizer.Normalize("Quality & Compliance", "module");
        var expectedRecord = AutomationIdSanitizer.Normalize("API Key #5", "record");
        var expectedLabel = AutomationIdSanitizer.Normalize("Status / State", "field");
        var expectedAutomationId = string.Format(
            CultureInfo.InvariantCulture,
            "Dock.Inspector.{0}.{1}.{2}",
            expectedModule,
            expectedRecord,
            expectedLabel);

        Assert.Equal(expectedAutomationId, field.AutomationId);
    }

    [Fact]
    public void Create_WhenRecordKeyMissing_UsesSanitizedFallback()
    {
        var originalLanguage = LocalizationManager.CurrentLanguage;
        try
        {
            LocalizationManager.SetLanguage("en");

            var field = InspectorField.Create(
                moduleKey: "Audit",
                moduleTitle: "Audit Trail",
                recordKey: null,
                recordTitle: "Audit Entry #42",
                label: "Timestamp",
                value: "2025-01-15 08:30");

            var expectedModule = AutomationIdSanitizer.Normalize("Audit", "module");
            var expectedRecord = AutomationIdSanitizer.Normalize(null, "record");
            var expectedLabel = AutomationIdSanitizer.Normalize("Timestamp", "field");
            var expectedAutomationId = string.Format(
                CultureInfo.InvariantCulture,
                "Dock.Inspector.{0}.{1}.{2}",
                expectedModule,
                expectedRecord,
                expectedLabel);

            Assert.Equal(expectedAutomationId, field.AutomationId);
        }
        finally
        {
            LocalizationManager.SetLanguage(originalLanguage);
        }
    }

    [Fact]
    public void Create_ComposesAutomationMetadata_FromLocalizationTemplates()
    {
        var originalLanguage = LocalizationManager.CurrentLanguage;
        try
        {
            LocalizationManager.SetLanguage("en");

            var field = InspectorField.Create(
                moduleKey: "Quality",
                moduleTitle: "Quality & Compliance",
                recordKey: "API-123",
                recordTitle: "API Key 123",
                label: "Status",
                value: "Active");

            var moduleDisplay = "Quality & Compliance";
            var recordDisplay = "API Key 123";

            var expectedNameTemplate = GetTemplate("Dock.Inspector.Field.AutomationName.Template", "{0} — {1} ({2})");
            var expectedTooltipTemplate = GetTemplate("Dock.Inspector.Field.AutomationTooltip.Template", "{0} for {1} in {2}.");

            var expectedAutomationName = string.Format(CultureInfo.CurrentCulture, expectedNameTemplate, moduleDisplay, "Status", recordDisplay);
            var expectedAutomationTooltip = string.Format(CultureInfo.CurrentCulture, expectedTooltipTemplate, "Status", recordDisplay, moduleDisplay);

            Assert.Equal(expectedAutomationName, field.AutomationName);
            Assert.Equal(expectedAutomationTooltip, field.AutomationTooltip);
        }
        finally
        {
            LocalizationManager.SetLanguage(originalLanguage);
        }
    }

    [Fact]
    public void Create_WhenRecordMissing_UsesLocalizedFallbackInAutomationMetadata()
    {
        var originalLanguage = LocalizationManager.CurrentLanguage;
        try
        {
            LocalizationManager.SetLanguage("hr");

            var field = InspectorField.Create(
                moduleKey: "Audit",
                moduleTitle: "Revizija",
                recordKey: null,
                recordTitle: null,
                label: "Vrijeme",
                value: "08:30");

            var moduleDisplay = "Revizija";
            var recordFallback = GetTemplate("Dock.Inspector.Field.RecordFallback", "Record");
            var expectedNameTemplate = GetTemplate("Dock.Inspector.Field.AutomationName.Template", "{0} — {1} ({2})");
            var expectedTooltipTemplate = GetTemplate("Dock.Inspector.Field.AutomationTooltip.Template", "{0} for {1} in {2}.");

            var expectedAutomationName = string.Format(CultureInfo.CurrentCulture, expectedNameTemplate, moduleDisplay, "Vrijeme", recordFallback);
            var expectedAutomationTooltip = string.Format(CultureInfo.CurrentCulture, expectedTooltipTemplate, "Vrijeme", recordFallback, moduleDisplay);

            Assert.Equal(expectedAutomationName, field.AutomationName);
            Assert.Equal(expectedAutomationTooltip, field.AutomationTooltip);
        }
        finally
        {
            LocalizationManager.SetLanguage(originalLanguage);
        }
    }

    private static string GetTemplate(string key, string fallback)
    {
        var value = LocalizationManager.GetString(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal)
            ? fallback
            : value;
    }
}
