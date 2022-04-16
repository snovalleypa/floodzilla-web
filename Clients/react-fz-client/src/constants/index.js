//$ TODO: Move everything site-specific out into a global configuration document of some kind.
import moment from "moment-timezone";

const prod = {
  GAGE_DATA_REFRESH_RATE: 60000, // ms
  DASHBOARD_DATA_REFRESH_RATE: 5 * 60000, // ms
  GAGE_CLIENT_CACHE_TIME: 50000, // ms

  SERVICE_BASE_URL: "",
  RESOURCE_BASE_URL: "",
  GAGE_IMAGE_BASE_URL: "https://svpastorage.blob.core.windows.net/uploads/",

  AUTH_BASE_URL: "",
  CLIENT_BASE_URL: "",
  READING_BASE_URL: "https://prodplanreadingsvc.azurewebsites.net",
  SUBSCRIPTION_BASE_URL: "",

  DEVELOPMENT_MODE: false,
};

const dev = {
  GAGE_DATA_REFRESH_RATE: 10000, // ms
  GAGE_CLIENT_CACHE_TIME: 9000, // ms
  DASHBOARD_DATA_REFRESH_RATE: 5 * 6000, // ms
  SERVICE_BASE_URL: "https://floodzilla.com",
//  SERVICE_BASE_URL: "http://localhost:3000",
  RESOURCE_BASE_URL: "//floodzilla.com",
  GAGE_IMAGE_BASE_URL: "https://svpastorage.blob.core.windows.net/uploads/",

  AUTH_BASE_URL: "https://floodzilla.com",
//  AUTH_BASE_URL: "http://localhost:3000",

  CLIENT_BASE_URL: "https://floodzilla.com",
//  CLIENT_BASE_URL: "https://fzbeta.azurewebsites.net",
//  CLIENT_BASE_URL: "http://localhost:3000",

  READING_BASE_URL: "https://prodplanreadingsvc.azurewebsites.net",
//  READING_BASE_URL: "http://localhost:7071",

  SUBSCRIPTION_BASE_URL: "https://floodzilla.com",
//  SUBSCRIPTION_BASE_URL: "http://localhost:3000",

  DEVELOPMENT_MODE: true,

  LOG_FETCH_CALLS: true,
};

const config = process.env.REACT_APP_STAGE === "dev" ? dev : prod;

const authApi = {
  AUTHENTICATE_URL: config.AUTH_BASE_URL + "/Account/Authenticate",
  CREATEACCOUNT_URL: config.AUTH_BASE_URL + "/Account/CreateAccount",
  REAUTHENTICATE_URL: config.AUTH_BASE_URL + "/Account/Reauthenticate",
  AUTHENTICATE_WITH_GOOGLE_URL: config.AUTH_BASE_URL + "/Account/AuthenticateWithGoogle",
  AUTHENTICATE_WITH_FACEBOOK_URL: config.AUTH_BASE_URL + "/Account/AuthenticateWithFacebook",
  UPDATEACCOUNT_URL: config.AUTH_BASE_URL + "/Account/UpdateAccount",
  FORGOTPASSWORD_URL: config.AUTH_BASE_URL + "/Account/APIForgotPassword",
  SETPASSWORD_URL: config.AUTH_BASE_URL + "/Account/APISetPassword",
  CREATEPASSWORD_URL: config.AUTH_BASE_URL + "/Account/APICreatePassword",
  RESETPASSWORD_URL: config.AUTH_BASE_URL + "/Account/APIResetPassword",
  SENDVERIFICATIONEMAIL_URL: config.AUTH_BASE_URL + "/Account/SendVerificationEmail",
  VERIFYEMAIL_URL: config.AUTH_BASE_URL + "/Account/VerifyEmail",
  SENDPHONEVERIFICATION_URL: config.AUTH_BASE_URL + "/Account/SendPhoneVerificationSms",
  VERIFYPHONE_URL: config.AUTH_BASE_URL + "/Account/VerifyPhone",

  FACEBOOK_LOGIN_PROVIDER_NAME: "Facebook",
  GOOGLE_LOGIN_PROVIDER_NAME: "Google",

  ID_TOKEN_HEADER: "X-fz-idToken",
}

const clientApi = {
  GET_GAGE_LIST_URL: config.CLIENT_BASE_URL + "/api/client/APIGetLocationInfo",
  GET_METAGAGES_URL: config.CLIENT_BASE_URL + "/api/client/GetMetagages",
}

const readingApi = {
  GET_STATUS_URL: config.READING_BASE_URL + "/api/GetGageStatusAndRecentReadings",
  GET_READINGS_URL: config.READING_BASE_URL + "/api/GetGageReadingsUTC",
  GET_FORECAST_URL: config.READING_BASE_URL + "/api/GetForecastsUTC",
}

const subscriptionApi = {
  SETTINGS_URL: config.SUBSCRIPTION_BASE_URL + "/api/subscription/usersettings",
  SUBSCRIPTIONS_URL: config.SUBSCRIPTION_BASE_URL + "/api/subscription/usersubs",
  UNSUBEMAIL_URL: config.SUBSCRIPTION_BASE_URL +"/api/subscription/unsubemail",
}

const defaultForecastGageIds = ['USGS-SF17/USGS-NF10/USGS-MF11', 'USGS-38', 'USGS-22']

export default {

  authApi: authApi,
  clientApi: clientApi,
  readingApi: readingApi,
  subscriptionApi: subscriptionApi,

  defaultForecastGageIds: defaultForecastGageIds,

  PASSWORD_MIN_LENGTH: 8,

  SHOW_LOGIN_ICON: true,
    
  FRONT_PAGE_CHART_DURATION: moment.duration(48, 'hours'),
  FRONT_PAGE_CHART_DURATION_LABEL: "48 hrs. ago",

  FRONT_PAGE_CHART_MAX_DURATION: moment.duration(2, 'weeks'),

  SITE_TITLE: "Floodzilla Gage Network",
  CHART_API_NOW_DATE_STRING: "",
  HOME_PAGE_TITLE: "Floodzilla Gage Network - Snoqualmie River / SVPA",
  PAGE_TITLE_SUFFIX: " | Floodzilla Gage Network / SVPA",
  SITE_SUBTITLE: "An SVPA Project",
  MOBILE_SCREEN_LIMIT: 768,
  FLOODZILLA_ORANGE: "#ff7f00",
  GAGE_CHART_LINE_COLOR: "#44b5f2", //"#98d6f8",
  GAGE_CHART_DELETED_LINE_COLOR: "#ff0000",
  GAGE_CHART_PREDICTIONS_LINE_COLOR: "#ff7f00",
  GAGE_CHART_ACTUAL_DATA_LINE_COLOR: "#00FF00",
  GAGE_CHART_FORECAST_DATA_LINE_COLOR: "#ff00ff",
  RECENTLY_ACTIVE_HOURS: 24,
  ABOUT_URL: "https://svpa.us/floodzilla-gage-network/",
  ...config,
};

