using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Exposes module registrations whose keys, localized titles, and categories hydrate the Modules pane,
/// localization lookups, and Golden Arrow automation identifiers.
/// </summary>
/// <remarks>
/// The shell queries this registry when composing navigation chrome: the left-hand Modules tree binds to
/// <see cref="Modules"/> for grouping and caption resources, the ribbon/backstage retrieve localization
/// resource keys from the registered titles, and Golden Arrow automation identifiers are derived from each
/// module key so automation suites can locate shell entry points.
/// During navigation the shell resolves module documents through <see cref="CreateModule"/>, enabling SAP B1
/// behaviors—form-mode transitions, toolbar enablement, and inspector population—to align with the
/// registered metadata.
/// </remarks>
public interface IModuleRegistry
{
    IReadOnlyList<ModuleMetadata> Modules { get; }

    ModuleDocumentViewModel CreateModule(string moduleKey);
}

/// <summary>
/// Captures module metadata consumed by the Modules pane, localization services, and automation id mapping.
/// </summary>
/// <remarks>
/// Shell components read <see cref="Key"/>, <see cref="Title"/>, and <see cref="Category"/> to populate the
/// navigation tree, resolve <c>ShellStrings</c> resource keys for translated captions/tooltips, and compose
/// automation identifiers used by Golden Arrow navigation and SAP B1 toolbar wiring.
/// </remarks>
public sealed class ModuleMetadata
{
    /// <summary>
    /// Initializes a new metadata instance that drives Modules pane grouping, localization lookups, and
    /// automation identifiers for SAP B1 behaviors.
    /// </summary>
    /// <param name="key">
    /// Unique module key that doubles as the Golden Arrow automation suffix and DI resolution token;
    /// expected to match the resource key prefix within <c>ShellStrings</c>.
    /// </param>
    /// <param name="title">
    /// Localization resource key whose translated values appear in the Modules pane, ribbon navigation,
    /// and automation name bindings.
    /// </param>
    /// <param name="category">
    /// Localization resource key representing the grouping header for the Modules pane and ribbon backstage
    /// sections; also influences SAP B1 form-mode enablement scopes.
    /// </param>
    /// <param name="description">Optional localization key surfaced in tooltips and inspector primers.</param>
    public ModuleMetadata(string key, string title, string category, string? description = null)
    {
        Key = key;
        Title = title;
        Category = category;
        Description = description ?? string.Empty;
    }

    public string Key { get; }

    public string Title { get; }

    public string Category { get; }

    public string Description { get; }
}

/// <summary>
/// Runtime registry backed by dependency-injection factories that surface module metadata to shell components.
/// </summary>
/// <remarks>
/// The shell's navigation services query this registry during startup and Golden Arrow jumps to hydrate the
/// Modules pane, localize category/title text, and wire SAP B1 toolbar enablement based on the registered
/// metadata. When navigation targets a module the registry resolves the backing <see cref="ModuleDocumentViewModel"/>
/// via DI so form-mode toggles, toolbar commands, and status messaging stay in sync with the metadata-driven
/// shell chrome.
/// </remarks>
public sealed class ModuleRegistry : IModuleRegistry
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, ModuleRegistration> _registrations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a registry that resolves module document instances through the shell service provider.
    /// </summary>
    /// <param name="services">Shell-level service provider used to instantiate module document view-models.</param>
    public ModuleRegistry(IServiceProvider services)
    {
        _services = services;
    }

    public IReadOnlyList<ModuleMetadata> Modules
        => _registrations.Values
            .Select(r => r.Metadata)
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Title)
            .ToList();

    /// <summary>
    /// Instantiates a module document for the supplied key so navigation and Golden Arrow actions can display it.
    /// </summary>
    /// <param name="moduleKey">
    /// Registry key sourced from <see cref="ModuleMetadata.Key"/>; expected to align with localization resource
    /// prefixes and Golden Arrow automation identifiers.
    /// </param>
    public ModuleDocumentViewModel CreateModule(string moduleKey)
    {
        if (!_registrations.TryGetValue(moduleKey, out var registration))
        {
            throw new InvalidOperationException($"Module '{moduleKey}' is not registered.");
        }

        return (ModuleDocumentViewModel)_services.GetRequiredService(registration.ViewModelType);
    }

    /// <summary>
    /// Registers a module document factory and associated metadata for Modules pane, localization, and automation wiring.
    /// </summary>
    /// <typeparam name="TViewModel">Concrete <see cref="ModuleDocumentViewModel"/> resolved when the module is activated.</typeparam>
    /// <param name="key">
    /// Unique module identifier used for DI lookup, Golden Arrow automation ids, and localization key prefixes.
    /// </param>
    /// <param name="title">
    /// Localization resource key representing the module caption displayed in the Modules pane and ribbon navigation.
    /// </param>
    /// <param name="category">
    /// Localization resource key for the grouping header that clusters modules in the Modules pane and backstage.
    /// </param>
    /// <param name="description">
    /// Optional localization resource key surfaced in Modules pane tooltips and shell inspector primers to guide SAP B1
    /// form-mode transitions.
    /// </param>
    public void Register<TViewModel>(string key, string title, string category, string? description = null)
        where TViewModel : ModuleDocumentViewModel
    {
        _registrations[key] = new ModuleRegistration(key, typeof(TViewModel), new ModuleMetadata(key, title, category, description));
    }

    private sealed record ModuleRegistration(string Key, Type ViewModelType, ModuleMetadata Metadata);
}
