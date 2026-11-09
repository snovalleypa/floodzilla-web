using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using FzCommon;

namespace SenixListener
{
    public class ReportValues
    {
        // Setting a very short function name because the Senix UI only allows 20 characters total for path
        [Function("RV")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            SenixListenerLog? result = null;
            string listenerInfo = Environment.MachineName + " - SenixListener/ReportValues, 9/2020";
            string clientIP = "";
            try
            {
                clientIP = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "n/a";
            }
            catch
            {
                // Just eat this, it's only for diagnostic purposes
            }

            using (SqlConnection conn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
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

                    dynamic senixData = JsonConvert.DeserializeObject(requestBody)!;

                    //$ TODO: How to decide whether we should do extra work (record receiver, etc)

                    SensorReading reading = new()
                    {
                        ListenerInfo = listenerInfo
                    };
                    SenixReadingResult readingResult = SenixSensorHelper.ProcessReading(conn,
                                                                                        reading,
                                                                                        senixData,
                                                                                        out string externalDeviceId,
                                                                                        out DeviceBase device);
                    result.ExternalDeviceId = externalDeviceId;

                    string receiverId = (string)senixData["gweui"];
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
                            result.Result = (readingResult.ShouldSaveAsDeleted) ? "Saved as IsDeleted" : "Saved";
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

                    // Post-processing.  Record metadata about this reading.
                    if (!String.IsNullOrEmpty((string)senixData["SampleRate1"]) && !String.IsNullOrEmpty((string)senixData["SampleRate2"]))
                    {
                        int sampleRate1 = (int)senixData["SampleRate1"];
                        int sampleRate2 = (int)senixData["SampleRate2"];

                        if (sampleRate1 != sampleRate2)
                        {
                            //$ TODO: Notify admins about this somehow.
                        }

                        try
                        {
                            if (device != null)
                            {
                                RecordSampleRate(conn, device, sampleRate1);
                            }
                        }
                        catch (Exception /*ex*/)
                        {
                            //$ TODO: Figure out what to do here; this info isn't critical,
                            //$ so we can probably just ignore the error...
                        }
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

        private static void RecordReceiver(SqlConnection conn, DeviceBase? device, string externalReceiverId, string clientIP, DateTime lastReadingReceived)
        {
            ReceiverBase receiver = ReceiverBase.EnsureReceiver(conn, externalReceiverId, clientIP);
            device?.SetLatestReceiver(conn, externalReceiverId, lastReadingReceived).Wait();
        }

        private static void RecordSampleRate(SqlConnection conn, DeviceBase device, int sampleRate)
        {
            device.SetSensorUpdateInterval(conn, sampleRate).Wait();
        }

        [Function("CheckStatus")]
        public static IActionResult CheckStatus([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            return new OkObjectResult("All OK");
        }
    }
}
