using System;
using System.Linq;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using Xunit;
using Xunit.Sdk;
using YasGMP.Wpf.Smoke.Helpers;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Smoke.Tests;

public class AssetsModuleSmokeTests
{
    [Fact]
    public async Task AssetsModule_ProvidesAttachmentAndSignaturePromptsAcrossFindAddUpdateAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new SkipException("FlaUI smoke automation requires Windows host.");
        }

        var localization = LocalizationTestContext.ResolveLocalizationService();

        await using var session = await WpfApplicationSession.LaunchAsync();
        var window = session.MainWindow;

        localization.SetLanguage("en");
        SmokeAutomationClient.SetLanguage(window, "en");
        session.WaitForIdle();

        OpenAssetsModule(window, localization, session);
        session.WaitForIdle();

        var statusAutomationId = localization.GetString("Shell.StatusBar.Status.Value.AutomationId");
        WaitForNonEmptyStatus(window, statusAutomationId);

        ToggleToolbarButton(window, localization.GetString("Module.Toolbar.Toggle.Find.AutomationId"));
        session.WaitForIdle();

        ToggleToolbarButton(window, localization.GetString("Module.Toolbar.Toggle.Add.AutomationId"));
        session.WaitForIdle();

        var addStatus = WaitForStatus(window, statusAutomationId, text =>
            text.StartsWith("Generated equipment code", StringComparison.Ordinal)
            && text.Contains(" and saved QR image to ", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(addStatus));

        PopulateRequiredFields(window, localization);
        session.WaitForIdle();

        InvokeToolbarButton(window, localization.GetString("Module.Toolbar.Button.Attach.AutomationId"));
        session.WaitForIdle();

        var expectedAttachment = localization.GetString("Module.Assets.Status.SaveBeforeAttachment");
        var attachmentStatus = WaitForStatus(window, statusAutomationId, text => string.Equals(text, expectedAttachment, StringComparison.Ordinal));
        Assert.Equal(expectedAttachment, attachmentStatus);

        InvokeToolbarButton(window, localization.GetString("Module.Toolbar.Command.Save.AutomationId"));
        session.WaitForIdle();

        HandleSignatureDialog(window, session, localization, confirm: false);
        session.WaitForIdle();

        var expectedCancelled = localization.GetString("Module.Assets.Status.SignatureCancelled");
        var cancelledStatus = WaitForStatus(window, statusAutomationId, text => string.Equals(text, expectedCancelled, StringComparison.Ordinal));
        Assert.Equal(expectedCancelled, cancelledStatus);

        InvokeToolbarButton(window, localization.GetString("Module.Toolbar.Command.Cancel.AutomationId"));
        session.WaitForIdle();

        var moduleTitle = localization.GetString("Module.Title.Assets");
        var expectedCancel = localization.GetString("Module.Status.Cancelled", moduleTitle);
        var cancelStatus = WaitForStatus(window, statusAutomationId, text => string.Equals(text, expectedCancel, StringComparison.Ordinal));
        Assert.Equal(expectedCancel, cancelStatus);

        SelectFirstAssetRecord(window, localization);
        session.WaitForIdle();

        ToggleToolbarButton(window, localization.GetString("Module.Toolbar.Toggle.Update.AutomationId"));
        session.WaitForIdle();

        AppendNotes(window, localization, "Smoke update");
        session.WaitForIdle();

        InvokeToolbarButton(window, localization.GetString("Module.Toolbar.Command.Save.AutomationId"));
        session.WaitForIdle();

        HandleSignatureDialog(window, session, localization, confirm: false);
        session.WaitForIdle();

        var cancelledUpdateStatus = WaitForStatus(window, statusAutomationId, text => string.Equals(text, expectedCancelled, StringComparison.Ordinal));
        Assert.Equal(expectedCancelled, cancelledUpdateStatus);

        InvokeToolbarButton(window, localization.GetString("Module.Toolbar.Command.Cancel.AutomationId"));
        session.WaitForIdle();

        var finalCancelStatus = WaitForStatus(window, statusAutomationId, text => string.Equals(text, expectedCancel, StringComparison.Ordinal));
        Assert.Equal(expectedCancel, finalCancelStatus);
    }

    private static void OpenAssetsModule(Window window, ILocalizationService localization, WpfApplicationSession session)
    {
        var modulesPaneId = localization.GetString("Dock.Modules.AutomationId");
        var modulesPane = WaitForElement(window, modulesPaneId);

        var maintenanceId = localization.GetString("ModuleTree.Category.Maintenance.AutomationId");
        var maintenanceNode = WaitForDescendant(modulesPane, maintenanceId, ControlType.TreeItem).AsTreeItem();
        if (maintenanceNode.Patterns.ExpandCollapse.IsSupported &&
            maintenanceNode.Patterns.ExpandCollapse.Pattern.ExpandCollapseState != ExpandCollapseState.Expanded)
        {
            maintenanceNode.Patterns.ExpandCollapse.Pattern.Expand();
            session.WaitForIdle();
        }

        var assetsNodeId = localization.GetString("ModuleTree.Node.Maintenance.Machines.AutomationId");
        var assetsNode = WaitForDescendant(maintenanceNode, assetsNodeId, ControlType.TreeItem);
        assetsNode.Focus();
        assetsNode.Patterns.SelectionItem?.Select();
        Keyboard.Type(VirtualKeyShort.RETURN);
        session.WaitForIdle();

        var gridId = localization.GetString("Module.Assets.Grid.AutomationId");
        WaitForElement(window, gridId, ControlType.DataGrid);
    }

    private static void PopulateRequiredFields(Window window, ILocalizationService localization)
    {
        SetText(window, localization.GetString("Module.Assets.Form.Name.AutomationId"), "Smoke Asset");
        SetText(window, localization.GetString("Module.Assets.Form.Manufacturer.AutomationId"), "Smoke Manufacturing");
        SetText(window, localization.GetString("Module.Assets.Form.Location.AutomationId"), "Smoke Lab");
        SetText(window, localization.GetString("Module.Assets.Form.Urs.AutomationId"), "URS-SMOKE-001");
    }

    private static void AppendNotes(Window window, ILocalizationService localization, string note)
    {
        var notesId = localization.GetString("Module.Assets.Form.Notes.AutomationId");
        var notesElement = WaitForElement(window, notesId, ControlType.Edit);
        var current = ReadText(notesElement);
        var updated = string.IsNullOrWhiteSpace(current) ? note : $"{current} {note}";
        SetText(notesElement, updated);
    }

    private static void SelectFirstAssetRecord(Window window, ILocalizationService localization)
    {
        var gridId = localization.GetString("Module.Assets.Grid.AutomationId");
        var grid = WaitForElement(window, gridId, ControlType.DataGrid);
        Retry.WhileTrue(
            () =>
            {
                var rows = grid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem));
                if (rows.Length == 0)
                {
                    return true;
                }

                rows[0].Patterns.SelectionItem?.Select();
                return false;
            },
            timeout: TimeSpan.FromSeconds(10),
            throwOnTimeout: true);
    }

    private static void HandleSignatureDialog(Window window, WpfApplicationSession session, ILocalizationService localization, bool confirm)
    {
        var title = localization.GetString("Dialog.ElectronicSignature.Title.ElectronicSignature");
        var dialog = Retry.WhileNull(
                () => window.ModalWindows.FirstOrDefault(w => string.Equals(w.Title, title, StringComparison.OrdinalIgnoreCase)),
                timeout: TimeSpan.FromSeconds(15),
                throwOnTimeout: true)
            .Result;

        if (dialog is null)
        {
            throw new InvalidOperationException("Electronic signature dialog did not appear.");
        }

        var passwordBox = dialog
            .FindAllDescendants(cf => cf.ByControlType(ControlType.Edit))
            .FirstOrDefault(e => e.Properties.IsPassword.TryGetValue(out var isPassword) && isPassword);
        if (passwordBox is not null)
        {
            SetText(passwordBox, "111");
        }

        var confirmId = localization.GetString("Dialog.ElectronicSignature.Button.Confirm.AutomationId");
        var cancelId = localization.GetString("Module.Toolbar.Command.Cancel.AutomationId");
        var targetId = confirm ? confirmId : cancelId;
        var button = dialog.FindFirstDescendant(cf => cf.ByAutomationId(targetId))?.AsButton();
        if (button is null)
        {
            throw new InvalidOperationException($"Unable to locate signature dialog button '{targetId}'.");
        }

        button.Invoke();
        Retry.WhileTrue(
            () => window.ModalWindows.Any(w => string.Equals(w.Title, title, StringComparison.OrdinalIgnoreCase)),
            timeout: TimeSpan.FromSeconds(10),
            throwOnTimeout: true);
    }

    private static void ToggleToolbarButton(Window window, string automationId)
    {
        var button = WaitForElement(window, automationId);
        if (button.Patterns.Toggle.IsSupported)
        {
            button.Patterns.Toggle.Pattern.Toggle();
        }
        else
        {
            button.AsButton().Invoke();
        }
    }

    private static void InvokeToolbarButton(Window window, string automationId)
    {
        var button = WaitForElement(window, automationId).AsButton();
        button.Invoke();
    }

    private static void SetText(AutomationElement element, string value)
    {
        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(value);
            return;
        }

        element.Focus();
        Keyboard.ShortPress(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(value);
    }

    private static void SetText(Window window, string automationId, string value)
    {
        var element = WaitForElement(window, automationId, ControlType.Edit);
        SetText(element, value);
    }

    private static string WaitForStatus(Window window, string automationId, Func<string, bool> predicate)
    {
        string? captured = null;
        Retry.WhileNull(
            () =>
            {
                var element = WaitForElement(window, automationId, ControlType.Text);
                var text = ReadText(element);
                if (predicate(text))
                {
                    captured = text;
                    return text;
                }

                return null;
            },
            timeout: TimeSpan.FromSeconds(15),
            throwOnTimeout: true);
        return captured!;
    }

    private static void WaitForNonEmptyStatus(Window window, string automationId)
    {
        WaitForStatus(window, automationId, text => !string.IsNullOrWhiteSpace(text));
    }

    private static AutomationElement WaitForElement(AutomationElement parent, string automationId, ControlType? controlType = null)
    {
        var cf = parent.Automation.ConditionFactory;
        return Retry.WhileNull(
                () => parent.FindFirstDescendant(controlType is null
                    ? cf.ByAutomationId(automationId)
                    : cf.ByAutomationId(automationId).And(cf.ByControlType(controlType.Value))),
                timeout: TimeSpan.FromSeconds(30),
                throwOnTimeout: true)
            .Result;
    }

    private static AutomationElement WaitForElement(Window window, string automationId, ControlType? controlType = null)
        => WaitForElement(window as AutomationElement, automationId, controlType);

    private static AutomationElement WaitForDescendant(AutomationElement parent, string automationId, ControlType? controlType = null)
        => WaitForElement(parent, automationId, controlType);

    private static string ReadText(AutomationElement element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        if (element.Patterns.Text.IsSupported)
        {
            var text = element.Patterns.Text.Pattern.DocumentRange.GetText(int.MaxValue);
            return text.TrimEnd('\r', '\n');
        }

        if (element.Patterns.Value.IsSupported)
        {
            return element.Patterns.Value.Pattern.Value ?? string.Empty;
        }

        return element.Name ?? string.Empty;
    }
}
