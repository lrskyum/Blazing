namespace Blazing.Modules.RdbmsDatabase;

public class RdbmsDatabaseOptions
{
    public RdbmsDatabaseOptions()
    {
    }

    public RdbmsDatabaseOptions(RdbmsDatabaseOptions options)
    {
        IsTemporaryDb = options.IsTemporaryDb;
        ConnectionString = options.ConnectionString;
        TemporaryPort = options.TemporaryPort;
    }

    /// <summary>
    /// Controls use of other two vars and is mandatory. By default, not temporary db
    /// </summary>
    public bool IsTemporaryDb { get; set; } = false;

    /// <summary>
    /// Filter applied to scripts to identify seed-data scripts that should only run on temporary databases
    /// </summary>
    public Func<string, bool>? SeedDataFilter { get; set; } = _ => true;

    /// <summary>
    /// Either provided for, for examle DEV, TEST, PROD, or computed in case of temp DB. Computed connection string for temp databases is injectable through DatabaseOptions
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Reuse same port even if temporary db. Makes it easier to connect to the db from your DB tool wo. changing the temp port all the time
    /// </summary>
    public int? TemporaryPort { get; set; }
}