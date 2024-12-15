using System.Reflection;
using DbUp;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Blazing.Modules.RdbmsDatabase;

public static class PostgresModule
{
    public static IServiceCollection AddMigratedPostgresDatabase(this IServiceCollection services, Action<RdbmsDatabaseOptions> optionsAction)
    {
        var options = new RdbmsDatabaseOptions();
        optionsAction(options);

        if (options.IsTemporaryDb)
        {
            var postgresSqlBuilder = new PostgreSqlBuilder();
            postgresSqlBuilder = postgresSqlBuilder
                .WithImage("postgres:latest")
                .WithName("temp-postgres");
            if (options.TemporaryPort is not null)
            {
                postgresSqlBuilder = postgresSqlBuilder.WithPortBinding(options.TemporaryPort.Value, 5432);
            }

            var postgresSqlContainer = postgresSqlBuilder.Build();
            postgresSqlContainer.StartAsync()
                .Wait(TimeSpan.FromSeconds(30));
            options.ConnectionString = postgresSqlContainer.GetConnectionString();
        }

        if (options.ConnectionString is null)
        {
            throw new ArgumentException("Database connection must be provided");
        }

        services.TryAddSingleton(new RdbmsDatabaseOptions(options));
        services.AddNpgsqlDataSource(options.ConnectionString);
        EnsureDatabase.For.PostgresqlDatabase(options.ConnectionString);

        bool IsSeedDataScript(string s) => options.SeedDataFilter == null || options.SeedDataFilter(s);
        bool IsSqlScript(string s) => s.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase);

        // Schema migrations
        var schemaChanges = DeployChanges
            .To
            .PostgresqlDatabase(options.ConnectionString)
            .WithExecutionTimeout(TimeSpan.FromMinutes(5))
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), s => IsSqlScript(s) && (!IsSeedDataScript(s) || options.IsTemporaryDb && IsSeedDataScript(s)))
            .WithTransaction()
            .LogToConsole()
            .Build();
        var schemaMigrationResult = schemaChanges.PerformUpgrade();
        if (schemaMigrationResult is not { Successful: true })
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(@"DbUp migrations completed with errors");
            Console.ResetColor();
            throw new ArgumentException("Database migration failed: " + schemaMigrationResult?.Error?.Message);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(@"DbUp completed successfully");
        Console.ResetColor();

        return services;
    }
}