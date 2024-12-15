using System.Diagnostics;
using System.Text;
using Blazing.Modules.JobQueue;

namespace Blazing.Modules.Import;

public interface IImportService
{
    long Import();
}

public class ImportService(IJobQueue jobQueue, IRowCounter rows, ILogger<IImportService> logger) : IImportService
{
    public long Import()
    {
        const int batchSize = 100_000;
        var csvReader = GetReader();
        var start = Stopwatch.StartNew();
        csvReader.ReadLine(); // Throw CSV header-line away
        for (var line = csvReader.ReadLine(); line != null; line = csvReader.ReadLine())
        {
            jobQueue.Submit(line);
            var newRows = rows.Increment();
            if (newRows % batchSize == 0)
            {
                logger.LogInformation("Imported rows: {Rows:N0} - elapsed: {Elapsed} - rows/s: {RowsPerMSec:N0}",
                    newRows, start.Elapsed, (newRows * 1_000) / start.ElapsedMilliseconds);
            }
        }

        jobQueue.Shutdown();
        logger.LogInformation("Imported rows: {Rows:N0} - elapsed: {Elapsed} - rows/s: {RowsPerMSec:N0}",
            rows.Value, start.Elapsed, (rows.Value * 1_000) / start.ElapsedMilliseconds);
        return start.ElapsedMilliseconds;
    }

    private TextReader GetReader()
    {
        if (true)
        {
            StringBuilder csv = new StringBuilder(2_000_000)
                .AppendLine("SsoId,AccountNumber,ProductName");
            for (var i = 0; i < 500_000; i++)
            {
                csv.Append("sso" + i + ",")
                    .Append("act" + i + ",")
                    .AppendLine("prn" + i);
            }

            return new StringReader(csv.ToString());
        }

        return new StringReader(Csv);
    }

    private const string Csv =
        """
            SsoId,AccountNumber,ProductName
            sso1,act1,prn1
            sso2,act2,prn2
            sso3,act3,prn3
        """;
}