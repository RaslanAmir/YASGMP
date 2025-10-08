using System.Globalization;
using Xunit;
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
}
