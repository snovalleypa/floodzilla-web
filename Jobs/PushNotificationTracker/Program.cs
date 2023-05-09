using Microsoft.Extensions.Hosting;
using FzCommon;

FzConfig.Initialize();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
