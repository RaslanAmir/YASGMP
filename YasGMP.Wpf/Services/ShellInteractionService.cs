using System;
using System.Diagnostics;
using System.IO;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Default shell interaction service that wires module documents, status updates, and Golden Arrow navigation into the WPF host.
/// </summary>
/// <remarks>
/// <para>
/// The shell bootstrapping sequence must call <see cref="Configure"/> once after constructing the docking manager, ribbon,
/// and Modules pane. The supplied delegates typically originate from <see cref="IModuleRegistry"/> metadata so navigation and
/// inspector payloads mirror the registered module keys, localization resources, and audit routing expectations.
/// </para>
/// <para>
/// All callbacks are expected to execute on the WPF dispatcher thread. Ribbon and Golden Arrow handlers should marshal to the
/// dispatcher before invoking navigation methods to avoid cross-thread access when docking documents or updating dependency
/// properties.
/// </para>
/// <para>
/// Inspector updates carry localization and audit metadata, allowing downstream consumers to propagate language changes and
/// audit traces through the inspector pane without rehydrating module state. Status updates follow the same pattern so localized
/// audit/a11y messages appear immediately after module operations complete.
/// </para>
/// <para>
/// Consumers should always retrieve modules through <see cref="OpenModule"/> and activate them with <see cref="Activate"/>
/// rather than caching view-model references. This ensures each navigation request respects current registry metadata, safely
/// reuses existing documents, and keeps audit/inspector synchronization consistent with the shell lifecycle.
/// </para>
/// </remarks>
public sealed class ShellInteractionService : IShellInteractionService, IModuleNavigationService
{
    private Func<string, object?, ModuleDocumentViewModel>? _openModule;
    private Action<DocumentViewModel>? _activate;
    private Action<string>? _statusUpdater;
    private Action<InspectorContext>? _inspectorUpdater;
    private Func<DocumentViewModel, bool, DocumentViewModel>? _openDocument;
    private Action<DocumentViewModel>? _closeDocument;

    /// <summary>
    /// Captures shell delegates for module creation/activation and status/inspector broadcasting; must be invoked during shell startup.
    /// </summary>
    /// <param name="openModule">
    /// Delegate that resolves module documents by key, usually derived from <see cref="IModuleRegistry"/> entries.
    /// </param>
    /// <param name="activate">Callback that brings a module document to the foreground within the docking host.</param>
    /// <param name="statusUpdater">Dispatcher-affine status bar update handler.</param>
    /// <param name="inspectorUpdater">Dispatcher-affine inspector update handler.</param>
    public void Configure(
        Func<string, object?, ModuleDocumentViewModel> openModule,
        Action<DocumentViewModel> activate,
        Action<string> statusUpdater,
        Action<InspectorContext> inspectorUpdater,
        Func<DocumentViewModel, bool, DocumentViewModel> openDocument,
        Action<DocumentViewModel> closeDocument)
    {
        _openModule = openModule ?? throw new ArgumentNullException(nameof(openModule));
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _statusUpdater = statusUpdater ?? throw new ArgumentNullException(nameof(statusUpdater));
        _inspectorUpdater = inspectorUpdater ?? throw new ArgumentNullException(nameof(inspectorUpdater));
        _openDocument = openDocument ?? throw new ArgumentNullException(nameof(openDocument));
        _closeDocument = closeDocument ?? throw new ArgumentNullException(nameof(closeDocument));
    }

    /// <inheritdoc />
    public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
    {
        if (_openModule == null)
        {
            throw new InvalidOperationException("Shell interaction service not configured.");
        }

        var document = _openModule(moduleKey, parameter);
        return document;
    }

    /// <inheritdoc />
    public void Activate(ModuleDocumentViewModel document)
    {
        if (_activate == null)
        {
            throw new InvalidOperationException("Shell interaction service not configured.");
        }

        _activate(document);
    }

    /// <inheritdoc />
    public void UpdateStatus(string message)
    {
        if (_statusUpdater == null)
        {
            return;
        }

        _statusUpdater(message);
    }

    /// <inheritdoc />
    public void UpdateInspector(InspectorContext context)
    {
        if (_inspectorUpdater == null)
        {
            return;
        }

        _inspectorUpdater(context);
    }

    /// <inheritdoc />
    public DocumentViewModel OpenDocument(DocumentViewModel document, bool activate = true)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (_openDocument is null)
        {
            throw new InvalidOperationException("Shell interaction service not configured.");
        }

        var added = _openDocument(document, activate);
        return added;
    }

    /// <inheritdoc />
    public void CloseDocument(DocumentViewModel document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (_closeDocument is null)
        {
            throw new InvalidOperationException("Shell interaction service not configured.");
        }

        _closeDocument(document);
    }

    /// <inheritdoc />
    public void PreviewDocument(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Preview path must be provided.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Preview document was not found.", path);
        }

        var startInfo = new ProcessStartInfo(path)
        {
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }
}
