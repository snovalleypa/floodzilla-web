using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using FzCommon;

[assembly: FunctionsStartup(typeof(SenixListener.ReportDistanceReadings))]
namespace SenixListener
{
    public class ReportDistanceReadings : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder hostBuilder)
        {
            FzConfig.Initialize();

            //$ TODO: Any other configuration/initialization?
        }

        [FunctionName("ReportDistanceReadings")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("Processing SenixListener.ReportDistanceReadings request");

            SenixListenerLog result = null;
            string listenerInfo = Environment.MachineName + " - SenixListener/ReportDistanceReadings, 3/2025";
            string clientIP = "";
            try
            {
                clientIP = req.HttpContext.Connection.RemoteIpAddress.ToString();
            }
            catch
            {
                // Just eat this, it's only for diagnostic purposes
            }

            using (SqlConnection conn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                conn.Open();

                try
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                    result = new SenixListenerLog()
                    {
                        Timestamp = DateTime.UtcNow,
                        ListenerInfo = listenerInfo,
                        ClientIP = clientIP,
                        RawSensorData = requestBody,
                    };

                    dynamic postData = JsonConvert.DeserializeObject(requestBody);

                    //$ TODO: How to decide whether we should do extra work (record receiver, etc)

                    string externalDeviceId;
                    string receiverId;
                    DeviceBase device;
                    SensorReading reading = new SensorReading();
                    reading.ListenerInfo = listenerInfo;
                    SenixReadingResult readingResult
                       = ThingsNetworkHelper.ProcessReading(conn, reading, postData, out externalDeviceId, out receiverId, out device);
                    result.ExternalDeviceId = externalDeviceId;
                    result.ReceiverId = receiverId;

                    if (device != null)
                    {
                        result.DeviceId = device.DeviceId;
                    }
                    if (readingResult.ShouldSave)
                    {
                        if (readingResult.Result != null)
                        {
                            result.Result = readingResult.Result;
                        }
                        else
                        {
                            result.Result =
                                (readingResult.ShouldSaveAsDeleted)
                                    ? "Saved as IsDeleted"
                                    : "Saved";
                        }
                        reading.IsDeleted = readingResult.ShouldSaveAsDeleted;
                        await reading.Save(conn);
                        result.ReadingId = reading.Id;

                        await result.Save(conn);

                        // fall through to post-processing below...
                    }
                    else
                    {
                        result.Result = readingResult.Result;
                        await result.Save(conn);
                        dynamic ignorePost = JsonConvert.SerializeObject("ok");
                        return new OkObjectResult(ignorePost);
                    }

                    try
                    {
                        RecordReceiver(conn, device, receiverId, clientIP, result.Timestamp);
                    }
                    catch (Exception /*ex*/)
                    {
                        //$ TODO: Figure out what to do here; this info isn't critical,
                        //$ so we can probably just ignore the error...
                    }
                }
                catch (Exception ex)
                {
                    //$ TODO: how to handle this?
                    if (result != null)
                    {
                        result.Result = String.Format("Exception: {0}", ex.ToString());
                        await result.Save(conn);
                    }

                    dynamic error = JsonConvert.SerializeObject($"Error: Unable to save sensor reading");
                    return new OkObjectResult(error);
                }
                conn.Close();
            }

            dynamic okResult = JsonConvert.SerializeObject("ok");
            return new OkObjectResult(okResult);
        }

        private static void RecordReceiver(SqlConnection conn, DeviceBase device, string externalReceiverId, string clientIP, DateTime lastReadingReceived)
        {
            ReceiverBase receiver = ReceiverBase.EnsureReceiver(conn, externalReceiverId, clientIP);
            device.SetLatestReceiver(conn, externalReceiverId, lastReadingReceived).Wait();
        }

        private static void RecordSampleRate(SqlConnection conn, DeviceBase device, int sampleRate)
        {
            device.SetSensorUpdateInterval(conn, sampleRate).Wait();
        }
    }
}
