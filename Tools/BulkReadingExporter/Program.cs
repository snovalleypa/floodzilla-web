using FzCommon;
using Microsoft.Data.SqlClient;

FzConfig.Initialize();

using (SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
{
    await sqlcn.OpenAsync();

    await new BulkReadingExporter.BulkReadingExporter().Run(sqlcn);

    await sqlcn.CloseAsync();
}

