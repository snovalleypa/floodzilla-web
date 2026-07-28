using System.Collections;
using System.Data;
using FzCommon;
using FzCommon.Map;
using Microsoft.Data.SqlClient;

//$ TODO: Metadata?

async Task DumpJobDetails(SqlConnection sqlcn, DateTime startDate)
{
    List<RecentJobRun> runs = await JobRunLog.GetRecentJobRunLogsWithDetails(sqlcn, startDate);
    foreach (RecentJobRun run in runs)
    {
        Console.WriteLine("===================================================================");
        Console.WriteLine("{0} @ {1}:", run.JobName, run.StartTime);
        Console.WriteLine("-------------------------");
        Console.WriteLine(run.Details);
        Console.WriteLine("===================================================================");
        Console.WriteLine();
    }
}

FzConfig.Initialize();
using (SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
{
    await sqlcn.OpenAsync();
    DateTime startTime = DateTime.UtcNow.AddHours(-6);
    await DumpJobDetails(sqlcn, startTime);
}
