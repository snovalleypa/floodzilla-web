import Constants from "../constants";
import moment from "moment-timezone";

function _clog(x) {
//  console.log(x);
}

function _dumpSet(s) {
  s.forEach(function(value) {
    _clog(value)
  });
}

export default class GageDataManager {
  static _gageId;
  static _chartDataCache = {};
  static _gageListeners = new Set();
  // Gage -> sends locationData with every update
  // Chart -> sends chart response with every update
  // Updating -> sends true when starting a batch of web request and false when done
  static types = { Gage: "Gage", Chart: "Chart", Updating: "Updating" };
  static _timeout;

  static addListener({ session, type, gage, gageId, callback }) {
    const ref = { type, gage, gageId, callback };
    _clog('=== ABOUT TO ADD');_clog(ref);_clog('-->');_dumpSet(GageDataManager._gageListeners);_clog('===');
    GageDataManager._gageListeners.add(ref);
    _clog('=== AFTER ADD');_dumpSet(GageDataManager._gageListeners);_clog('===');
    ref.delete = () => {
      _clog('=== ABOUT TO DELETE');_clog(ref);_clog('-from->');_dumpSet(GageDataManager._gageListeners);_clog('===');
      GageDataManager._gageListeners.delete(ref);
      _clog('=== AFTER DELETE');_dumpSet(GageDataManager._gageListeners);_clog('===');
    };
    GageDataManager._startKeepUpdatedLoop(session);
    return ref;
  }

  static deleteAllListeners() {
    GageDataManager._stopKeepUpdatedLoop();
    GageDataManager._gageListeners.clear();
  }

  // returns response and response to listeners
  static async getLiveChartData({ session, gageData, gage, gageId, apiStartDateString, force = false }) {
    GageDataManager._toggleLoading(true);
    const response = await GageDataManager._getLiveChartData({
      session,
      gageData,
      gage,
      gageId,
      apiStartDateString,
      force,
    });
    GageDataManager._toggleLoading(false);
    return response;
  }

  // returns response and response to listeners
  static async _getLiveChartData({
    session,
    gageData,
    gage,
    gageId,
    apiStartDateString,
    force = false,
  }) {
    const apiEndDateString = Constants.CHART_API_NOW_DATE_STRING;
    const startDate = moment(apiStartDateString, "YYYY-MM-DD");
    let cachedChart = GageDataManager._chartDataCache[gageId];
    let chartResponse;
    if (!cachedChart) {
      chartResponse = await gageData.getChartData(session, gage, gageId, apiStartDateString, apiEndDateString);
      GageDataManager._chartDataCache[gageId] = {
        startDate,
        apiStartDateString,
        response: chartResponse,
      };
    } else if (startDate < cachedChart.startDate) {
      chartResponse = await gageData.getChartData(session, gage, gageId, apiStartDateString, apiEndDateString);
      GageDataManager._chartDataCache[gageId] = {
        ...cachedChart,
        response: chartResponse,
      };
    } else {

      const chartUpdateResponse = await gageData.getChartData(session, gage, gageId, cachedChart.apiStartDateString, apiEndDateString, cachedChart.response.lastReadingId);
      chartResponse = GageDataManager._mergeUpdate(
        chartUpdateResponse,
        cachedChart.response
      );
      GageDataManager._chartDataCache[gageId] = {
        startDate,
        apiStartDateString,
        response: chartResponse,
      };
    }
    GageDataManager._sendUpdates(gage, gageId, chartResponse);
    GageDataManager._startKeepUpdatedLoop(session, gageData); // reset timer
    return chartResponse;
  }

  static _mergeUpdate(response, cachedResponse) {
    if (
      response &&
      response.chartData &&
      response.chartData.hasData &&
      cachedResponse &&
      cachedResponse.chartData &&
      cachedResponse.chartData.hasData
    ) {
      response.chartData.dataPoints = response.chartData.dataPoints.concat(
        cachedResponse.chartData.dataPoints
      );
    }
    return response || cachedResponse;
  }

  static async getHistoricalChartData({
    session,
    gageData,
    gage,
    gageId,
    apiStartDateString,
    apiEndDateString,
    force = false,
  }) {
    GageDataManager._toggleLoading(true);

    const response = await gageData.getChartData(
      session,
      gage,
      gageId,
      apiStartDateString,
      apiEndDateString,
/*      isNow: false,*/
    );
    GageDataManager._toggleLoading(false);
    return response;
  }

  static _stopKeepUpdatedLoop() {
    clearTimeout(GageDataManager._timeout);
  }

  static _startKeepUpdatedLoop(session, gageData) {
    GageDataManager._stopKeepUpdatedLoop();
    GageDataManager._timeout = setTimeout(async () => {
      await GageDataManager.checkForUpdates(session, gageData, false);
      GageDataManager._startKeepUpdatedLoop(session, gageData);
    }, Constants.GAGE_DATA_REFRESH_RATE);
  }

  static async checkForUpdates(session, gageData, force = true) {
    if (document.hidden) {
      //_clog("checking - hidden, skipped");
      return;
    }
    //_clog("checking...");
    if (GageDataManager._gageListeners.size === 0) {
      return;
    }

    GageDataManager._stopKeepUpdatedLoop();
    const gagesToUpdate = Array.from(GageDataManager._gageListeners).reduce(
      (gages, l) => {
        if (l.gageId) {
          gages.add([l.gage, l.gageId]);
        }
        return gages;
      },
      new Set()
    );
    // call Updating listeners - true
    GageDataManager._toggleLoading(true);

    for (const [gage, gageId] of gagesToUpdate) {
      const cachedChart = GageDataManager._chartDataCache[gageId];
      if (cachedChart) {
        try {
          await GageDataManager._getLiveChartData({
            session,
            gageData,
            gage,
            gageId,
            force,
            apiStartDateString: cachedChart.apiStartDateString,
          });
        } catch (e) {
          console.warn("error in background update", e);
        }
      }
    }

    // call Updating listeners - false
    GageDataManager._toggleLoading(false);
    GageDataManager._startKeepUpdatedLoop(session, gageData);
  }

  static _toggleLoading(loading) {
    for (const listener of GageDataManager._gageListeners) {
      if (listener.type === GageDataManager.types.Updating) {
        listener.callback(loading);
      }
    }
  }

  static _sendUpdates(gage, gageId, response) {
    _clog("_sendUpdates(", gageId,"), listeners is");_dumpSet(GageDataManager._gageListeners);
    for (const listener of Array.from(GageDataManager._gageListeners).filter(
      l => l.gageId === gageId
    )) {
      //_clog("update", listener.type, gageId);
      if (listener.type === GageDataManager.types.Gage) {
        _clog("===== CALLBACKING(",gageId,")");_clog(listener);_clog(listener.callback);_clog(gage);
        listener.callback(gage);
      } else if (listener.type === GageDataManager.types.Chart) {
        listener.callback(response);
      }
    }
  }
}
document.addEventListener("visibilitychange", GageDataManager.checkForUpdates);
