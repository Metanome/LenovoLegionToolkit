using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Station.Core;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.WPF.Station.Core;

public sealed class ExtensionManager : IExtensionManager
{
    private readonly ExtensionContextFactory _contextFactory;
    private readonly List<IExtensionProvider> _providers = [];

    public ExtensionManager(ExtensionContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public IReadOnlyCollection<IExtensionProvider> Providers => _providers.AsReadOnly();

    public bool HasProvider(string capability) =>
        _providers.Any(p => capability.Equals(p.GetData(nameof(ExtensionDataKey.Capability))));

    public string? GetProviderVersion(string capability) =>
        _providers.FirstOrDefault(p => capability.Equals(p.GetData(nameof(ExtensionDataKey.Capability))))
            ?.GetData(nameof(ExtensionDataKey.Version)) as string;

    public void Load()
    {
        var pluginRoot = Path.Combine(Folders.AppData, "Plugins");
        Log.Instance.Trace($"Starting extension discovery. BaseDirectory={Folders.AppData}");
        Log.Instance.Trace($"Expected plugin root: {pluginRoot}");

        if (!Directory.Exists(pluginRoot))
        {
            Log.Instance.Trace($"Plugin directory not found: {pluginRoot}");
            return;
        }

        var dlls = Directory.EnumerateFiles(pluginRoot, "*.dll", SearchOption.AllDirectories).ToArray();
        Log.Instance.Trace($"Discovered {dlls.Length} plugin assembly file(s)");

        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        try
        {
            foreach (var dll in dlls)
            {
                Log.Instance.Trace($"Discovered plugin candidate: {dll}");
                TryLoadAssemblyProviders(dll);
            }
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }

        Log.Instance.Trace($"Extension discovery completed. Loaded provider count: {_providers.Count}");
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var requestedName = new AssemblyName(args.Name);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name == requestedName.Name)
                return assembly;
        }
        return null;
    }

    public async Task StopAsync()
    {
        Log.Instance.Trace($"Stopping extension providers. Count={_providers.Count}");

        foreach (var provider in _providers)
        {
            try
            {
                Log.Instance.Trace($"Disposing provider: {provider.GetType().FullName}");
                await provider.DisposeAsync().ConfigureAwait(false);
                Log.Instance.Trace($"Disposed provider successfully: {provider.GetType().FullName}");
            }
            catch (Exception ex)
            {
                Log.Instance.ErrorReport($"Failed to dispose provider {provider.GetType().FullName}", ex);
                Log.Instance.Trace($"Failed to dispose provider {provider.GetType().FullName}", ex);
            }
        }

        _providers.Clear();
    }

    private void TryLoadAssemblyProviders(string assemblyPath)
    {
        try
        {
            Log.Instance.Trace($"Loading extension assembly: {assemblyPath}");

            var assembly = Assembly.LoadFrom(assemblyPath);

            Log.Instance.Trace($"Assembly loaded successfully: {assembly.FullName}");

            Type[] types;
            try
            {
                types = assembly.GetTypes();
                Log.Instance.Trace($"Assembly type scan succeeded. Type count={types.Length}");
            }
            catch (ReflectionTypeLoadException ex)
            {
                var loaderExceptions = ex.LoaderExceptions?
                    .Where(e => e is not null)
                    .Select(e => e!.Message)
                    .ToArray() ?? [];

                Log.Instance.ErrorReport($"Failed to enumerate types from assembly {assemblyPath}", ex);
                Log.Instance.Trace($"Failed to enumerate types from assembly {assemblyPath}. LoaderExceptions={string.Join(" | ", loaderExceptions)}", ex);
                return;
            }

            var providerTypes = types
                .Where(t => !t.IsAbstract && typeof(IExtensionProvider).IsAssignableFrom(t))
                .ToArray();

            Log.Instance.Trace($"Provider type scan completed. Provider count={providerTypes.Length}");

            if (providerTypes.Length == 0)
            {
                Log.Instance.Trace($"No IExtensionProvider implementations found in assembly: {assemblyPath}");
                return;
            }

            foreach (var providerType in providerTypes)
            {
                Log.Instance.Trace($"Creating provider instance: {providerType.FullName}");

                if (Activator.CreateInstance(providerType) is not IExtensionProvider provider)
                {
                    Log.Instance.Trace($"Activator returned null or incompatible instance for provider type: {providerType.FullName}");
                    continue;
                }

                Log.Instance.Trace($"Initializing provider: {providerType.FullName}");

                provider.Initialize(_contextFactory.Create(providerType.FullName ?? providerType.Name));

                _providers.Add(provider);

                var capability = provider.GetData(nameof(ExtensionDataKey.Capability)) as string;
                var version = provider.GetData(nameof(ExtensionDataKey.Version)) as string;
                var capabilityDisplay = string.IsNullOrEmpty(capability) ? "(none)" : capability;

                Log.Instance.Trace($"Loaded provider successfully: {providerType.FullName}, Version: {version}, Capabilities: {capabilityDisplay}, Total loaded providers: {_providers.Count}");
            }
        }
        catch (Exception ex)
        {
            Log.Instance.ErrorReport($"Failed to load extension assembly {assemblyPath}", ex);
            Log.Instance.Trace($"Failed to load extension assembly {assemblyPath}", ex);
        }
    }
}
