using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>Provides factories for module documents discoverable in the Modules pane.</summary>
public interface IModuleRegistry
{
    IReadOnlyList<ModuleMetadata> Modules { get; }

    ModuleDocumentViewModel CreateModule(string moduleKey);
}

public sealed class ModuleMetadata
{
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

/// <summary>Runtime implementation backed by DI factories.</summary>
public sealed class ModuleRegistry : IModuleRegistry
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, ModuleRegistration> _registrations = new(StringComparer.OrdinalIgnoreCase);

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

    public ModuleDocumentViewModel CreateModule(string moduleKey)
    {
        if (!_registrations.TryGetValue(moduleKey, out var registration))
        {
            throw new InvalidOperationException($"Module '{moduleKey}' is not registered.");
        }

        return (ModuleDocumentViewModel)_services.GetRequiredService(registration.ViewModelType);
    }

    public void Register<TViewModel>(string key, string title, string category, string? description = null)
        where TViewModel : ModuleDocumentViewModel
    {
        _registrations[key] = new ModuleRegistration(key, typeof(TViewModel), new ModuleMetadata(key, title, category, description));
    }

    private sealed record ModuleRegistration(string Key, Type ViewModelType, ModuleMetadata Metadata);
}

