using System.Diagnostics;
using System.Text;
using Blazing.Modules.JobQueue;

namespace Blazing.Modules.Import;

public interface IImportService
{
    long Import();
}

public class ImportService(IJobQueue jobQueue, ILogger<IImportService> logger) : IImportService
{
    public long Import()
    {
        const int batchSize = 100_000;
        var csvReader = GetReader();
        var start = Stopwatch.StartNew();
        var rows = 0;
        csvReader.ReadLine(); // Throw CSV header-line away
        for (var line = csvReader.ReadLine(); line != null; line = csvReader.ReadLine())
        {
            jobQueue.Submit(line);
            if (++rows % batchSize == 0)
            {
                logger.LogInformation("Imported rows: {Rows:N0} - elapsed: {Elapsed} - rows/s: {RowsPerMSec:N0}", rows,
                    start.Elapsed, (rows * 1_000) / start.ElapsedMilliseconds);
            }
        }

        jobQueue.Shutdown();
        logger.LogInformation("Imported rows: {Rows:N0} - elapsed: {Elapsed} - rows/s: {RowsPerMSec:N0}", rows,
            start.Elapsed, (rows * 1_000) / start.ElapsedMilliseconds);
        return start.ElapsedMilliseconds;
    }

    private TextReader GetReader()
    {
        if (true)
        {
            StringBuilder csv = new(2_000_000);
            for (int i = 0; i < 1_500_000; i++)
            {
                csv.Append("sso" + i + ",")
                    .Append("act" + i + ",")
                    .AppendLine("prn" + i);
            }

            return new StringReader(csv.ToString());
        }

        return new StringReader(Csv);
    }

    private const string Csv = """
                                   SsoId,AccountNumber,ProductName
                               """;
}