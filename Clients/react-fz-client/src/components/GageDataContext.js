import React, { useContext, useEffect, useState } from "react";
import moment from "moment-timezone";
import { SessionContext } from "./SessionContext";
import Constants from "../constants";
import ChartRange from "../lib/chartRange";
import { DebugContext } from "./DebugContext";
import GageStatusModel from "../models/GageStatusModel";
import GageForecastModel from "../models/GageForecastModel";
import useInterval from "../lib/useInterval";

export const GageDataContext = React.createContext(null);

export const GageDataResult = {
  PENDING:  "pending",
  ERROR:    "error",
  OK:       "ok",
};

export function GageDataContextProvider(props) {

  const session = useContext(SessionContext);
  const debug = useContext(DebugContext);

  const pendingResult = {
    result: GageDataResult.PENDING
  }

  // getGageStatus will get this if an error occurred or if the gage has no current data.
  const offlineStatus = new GageStatusModel({
    status: {
      floodLevel: "Offline",
      levelTrend: "Offline",
    },
    isBogus: true,
  });

  // all in milliseconds
  const REFRESH_CHECK_INTERVAL = 500;
  const GAGE_STATUS_UPDATE_INTERVAL = 1000 * 60 * 5;
  const GAGE_LIST_UPDATE_INTERVAL = 1000 * 60 * 60 * 6;
  const METAGAGES_UPDATE_INTERVAL = 1000 * 60 * 60 * 6;

  const [ gageListResult, setGageListResult ] = useState(pendingResult);
  const [ gageListTimestamp, setGageListTimestamp ] = useState(null);
  const [ metagagesResult, setMetagagesResult ] = useState(pendingResult);
  const [ metagagesTimestamp, setMetagagesTimestamp ] = useState(null);
  const [ gageStatusResult, setGageStatusResult ] = useState(pendingResult);
  const [ gageStatusTimestamp, setGageStatusTimestamp ] = useState(null);
  const [ connError, setConnError ] = useState(false);

  const onFetchGageList = (response) => {
    setConnError(false);
    setGageListResult({
      result: GageDataResult.OK,
      value: response,
    });
    setGageListTimestamp(debug.getNow());
  }

  const onFetchGageListFail = (error) => {

    setConnError(true);
    setGageListTimestamp(debug.getNow());
    if (gageListResult.result !== GageDataResult.OK) {
      setGageListResult({
        result: GageDataResult.ERROR,
        value: error
      });
    }
  }

  const onFetchMetagages = (response) => {
    setConnError(false);
    setMetagagesResult({
      result: GageDataResult.OK,
      value: response
    });
    setMetagagesTimestamp(debug.getNow());
  }

  const onFetchMetagagesFail = (error) => {

    setConnError(true);
    setMetagagesTimestamp(debug.getNow());
    if (gageListResult.result !== GageDataResult.OK) {
      setMetagagesResult({
        result: GageDataResult.ERROR,
        value: error
      });
    }
  }

  const onFetchGageStatusList = (response) => {
    setConnError(false);
    response.gages = response.gages.map(s => {
      return new GageStatusModel(s);
    });
    setGageStatusResult({
      result: GageDataResult.OK,
      value: response,
    });
    setGageStatusTimestamp(debug.getNow());
  }

  const onFetchGageStatusListFail = (error) => {
    setConnError(true);
    setGageStatusTimestamp(debug.getNow());
    if (gageStatusResult.result !== GageDataResult.OK) {
      setGageStatusResult({
        result: GageDataResult.ERROR,
        value: error
      });
    }
  }

  const fetchGageList = (onSuccess, onFail) => {
    try {
      const gageListUrl = Constants.clientApi.GET_GAGE_LIST_URL + "?regionId=" + window.regionSettings.id;
      session.authFetch(gageListUrl, "GET", null, true,
                        (response) => {
                          onSuccess(response);
                        },
                        (status, message) => {
                          onFail(message);
                        });
    } catch {
      onFail("An error occurred while retrieving the gage list.");
    }
  }

  const fetchMetagages = (onSuccess, onFail) => {
    try {
      const metagagesUrl = Constants.clientApi.GET_METAGAGES_URL + "?regionId=" + window.regionSettings.id;
      session.authFetch(metagagesUrl, "GET", null, true,
                        (response) => {
                          onSuccess(response);
                        },
                        (status, message) => {
                          onFail(message);
                        });
    } catch {
      onFail("An error occurred while retrieving the metagage list.");
    }
  }

  const fetchGageStatus = (onSuccess, onFail) => {

    // 'now' is always in region-local time.
    const now = debug.getNow();
    const from = now.clone().subtract(Constants.FRONT_PAGE_CHART_DURATION);

    const url = Constants.readingApi.GET_STATUS_URL
                + "?regionId=" + window.regionSettings.id
                + "&fromDateTime=" + from.utc().format()
                + "&toDateTime=" + now.utc().format();

    try {
      session.authFetch(url, "GET", null, true,
                        (response) => {
                          onSuccess(response);
                        },
                        (status, message) => {
                          onFail(message);
                        });
    } catch {
      onFail("An error occurred while retrieving the gage status list.");
    }
  }

  const onVisibilityChange = () => {
    if (!document.hidden) {
      checkForUpdates();
    }
  }

  // Fetch initial data on startup
  useEffect(() => {

    document.addEventListener("visibilitychange", onVisibilityChange);

    fetchGageList(onFetchGageList, onFetchGageListFail);
    fetchMetagages(onFetchMetagages, onFetchMetagagesFail);
    fetchGageStatus(onFetchGageStatusList, onFetchGageStatusListFail);
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const forceReloadGageInfo = () => {
    fetchGageList(onFetchGageList, onFetchGageListFail);
    fetchMetagages(onFetchMetagages, onFetchMetagagesFail);
    fetchGageStatus(onFetchGageStatusList, onFetchGageStatusListFail);
  }

  const getGageStatus = (gageid) => {
    if (gageStatusResult) {
      if (gageStatusResult.result === GageDataResult.PENDING) {
        return null;
      }
      if (gageStatusResult.result === GageDataResult.ERROR) {
        return offlineStatus;
      }
    }
    const stat = gageStatusResult.value.gages.find(s => s.locationId === gageid);
    if (!stat) {
      return offlineStatus;
    }
    return stat;
  }

  const _getChartDataUrl = (gageId, apiStartDateString, apiEndDateString, lastReadingId, includeStatus, includePredictions) => {
    return Constants.readingApi.GET_READINGS_URL
           + "?getMinimalReadings=true&regionId=" + window.regionSettings.id
           + "&id=" + gageId
           + (apiStartDateString ? ("&fromDateTime=" + apiStartDateString) : "")
           + (apiEndDateString ? ("&toDateTime=" + apiEndDateString) : "")
           + (lastReadingId ? ("&lastReadingId=" + lastReadingId) : "")
           + (includeStatus ? "&includeStatus=true" : "")
           + (includePredictions ? "&includePredictions=true" : "");
  }

  const getChartData = (gage,
                        requestData,
                        apiStartDateString,
                        apiEndDateString,
                        onLoading,
                        onSuccess,
                        onFailure) => {
    
    try {
      const url = _getChartDataUrl(gage.id, apiStartDateString, apiEndDateString, 0, true, false);
      onLoading(requestData, true);
      session.authFetch(url, "GET", null, true,
                        (response) => {
                          onLoading(requestData, false);
                          onSuccess(requestData, new GageStatusModel(response));
                        },
                        (status, message) => {
                          onLoading(requestData, false);
                          onFailure(requestData, message);
                        });
    } catch (err) {
      if (Constants.DEVELOPMENT_MODE) {
        console.log('_____________________________________________________________________________');
        console.log(err);
        console.log('_____________________________________________________________________________');
      }
      onLoading(false);
      onFailure('An error occurred while fetching chart data.');
    }
  }

  const fetchLiveChartDataForListener = (listener) => {
    if (listener.onLoading) {
      listener.onLoading(listener.requestData, true);
    }
    let chartRange = new ChartRange();
    chartRange.changeDays(listener.chartTimespan.asDays());

    const url = _getChartDataUrl(listener.gage.id, chartRange.apiStartDateString, chartRange.apiEndDateString, listener.lastReadingId, true, true);
    const startTime = chartRange.chartStartDate.clone().utc();

    listener.timestamp = debug.getNow();
    try {
      session.authFetch(url,
                        'GET',
                        null,
                        true,
                        (response) => {

                          setConnError(false);
                          if (listener.onLoading) {
                            listener.onLoading(listener.requestData, false);
                          }
                          if (response.noData) {
                            // No news is good news.
                            return;
                          }

                          if (listener.lastReadingId > 0) {
                            // This is an incremental response.  Take the last response, trim off any
                            // now-expired readings from the end, and add the new response at the beginning.

                            let i = listener.lastResponse.readings.length;

                            while (moment(listener.lastResponse.readings[i - 1].timestamp) < startTime) {
                              i--;
                            }
                            listener.lastResponse.readings = response.readings.concat(listener.lastResponse.readings.slice(0, i));
                            listener.lastResponse.status = response.status;

                            // also replace predictions (and, if they're there, actual readings).
                            listener.lastResponse.predictions = response.predictions;
                            listener.lastResponse.actualReadings = response.actualReadings;
                          } else {
                            listener.lastResponse = response;
                          }

                          listener.lastReadingId = response.lastReadingId;

                          if (listener.onSuccess) {
                            listener.onSuccess(listener.requestData, new GageStatusModel(listener.lastResponse));
                          }
                        },
                        (status, message) => {
                          if (listener.onLoading) {
                            listener.onLoading(listener.requestData, false);
                          }
                          if (listener.onFailure) {
                            listener.onFailure(listener.requestData, message);
                          }
                          setConnError(true);
                        });
    } catch {
      if (listener.onLoading) {
        listener.onLoading(listener.requestData, false);
      }
      if (listener.onFailure) {
        listener.onFailure(listener.requestData, "An error occurred while updating.");
      }
      setConnError(true);
    }
  }

  const registerLiveChartDataListener = (gage,
                                         requestData,
                                         chartTimespan,   // moment.duration
                                         refreshInterval, // milliseconds
                                         onLoading,       // onLoading(requestData, isLoading)
                                         onSuccess,       // onSuccess(requestData, response-from-fetch)
                                         onFailure) => {  // onFailure(requestData, errorMessage)

    const listener = {
      gage, requestData, chartTimespan, refreshInterval, onLoading, onSuccess, onFailure,
      timestamp: 0,
      lastReadingId: 0,
    };
    listener.delete = () => {
      delete GageDataContextProvider._listeners[gage.id];
    };
    GageDataContextProvider._listeners[gage.id] = listener;

    fetchLiveChartDataForListener(listener);

    return listener;
  };

  const _getForecastUrl = (gageIds, apiStartDateString, apiEndDateString) => {
    return Constants.readingApi.GET_FORECAST_URL
        + "?regionId=" + window.regionSettings.id
        + "&includePredictions=true&gageIds=" + gageIds
        + (apiStartDateString ? ("&fromDateTime=" + apiStartDateString) : "")
        + (apiEndDateString ? ("&toDateTime=" + apiEndDateString) : "");
  }

  const fetchForecastForListener = (forecastListener) => {
    if (forecastListener.onLoading) {
      forecastListener.onLoading(forecastListener.requestData, true);
    }
    let chartRange = new ChartRange();
    chartRange.changeDays(forecastListener.chartTimespan.asDays());

    const url = _getForecastUrl(forecastListener.gageIds || forecastListener.gage.id, chartRange.apiStartDateString, chartRange.apiEndDateString);

    forecastListener.timestamp = debug.getNow();
    try {
      session.authFetch(url,
                        'GET',
                        null,
                        true,
                        (response) => {

                          setConnError(false);
                          if (forecastListener.onLoading) {
                            forecastListener.onLoading(forecastListener.requestData, false);
                          }
                          if (response.noData) {
                            // No news is good news.
                            return;
                          }
                          forecastListener.lastResponse = response;

                          if (forecastListener.onSuccess) {
                            forecastListener.onSuccess(forecastListener.requestData, new GageForecastModel(forecastListener.lastResponse, window.regionSettings.timezone));
                          }
                        },
                        (status, message) => {
                          if (forecastListener.onLoading) {
                            forecastListener.onLoading(forecastListener.requestData, false);
                          }
                          if (forecastListener.onFailure) {
                            forecastListener.onFailure(forecastListener.requestData, message);
                          }
                          setConnError(true);
                        });
    } catch {
      if (forecastListener.onLoading) {
        forecastListener.onLoading(forecastListener.requestData, false);
      }
      if (forecastListener.onFailure) {
        forecastListener.onFailure(forecastListener.requestData, "An error occurred while updating.");
      }
      setConnError(true);
    }
  }

  const registerForecastListener = (requestData,
                                    gageIds,         // comma-separated list (can include slash-separated sets, which will be summed)
                                    chartTimespan,   // moment.duration
                                    refreshInterval, // milliseconds
                                    onLoading,       // onLoading(requestData, isLoading)
                                    onSuccess,       // onSuccess(requestData, response-from-fetch)
                                    onFailure) => {  // onFailure(requestData, errorMessage)

    const forecastListener = {
      requestData, gageIds, chartTimespan, refreshInterval, onLoading, onSuccess, onFailure,
      timestamp: 0,
    };
    forecastListener.delete = () => {
      // nothing?
    };
    GageDataContextProvider._forecastListener = forecastListener;

    fetchForecastForListener(forecastListener);

    return forecastListener;
  };

  const checkForUpdates = () => {

    const now = debug.getNow();

    if (gageListTimestamp) {
      if (now.diff(gageListTimestamp) > GAGE_LIST_UPDATE_INTERVAL) {
        setGageListTimestamp(null);
        fetchGageList(onFetchGageList, onFetchGageListFail);
      }
    }
    if (metagagesTimestamp) {
      if (now.diff(metagagesTimestamp) > METAGAGES_UPDATE_INTERVAL) {
        setMetagagesTimestamp(null);
        fetchMetagages(onFetchMetagages, onFetchMetagagesFail);
      }
    }
    if (gageStatusTimestamp) {
      if (now.diff(gageStatusTimestamp) > GAGE_STATUS_UPDATE_INTERVAL) {
        setGageStatusTimestamp(null);
        fetchGageStatus(onFetchGageStatusList, onFetchGageStatusListFail);
      }
    }

    for (let gageid in GageDataContextProvider._listeners) {
      let listener = GageDataContextProvider._listeners[gageid];
      if (now.diff(listener.timestamp) > listener.refreshInterval) {
        try {
          fetchForecastForListener(listener);
        } catch {
          // just eat this; it will have been handled elsewhere...
        }
      }
    }

    if (GageDataContextProvider._forecastListener) {
      if (now.diff(GageDataContextProvider._forecastListener.timestamp) > GageDataContextProvider._forecastListener.refreshInterval) {
        try {
          fetchForecastForListener(GageDataContextProvider._forecastListener);
        } catch {
          // just eat this; it will have been handled elsewhere...
        }
      }
    }
  }

  const forceLiveDataRefresh = (listener) => {
    listener.timestamp = 0;
  }

  const forceForecastRefresh = () => {
    GageDataContextProvider._forecastListener.timestamp = 0;
  }

  useInterval(() => {

    if (document.hidden) {
      return;
    }
    checkForUpdates();
  }, REFRESH_CHECK_INTERVAL);

  // If our logged-in status changes, we can re-fetch gage data, if we want to...
/*  useEffect(() => {
  }, [session.sessionState]);
*/  

  return (
  <GageDataContext.Provider
          value = { {
            gageListResult: gageListResult,
            metagagesResult: metagagesResult,
            gageStatusResult: gageStatusResult,
            getGageStatus: (gageid) => getGageStatus(gageid),
            getChartData: getChartData,
            getConnError: () => { return connError; },
            forceLiveDataRefresh: forceLiveDataRefresh,
            forceForecastRefresh: forceForecastRefresh,
            forceReloadGageInfo: forceReloadGageInfo,
            registerLiveChartDataListener: registerLiveChartDataListener,
            registerForecastListener: registerForecastListener,
          } }
  >
    {props.children}
  </GageDataContext.Provider>
  );
};

GageDataContextProvider._listeners = {};
GageDataContextProvider._forecastListener = null;
