using ISession = Cassandra.ISession;

namespace Blazing.Modules.NoSqlDatabase;

public class NoSqlDatabaseOptions
{
    public NoSqlDatabaseOptions()
    {
    }

    public NoSqlDatabaseOptions(NoSqlDatabaseOptions options)
    {
        IsTemporaryDb = options.IsTemporaryDb;
        Session = options.Session;
        TemporaryPort = options.TemporaryPort;
    }

    /// <summary>
    /// Controls use of other two vars and is mandatory. By default, not temporary db
    /// </summary>
    public bool IsTemporaryDb { get; set; } = false;

    /// <summary>
    /// Reuse same port even if temporary db. Makes it easier to connect to the db from your DB tool wo. changing the temp port all the time
    /// </summary>
    public int? TemporaryPort { get; set; }

    public ISession? Session { get; set; }
}