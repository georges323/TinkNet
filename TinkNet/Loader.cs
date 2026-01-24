using System.Reflection;
using System.Runtime.Loader;

namespace TinkNet;

public static class Loader
{
    private static string? _dllPath;
    private static string? _basePath;

    public static Assembly Load(string dllPath)
    {
        if (!File.Exists(dllPath))
            throw new FileNotFoundException("DLL not found", dllPath);

        _dllPath = dllPath;
        _basePath = Path.GetDirectoryName(_dllPath);
        
        // We hook up AssemblyResolve when the CLR is requiring to find dll dependencies form the original
        // dll passed
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        return Assembly.LoadFrom(dllPath);
    }

    private static Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        if (_basePath == null) return null;

        // args.Name tells us the DLL missing
        var assemblyName = new AssemblyName(args.Name).Name;
        var assemblyPath = Path.Combine(_basePath, $"{assemblyName}.dll");

        if (!File.Exists(assemblyPath)) return null;

        return Assembly.LoadFrom(assemblyPath);
    }

    public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            // running GetTypes() forces the CLR to know about dependencies thus calling AssemblyResolve to load them
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return only the types that were successfully loaded
            return ex.Types.Where(t => t != null)!;
        }
    }

    public static IEnumerable<Assembly> GetAssemblyReferences()
    {
        var references = new List<Assembly>();

        if (_basePath == null) return references;

        foreach (var dll in Directory.GetFiles(_basePath, "*.dll"))
        {
            // Don't reload the main assembly or system ones we might have trouble with
            if (Path.GetFileName(dll).Equals(Path.GetFileName(_dllPath), StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                var refAssembly = Assembly.LoadFrom(dll);
                references.Add(refAssembly);
            }
            catch
            {
                // Ignore assembly if it can't be loaded
                continue;
            }
        }

        return references;
    }
}

