using Cassandra;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blazing.Modules.NoSqlDatabase;

public static class CassandraModule
{
    public static IServiceCollection AddMigratedCassandraDatabase(this IServiceCollection services, Action<NoSqlDatabaseOptions> optionsAction)
    {
        var options = new NoSqlDatabaseOptions();
        optionsAction(options);

        if (options.IsTemporaryDb)
        {
            var cassandraBuilder = new ContainerBuilder()
                .WithImage("cassandra:4.1.3")
                .WithName("temp-cassandra")
                .WithPortBinding(9042, false)
                .WithResourceMapping(new DirectoryInfo(@"C:\tmp\cassandra"), "/var/lib/cassandra")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9042));

            var cassandraContainer = cassandraBuilder.Build();
            cassandraContainer.StartAsync()
                .Wait(TimeSpan.FromSeconds(240));
            
            var cluster = Cluster.Builder()
                .AddContactPoint(cassandraContainer.Hostname)
                .Build();

            var session = cluster.Connect();
            options.Session = session;
        }

        if (options.Session is null)
        {
            throw new ArgumentException("Database session must be provided");
        }

        services.TryAddSingleton(new NoSqlDatabaseOptions(options));

        //
        // if (schemaMigrationResult is not { Successful: true })
        // {
        //     Console.ForegroundColor = ConsoleColor.Red;
        //     Console.WriteLine(@"DbUp migrations completed with errors");
        //     Console.ResetColor();
        //     throw new ArgumentException("Database migration failed: " + schemaMigrationResult?.Error?.Message);
        // }
        //
        // Console.ForegroundColor = ConsoleColor.Green;
        // Console.WriteLine(@"DbUp completed successfully");
        // Console.ResetColor();
        //
        return services;
    }
}