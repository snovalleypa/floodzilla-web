using Microsoft.Extensions.Hosting;
using FzCommon;

FzConfig.Initialize();

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .Build();

host.Run();
