using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Coordinates shell-level status bar and inspector broadcasts raised by module documents and ribbon commands.
/// </summary>
/// <remarks>
/// <para>
/// Ribbon controls and Golden Arrow components should forward user-facing status changes through this contract
/// on the UI dispatcher so the shell can refresh the docked status bar without racing background tasks.
/// </para>
/// <para>
/// Inspector updates are expected to originate from the active <see cref="ModuleDocumentViewModel"/> whenever the
/// selection changes, attachments mutate, or audit context needs to surface. Consumers should ensure the supplied
/// <see cref="InspectorContext"/> matches the module metadata registered in <see cref="IModuleRegistry"/> so the
/// inspector renders localization and audit traces that align with the currently activated module pane.
/// </para>
/// </remarks>
public interface IShellInteractionService
{
    /// <summary>
    /// Pushes a localized status message into the docked status bar, typically in response to ribbon command execution.
    /// </summary>
    /// <param name="message">Localized status string composed from <c>ShellStrings</c> resources.</param>
    void UpdateStatus(string message);

    /// <summary>
    /// Updates the inspector panel with module-specific metadata, audit trails, or Golden Arrow primers.
    /// </summary>
    /// <param name="context">
    /// Context payload produced by the active module document; should be captured on the UI dispatcher before invoking
    /// the shell service so bound dependency properties remain thread-affine.
    /// </param>
    void UpdateInspector(InspectorContext context);

    /// <summary>
    /// Launches a shell preview for the supplied document or asset path so modules can surface generated artifacts.
    /// </summary>
    /// <param name="path">Absolute file system path that should be opened with the registered handler.</param>
    void PreviewDocument(string path)
    {
        // Default implementations may ignore the request; concrete shells can override to launch external viewers.
    }
}

/// <summary>
/// Navigation contract used by Golden Arrow buttons, ribbon launchers, and Modules pane selection handlers to activate
/// related documents.
/// </summary>
/// <remarks>
/// <para>
/// Implementers should route navigation requests through the metadata registered in <see cref="IModuleRegistry"/> to keep
/// module instantiation, localization, and audit routing consistent with shell expectations.
/// </para>
/// <para>
/// Calls must occur on the WPF dispatcher thread; Golden Arrow callbacks often originate from background data observers and
/// need to marshal to the dispatcher before invoking <see cref="OpenModule"/> or <see cref="Activate"/> to avoid cross-thread
/// access violations.
/// </para>
/// </remarks>
public interface IModuleNavigationService
{
    /// <summary>
    /// Opens (or resolves) the module document associated with the supplied key, using <see cref="IModuleRegistry"/> metadata
    /// to instantiate the document when necessary.
    /// </summary>
    /// <param name="moduleKey">Registry key defined in <see cref="IModuleRegistry"/> that maps Golden Arrow sources to modules.</param>
    /// <param name="parameter">
    /// Optional navigation payload (e.g., selected record identifiers) that Golden Arrow buttons pass to prime the target module.
    /// </param>
    /// <returns>The module document instance representing the opened/activated pane.</returns>
    ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null);

    /// <summary>
    /// Brings an existing module document to the foreground within the docking manager and synchronizes inspector/status state.
    /// </summary>
    /// <param name="document">Document previously resolved from <see cref="OpenModule"/>.</param>
    void Activate(ModuleDocumentViewModel document);
}
