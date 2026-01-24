using System.Reflection;

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

    public static IEnumerable<Assembly> GetAssemblyDependencies()
    {
        var references = new List<Assembly>();

        if (_basePath == null) return references;

        foreach (var dll in Directory.GetFiles(_basePath, "*.dll"))
        {
            // Ignore target assembly
            if (Path.GetFileName(dll).Equals(Path.GetFileName(_dllPath), StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                var refAssembly = Assembly.LoadFrom(dll);
                references.Add(refAssembly);
            }
            catch
            {
                continue;
            }
        }

        return references;
    }

    // GTODO: Maybe I should check if the baseClass is DbContext instead
    public static Type? FindDbContextFromAssembly(Assembly assembly) =>
         GetLoadableTypes(assembly).FirstOrDefault(t =>
            t.IsClass && !t.IsAbstract &&
            (t.BaseType?.Name == "DbContext" || t.BaseType?.FullName == "Microsoft.EntityFrameworkCore.DbContext"));

    public static IEnumerable<string> GetPublicNamespacesFromAssemblies(IEnumerable<Assembly> assemblies) => assemblies
        .SelectMany(GetLoadableTypes)
        .Where(t => t.IsPublic && !string.IsNullOrEmpty(t.Namespace))
        .Select(t => t.Namespace!)
        .Distinct();
        

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
}