# TinkNet 

**TinkNet** is an interactive CLI REPL for .NET developers. It allows you to dynamically load **any** .NET DLL, discover its `DbContext`, load the DLL's dependencies, connect to a database, and execute C# LINQ queries on the fly or any C# code really to aid in development.

Think of it as "LINQPad meets PHP's Tinker for your terminal," but completely project-agnostic.

## ✨ Features

*   **Dynamic DLL Loading**: Point to any compiled `.dll` (e.g., a Web API bin folder), and TinkNet will load it along with all its dependencies (including ASP.NET Core framework references).
*   **Auto-Discovery**: Automatically scans for and identifies `DbContext` classes within the loaded assembly.
*   **Interactive DB Setup**: Built-in wizard to configure connections for **SQL Server**, **PostgreSQL**, or **SQLite**.
*   **Smart Namespace Import**: Automatically imports all public namespaces from your project, so you can use your DTOs and Models immediately.
*   **NodaTime Support**: Detects and enables `NodaTime` support dynamically if your project uses it (no hard dependencies required).
*   **Rich Visualizations**: Results are displayed as beautiful, auto-generated ASCII tables for collections, or detailed panels for single objects.
*   **Dependency Injection Free**: Manually reconstructs valid `DbContextOptions` so you don't need to run the target application's full startup logic.

## 🚀 Getting Started

### Prerequisites
*   .NET 8.0 SDK or later (Supports .NET 9/10 previews).

### Installation & Running
Clone the repository and run the project directly:

```bash
git clone https://github.com/georges323/TinkNet.git
cd TinkNet
dotnet run --project TinkNet/TinkNet.csproj
```

## 📖 Usage Guide

### 1. Load your Assembly
When started, TinkNet will ask for the path to your target DLL. Point it to the **bin** folder of your API or Class Library.

```text
> Path to DLL: /path/to/my-project/bin/Debug/net8.0/MyProject.Api.dll
```

### 2. Configure Database
TinkNet will find your `DbContext` and ask if you want to connect.

```text
DbContext Found: MyProject.Data.AppDbContext
> Do you want to configure the database connection now? [y/n]
```

Select your provider and paste your connection string:

```text
> Select Database Provider:
  > SqlServer
    Postgres
    Sqlite

> Enter Connection String: conn_string
```

### 3. Query Away!
Once initialized, you are dropped into a C# REPL. A variable named `db` is already pre-configured for you.

**Basic Query:**
```csharp
> db.Users.ToList()
```
*Output: A nice table of users.*

**Complex LINQ:**
```csharp
> db.Orders
    .Where(o => o.Total > 1000)
    .Select(o => new { o.Id, o.Customer.Name, o.Total })
    .ToList()
```

**Using Domain Types:**
```csharp
// Namespaces are auto-imported!
> var newUser = new UserDto { Name = "Georges", Role = UserRole.Admin };
> newUser
```

## 🧩 How It Works

1.  **Loader**: Uses a custom `AssemblyLoadContext` logic to resolve dependencies from the target folder, ensuring that `Microsoft.EntityFrameworkCore` and other libraries referenced by your DLL are loaded correctly.
2.  **ScriptGlobals**: Inject a helper object into the Roslyn Script Engine that provides the `GetContext<T>` method.
3.  **Dynamic Reflection**: Uses reflection to inspect your loaded assemblies. If it detects plugins like `SimplerSoftware.EntityFrameworkCore.SqlServer.NodaTime`, it automatically invokes their configuration methods (`UseNodaTime`) on the context builder, ensuring your custom types map correctly to the database.

## 🛠️ Up Coming Features

1. Code history using arrowkeys and easier editing
2. Be able to get Linq query SQL translations
3. More interactive TUI experience for displaying greater and more complex query/linq results
4. Simpler wizard setup by pointing to appsettings.json or to the target project's appsettings.json

## 📄 License
MIT
