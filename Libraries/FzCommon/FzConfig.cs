using System.Reflection;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace FzCommon
{
    public class FzConfig
    {
        public class Keys
        {
            public const string AzureStorageConnectionString = "AzureStorageConnectionString";
            public const string EmailSiteRoot = "EmailSiteRoot";
            public const string EmailFromAddress = "EmailFromAddress";
            public const string EmailToAddressOverride = "EmailToAddressOverride";
            public const string FacebookUserQueryEndpointFormat = "FacebookUserQueryEndpointFormat";
            public const string GoogleAuthClientID = "GoogleAuthClientID";
            public const string GoogleAuthTokenEndpoint = "GoogleAuthTokenEndpoint";
            public const string GoogleAuthClientSecret = "GoogleAuthClientSecret";
            public const string GoogleCaptchaSecretKey = "GoogleCaptchaSecretKey";
            public const string GoogleCaptchaSiteKey = "GoogleCaptchaSiteKey";
            public const string GoogleInvisibleCaptchaSiteKey = "GoogleInvisibleCaptchaSiteKey";
            public const string GoogleInvisibleCaptchaSecretKey = "GoogleInvisibleCaptchaSecretKey";
            public const string GoogleMapsApiKey = "GoogleMapsApiKey";
            public const string JwtTokenKey = "JwtTokenKey";
            public const string LocalSmtpHost = "LocalSmtpHost";
            public const string LocalSmtpPass = "LocalSmtpPass";
            public const string LocalSmtpUser = "LocalSmtpUser";
            public const string NwrfcForecastUrlFormat = "NwrfcForecastUrlFormat";
            public const string PlivoAuthId = "PlivoAuthId";
            public const string PlivoAuthToken = "PlivoAuthToken";
            public const string PushNotificationServiceUrl = "PushNotificationServiceUrl";
            public const string PushReceiptsServiceUrl = "PushReceiptsServiceUrl";
            public const string SendGridApiKey = "SendGridApiKey";
            public const string SiteUrl = "SiteUrl";
            public const string SlackUrlOverride = "SlackUrlOverride";
            public const string SlackForecastNotificationUrl = "SlackForecastNotificationUrl";
            public const string SlackGageNotificationUrl = "SlackGageNotificationUrl";
            public const string SlackLogBookNotificationUrl = "SlackLogBookNotificationUrl";
            public const string SmsFromPhone = "SmsFromPhone";
            public const string SmsReceiveServiceUrl = "SmsReceiveServiceUrl";
            public const string SmsSendServiceUrl = "SmsSendServiceUrl";
            public const string SmsStopResponse = "SmsStopResponse";
            public const string SqlConnectionString = "SqlConnectionString";
            public const string TestPushNotificationServiceUrl = "TestPushNotificationServiceUrl";
            public const string UploadsBlobContainer = "UploadsBlobContainer";
            public const string UseSendGridSandboxMode = "UseSendGridSandboxMode";
            public const string UsgsWaterServiceUrlFormat = "UsgsWaterServiceUrlFormat";
        }

        //$ TODO: use managed identities for services
        //$ https://docs.microsoft.com/en-us/azure/azure-app-configuration/howto-integrate-azure-managed-service-identity

        //$ TODO: Integrate app.config for FhWebJob and similar services?  Or just plan on converting all
        //$ of those to Azure Functions?

        private static string GetBasePath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static void Initialize()
        {

            // This is temporary.  For now we require an appconfig.settings.json with the
            // connect string for the Azure App Configuration.  Eventually the plan is to
            // separate this out, so that developers who run locally will need a config,
            // but deployed services will use Azure Managed Identities to connect to the
            // config store.
            string basePath = GetBasePath();
            
            ConfigurationBuilder bootstrapBuilder = new ConfigurationBuilder();
            bootstrapBuilder
                    .SetBasePath(basePath)
                    .AddJsonFile("appconfig.settings.json", optional: true, reloadOnChange: false)
                    ;
            IConfigurationRoot tempConfig = bootstrapBuilder.Build();
            if (String.IsNullOrEmpty(tempConfig["AzureAppConfiguration"]))
            {
                throw new ApplicationException(String.Format("FloodZilla configuration error: FloodZilla apps must have an appconfig.settings.json file that has the Azure App Configuration endpoint.  See FzCommon\\FzConfig.cs for details. [base path {0}]", basePath));
            }

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddAzureAppConfiguration(tempConfig["AzureAppConfiguration"])
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("developer.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    ;

            s_config = builder.Build();
        }

        // This is a fallback; it's mostly for use in cases where the database is down or some
        // other catastrophe is happening.  As such, don't log any errors or anything; just
        // return null if anything goes wrong.
        public static dynamic GetAppSettingsFromFile()
        {
            try
            {
                string filePath = GetBasePath() + "\\appconfig.settings.json";
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        using (JsonTextReader jr = new JsonTextReader(sr))
                        {
                            return new JsonSerializer().Deserialize(jr);
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static IConfigurationRoot Config
        {
            get
            {
                if (s_config == null)
                {
                    throw new ApplicationException("Apps using FzConfig must call FzConfig.Initialize()");
                }
                return s_config;
            }
        }

        private static IConfigurationRoot s_config = null;

    }
}
