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

public sealed class ExtensionManager(ExtensionContextFactory contextFactory) : IExtensionManager
{
    private readonly List<IExtensionProvider> _providers = [];
    private string _pluginRoot = Path.Combine(Folders.AppData, "Plugins");

    public IReadOnlyCollection<IExtensionProvider> Providers => _providers.AsReadOnly();

    public bool HasProvider(string capability) =>
        _providers.Any(p => capability.Equals(p.GetData(nameof(ExtensionDataKey.Capability))));

    public string? GetProviderVersion(string capability) =>
        _providers.FirstOrDefault(p => capability.Equals(p.GetData(nameof(ExtensionDataKey.Capability))))
            ?.GetData(nameof(ExtensionDataKey.Version)) as string;

    public void Load()
    {
        if (!Directory.Exists(_pluginRoot))
        {
            var singularRoot = Path.Combine(Folders.AppData, "Plugin");
            if (Directory.Exists(singularRoot))
            {
                _pluginRoot = singularRoot;
            }
        }

        Log.Instance.Trace($"Starting extension discovery. PluginRoot={_pluginRoot}");

        if (!Directory.Exists(_pluginRoot))
        {
            Log.Instance.Trace($"Plugin directory not found: {_pluginRoot}");
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        var discovered = new List<PluginDiscoveryInfo>();

        try
        {
            var dlls = new List<string>();

            var depDir = Path.Combine(_pluginRoot, "Dependency");
            if (Directory.Exists(depDir))
            {
                var depDlls = Directory.EnumerateFiles(depDir, "*.dll", SearchOption.TopDirectoryOnly).ToArray();
                Log.Instance.Trace($"Discovered {depDlls.Length} plugin assembly file(s) in Dependency.");
                dlls.AddRange(depDlls);
            }

            var rootDlls = Directory.EnumerateFiles(_pluginRoot, "*.dll", SearchOption.TopDirectoryOnly).ToArray();
            Log.Instance.Trace($"Discovered {rootDlls.Length} plugin assembly file(s) in root.");
            dlls.AddRange(rootDlls);

            foreach (var subDir in Directory.EnumerateDirectories(_pluginRoot))
            {
                var folderName = Path.GetFileName(subDir);
                if (string.Equals(folderName, "Dependency", StringComparison.OrdinalIgnoreCase))
                    continue;

                var candidateDll = Path.Combine(subDir, $"{folderName}.dll");
                if (File.Exists(candidateDll))
                {
                    Log.Instance.Trace($"Discovered nested plugin assembly file matching folder name: {candidateDll}");
                    dlls.Add(candidateDll);
                }
            }

            foreach (var dll in dlls)
            {
                Log.Instance.Trace($"Discovered plugin candidate: {dll}");
                TryLoadAssemblyProviderTypes(dll, discovered);
            }

            if (discovered.Count == 0)
            {
                Log.Instance.Trace($"No providers discovered.");
                return;
            }
            Log.Instance.Trace($"Phase 1 complete: {discovered.Count} provider(s) discovered.");

            var sorted = TopologicalSort(discovered);
            Log.Instance.Trace($"Phase 2 complete: {sorted.Count} provider(s) after dependency resolution.");

            foreach (var info in sorted)
            {
                try
                {
                    Log.Instance.Trace($"Initializing provider: {info.TypeName} (capability={info.Capability ?? "(none)"})");
                    info.Provider.Initialize(contextFactory.Create(info.TypeName));
                    _providers.Add(info.Provider);

                    var capability = info.Capability;
                    var capabilityDisplay = string.IsNullOrEmpty(capability) ? "(none)" : capability;
                    Log.Instance.Trace($"Loaded provider successfully: {info.TypeName}, Capabilities: {capabilityDisplay}, Total loaded providers: {_providers.Count}");
                }
                catch (Exception ex)
                {
                    Log.Instance.ErrorReport($"Failed to initialize provider {info.TypeName}", ex);
                    Log.Instance.Trace($"Failed to initialize provider {info.TypeName}", ex);
                }
            }
            Log.Instance.Trace($"Phase 3 complete. Total loaded: {_providers.Count}");
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }

        Log.Instance.Trace($"Extension discovery completed. Loaded provider count: {_providers.Count}");
    }

    private Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var requestedName = new AssemblyName(args.Name);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name == requestedName.Name)
            {
                return assembly;
            }
        }

        if (args.RequestingAssembly != null && !args.RequestingAssembly.IsDynamic)
        {
            try
            {
                var requestingLocation = args.RequestingAssembly.Location;
                if (!string.IsNullOrEmpty(requestingLocation))
                {
                    var requestingDir = Path.GetDirectoryName(requestingLocation);
                    if (!string.IsNullOrEmpty(requestingDir))
                    {
                        var candidatePath = Path.Combine(requestingDir, $"{requestedName.Name}.dll");
                        if (File.Exists(candidatePath))
                        {
                            Log.Instance.Trace($"Resolving assembly '{requestedName.Name}' from requesting assembly directory: {candidatePath}");
                            return Assembly.LoadFrom(candidatePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Trace($"Failed to resolve assembly from requesting assembly directory: {ex.Message}", ex);
            }
        }

        var depDir = Path.Combine(_pluginRoot, "Dependency");
        if (Directory.Exists(depDir))
        {
            var candidatePath = Path.Combine(depDir, $"{requestedName.Name}.dll");
            if (File.Exists(candidatePath))
            {
                Log.Instance.Trace($"Resolving assembly '{requestedName.Name}' from Dependency folder: {candidatePath}");
                try
                {
                    return Assembly.LoadFrom(candidatePath);
                }
                catch (Exception ex)
                {
                    Log.Instance.Trace($"Failed to load assembly from Dependency folder {candidatePath}: {ex.Message}", ex);
                }
            }
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

    private void TryLoadAssemblyProviderTypes(string assemblyPath, List<PluginDiscoveryInfo> discovered)
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
                Log.Instance.Trace($"Creating provider instance (Phase 1): {providerType.FullName}");

                if (Activator.CreateInstance(providerType) is not IExtensionProvider provider)
                {
                    Log.Instance.Trace($"Activator returned null or incompatible instance for provider type: {providerType.FullName}");
                    continue;
                }

                var typeName = providerType.FullName ?? providerType.Name;
                discovered.Add(new PluginDiscoveryInfo
                {
                    Provider = provider,
                    TypeName = typeName
                });

                Log.Instance.Trace($"Provider instance created (not initialized): {typeName}");
            }
        }
        catch (Exception ex)
        {
            Log.Instance.ErrorReport($"Failed to load extension assembly {assemblyPath}", ex);
            Log.Instance.Trace($"Failed to load extension assembly {assemblyPath}", ex);
        }
    }

    private List<PluginDiscoveryInfo> TopologicalSort(List<PluginDiscoveryInfo> discovered)
    {
        Log.Instance.Trace($"Starting Phase 2: dependency resolution. Provider count={discovered.Count}");

        var capToIdx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < discovered.Count; i++)
        {
            var info = discovered[i];

            try
            {
                info.Capability = info.Provider.GetData(nameof(ExtensionDataKey.Capability)) as string;
            }
            catch (Exception ex)
            {
                Log.Instance.Trace($"Provider {info.TypeName}: GetData(Capability) threw, treating as no capability.", ex);
                info.Capability = null;
            }

            try
            {
                info.Dependencies = (info.Provider.GetData(nameof(ExtensionDataKey.Dependencies)) as string[]) ?? [];
            }
            catch (Exception ex)
            {
                Log.Instance.Trace($"Provider {info.TypeName}: GetData(Dependencies) threw, treating as no dependencies.", ex);
                info.Dependencies = [];
            }

            if (!string.IsNullOrEmpty(info.Capability))
            {
                if (capToIdx.ContainsKey(info.Capability))
                {
                    Log.Instance.Trace($"Duplicate capability '{info.Capability}' detected. Provider {info.TypeName} will shadow earlier registration.");
                }
                capToIdx[info.Capability] = i;
            }

            Log.Instance.Trace($"Provider [{info.TypeName}]: Capability={info.Capability ?? "(none)"}, Dependencies={string.Join(", ", info.Dependencies)}");

            discovered[i] = info;
        }

        int n = discovered.Count;
        var adj = new List<int>[n];
        var inDegree = new int[n];
        for (int i = 0; i < n; i++)
        {
            adj[i] = [];
        }

        var missingDeps = new HashSet<int>();

        for (int j = 0; j < n; j++)
        {
            foreach (var depCapability in discovered[j].Dependencies ?? [])
            {
                if (capToIdx.TryGetValue(depCapability, out int i))
                {
                    adj[i].Add(j);
                    inDegree[j]++;
                }
                else
                {
                    Log.Instance.Trace($"Warning: Provider {discovered[j].TypeName} depends on capability '{depCapability}' which is not provided by any discovered plugin.");
                    missingDeps.Add(j);
                }
            }
        }

        var queue = new Queue<int>();
        for (int i = 0; i < n; i++)
        {
            if (inDegree[i] == 0)
            {
                queue.Enqueue(i);
            }
        }

        var sorted = new List<PluginDiscoveryInfo>();
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            sorted.Add(discovered[i]);

            foreach (int j in adj[i])
            {
                inDegree[j]--;
                if (inDegree[j] == 0)
                {
                    queue.Enqueue(j);
                }
            }
        }

        if (sorted.Count < n)
        {
            var unresolved = new HashSet<int>();
            for (int i = 0; i < n; i++)
            {
                if (inDegree[i] > 0)
                {
                    unresolved.Add(i);
                }
            }

            Log.Instance.Trace($"Cycle detected in dependency graph. {unresolved.Count} provider(s) involved: {string.Join(", ", unresolved.Select(i => discovered[i].TypeName))}");

            foreach (int i in unresolved)
            {
                Log.Instance.ErrorReport(
                    $"Provider {discovered[i].TypeName} is part of a dependency cycle and will not be loaded.",
                    new InvalidOperationException("Circular dependency detected"));
            }
        }

        foreach (int j in missingDeps)
        {
            if (sorted.Contains(discovered[j]))
            {
                Log.Instance.Trace($"Note: Provider {discovered[j].TypeName} was loaded with one or more unresolved dependencies.");
            }
        }

        Log.Instance.Trace($"Dependency resolution complete. Sorted {sorted.Count} provider(s), {n - sorted.Count} skipped (cycles).");
        return sorted;
    }
}
