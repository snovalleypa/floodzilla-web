using FzCommon;
using FixupBenchmarkChanges;
using Microsoft.Data.SqlClient;

FzConfig.Initialize();

static async Task FixupBenchmarkChanges(SqlConnection sqlcn, SensorLocationBase location)
{
    //$ TODO: Make these paths configurable?
    BenchmarkFixer fixer = new(location, ".", ".");
    Console.CancelKeyPress += new ConsoleCancelEventHandler((o, e) =>
    {
        Console.WriteLine("Cancelling...");
        fixer.Cancel();
    });
    await fixer.Run(sqlcn);
}

if (args.Length < 1)
{
    Console.WriteLine("USAGE: FixupBenchmarkChanges <locationId>");
    Environment.Exit(-1);
}

int locationId = Int32.Parse(args[0]);

using (SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
{
    await sqlcn.OpenAsync();
    SensorLocationBase location = SensorLocationBase.GetLocation(sqlcn, locationId);
    if (location == null)
    {
        Console.WriteLine("Unknown location {0}", locationId);
    }
    else
    {
        location.ConvertValuesForDisplay();
        await FixupBenchmarkChanges(sqlcn, location);
    }
    await sqlcn.CloseAsync();
}
