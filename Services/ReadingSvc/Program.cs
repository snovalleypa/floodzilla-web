using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using FzCommon;

// NOTE: This has to basically be the first thing we do, or else the wrong defaults will get used
JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
{
    ContractResolver = new DefaultContractResolver
    {
        NamingStrategy = new CamelCaseNamingStrategy()
    },
    NullValueHandling = NullValueHandling.Ignore,
    Converters = [new StringEnumConverter() { AllowIntegerValues = false }],
};

FzConfig.Initialize();

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .Build();

host.Run();
