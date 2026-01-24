using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace TinkNet;

public class ScriptGlobals
{
    public T GetContext<T>(string connectionString, DbProvider provider) where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>();
        
        switch (provider)
        {
            case DbProvider.SqlServer:
                builder.UseSqlServer(connectionString, options => ConfigureNodaTime(options));
                break;
            case DbProvider.Postgres:
                builder.UseNpgsql(connectionString, options => ConfigureNodaTime(options));
                break;
            case DbProvider.Sqlite:
                builder.UseSqlite(connectionString, options => ConfigureNodaTime(options));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
        
        // We do this because the Compiler cannot prove at compile-time that every possible T has a
        // constructore accepting DbContextOptions. So we have to use Activator.CreateInstance
        // to look for the matching constructor at runtime
        return (T)Activator.CreateInstance(typeof(T), builder.Options)!;
    }

    // Using Reflection, if we find that the loaded assembly has a UseNodaTime then we invoke it in the
    // builder
    private void ConfigureNodaTime(object providerOptionsBuilder)
    {
        if (providerOptionsBuilder == null) return;
        var builderType = providerOptionsBuilder.GetType();

        // Scan loaded assemblies for the UseNodaTime extension method
        // We look for: public static [ConfigBuilder] UseNodaTime(this [ConfigBuilder] builder)
        var method = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => Loader.GetLoadableTypes(a))
            .Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested) // Static classes
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public))
            .Where(m => m.Name == "UseNodaTime" && m.IsDefined(typeof(ExtensionAttribute), false))
            .FirstOrDefault(m => {
                var parameters = m.GetParameters();
                // Check if the first parameter (the 'this' parameter) matches the builder type
                return parameters.Length > 0 && parameters[0].ParameterType.IsAssignableFrom(builderType);
            });

        if (method != null)
        {
            try 
            {
                // Invoke UseNodaTime(builder)
                var parameters = method.GetParameters();
                var args = new object?[parameters.Length];
                args[0] = providerOptionsBuilder;
                
                // Fill optional parameters with default values
                for (int i = 1; i < parameters.Length; i++)
                {
                    if (parameters[i].HasDefaultValue)
                        args[i] = parameters[i].DefaultValue;
                    else
                        args[i] = null; 
                }

                method.Invoke(null, args);
            }
            catch 
            {
                // Ignore failure (best effort)
            }
        }
    }

    public DbContextOptionsBuilder<T> CreateOptionsBuilder<T>() where T : DbContext
    {
        return new DbContextOptionsBuilder<T>();
    }
}

