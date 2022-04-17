import Constants from "../constants";
import moment from "moment-timezone";
import Gage from "../models/gage";
import ChartData from "../models/chartData";

import { showDeletedReadings } from "../components/Main";

export default class GageApi {
  static _requestCache = {};

  static clearCache() {
    GageApi._requestCache = {};
  }

  static async getGageList({
    session,
    active = true,
    locationDataOnly = false,
    gageIdFilter = "",
    force = false,
  } = {}) {
    const GageListUrl = '/api/Client/GetLocations';

    const url =
      `${Constants.SERVICE_BASE_URL}${GageListUrl}?` +
      `regionId=1&isActiveSensors=${active}&locationDataOnly=${locationDataOnly}` +
      `&showDeletedReadings=${showDeletedReadings()}` + 
      `&id=${gageIdFilter}`;
    let response = await GageApi._requestWithCache({ session, url, force });
    if (response && response.length) {
      response = response.map(gageData => {
        const gage = new Gage(gageData);
        gage.chartData =
          gage.recentReadings && new ChartData(gage, gage.recentReadings, gage.timeZoneName);
        delete gage.recentReadings;
        return gage;
      });
    }
    return response;
  }

  //$ region
  static async getMetagages({
    session,
    force = false,
  } = {}) {
    const GageListUrl = '/api/Client/GetMetagages';

    const url =
      `${Constants.SERVICE_BASE_URL}${GageListUrl}?regionId=1`
    let response = await GageApi._requestWithCache({ session, url, force });
    return response;
  }

  static async OLD_getChartData(session,
  {
    gageId,
    force = false,
    apiStartDateString,
    apiEndDateString,
    lastReadingId,
    isNow,
  }) {
    const url =
      `${Constants.SERVICE_BASE_URL}/api/Client/GetGageReadings?regionId=1&id=${gageId}` +
      `&fromDateTime=${apiStartDateString}` +
      `&toDateTime=${apiEndDateString}` +
      `&showDeletedReadings=${showDeletedReadings()}` + 
      `${lastReadingId ? "&lastReadingId=" + lastReadingId : ""}`;
    const chartData = await GageApi._requestWithCache({
      session,
      url,
      force,
      timesOut: isNow,
    });
    if (!chartData || chartData.noData) {
      return null;
    }
    return {
      chartData: new ChartData(chartData.gage, chartData.readings, chartData.gage.timeZoneName),
      gage: new Gage(chartData.gage),
      lastReadingId: chartData.lastReadingId,
    };
  }

  static async _requestWithCache({
    session,
    url,
    key, // defaults to url
    force = false, //force update
    timesOut = true, //will not time out if false
    test, // validate function f(testParams, cachedParams) - true: cache still valid
    testParams, // passed to test function
  }) {
    key = key || url;
    const cacheEntry = GageApi._requestCache[key];
    const timedOut =
      cacheEntry &&
      timesOut &&
      moment() - cacheEntry.lastRequestTime > Constants.GAGE_CLIENT_CACHE_TIME;
    const passedTest =
      cacheEntry && (!test || test(testParams, cacheEntry.testParams));
    if (!force && cacheEntry && !timedOut && passedTest) {
      //console.log("cache hit", key);
      return cacheEntry.response;
    } else {
      //console.log("cache miss", force, key);
      const response = await request(session, url);
      GageApi._requestCache[key] = {
        lastRequestTime: moment(),
        response,
        testParams,
      };
      return response;
    }
  }
}

//$ error handling

const request = async (session, url) => {
  return await session.authFetch(url, "get", null, true);
};

