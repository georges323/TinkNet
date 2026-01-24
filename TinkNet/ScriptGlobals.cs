using Microsoft.EntityFrameworkCore;

namespace TinkNet;

public class ScriptGlobals
{
    public T GetContext<T>(string connectionString, DbProvider provider) where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>();
        
        switch (provider)
        {
            case DbProvider.SqlServer:
                builder.UseSqlServer(connectionString);
                break;
            case DbProvider.Postgres:
                builder.UseNpgsql(connectionString);
                break;
            case DbProvider.Sqlite:
                builder.UseSqlite(connectionString);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
        
        // We do this because the Compiler cannot prove at compile-time that every possible T has a
        // constructore accepting DbContextOptions. So we have to use Activator.CreateInstance
        // to look for the matching constructor at runtime
        return (T)Activator.CreateInstance(typeof(T), builder.Options)!;
    }

    public DbContextOptionsBuilder<T> CreateOptionsBuilder<T>() where T : DbContext
    {
        return new DbContextOptionsBuilder<T>();
    }
}

