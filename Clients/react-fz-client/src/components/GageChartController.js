import React, { useContext, useEffect, useRef, useState } from "react";
import { useHistory, useLocation } from "react-router-dom";
import queryStringUtil from "query-string";
import moment from "moment-timezone";
import "react-dates/initialize";
import "react-dates/lib/css/_datepicker.css";

import "../style/LocationDetails.css";
import "../style/DatePickerOverrides.css";

import GageChart from "./GageChart";
import Constants from "../constants";
import ChartRange from "../lib/chartRange";
import ChartDatePicker from "./ChartDatePicker";
import { GageDataContext } from "./GageDataContext";
import ChartDataModel, { GageChartDataType } from "../models/ChartDataModel";
import GageStatusModel from "../models/GageStatusModel";

const CHART_RANGE_OPTIONS = [14, 7, 2, 1];

export default function GageChartController({
  chartRangeRef,
  gageId,
  gage,
  gageStatus,
  isMobile,
  setGageNotFound,
  onLoading,
  onLiveStatusUpdate,
}) {
  const gageData = useContext(GageDataContext);
  const location = useLocation();
  const history = useHistory();

  const LIVE_CHART_DATA_REFRESH_INTERVAL = 60 * 1000;   // milliseconds

  const getInitialChartRange = () => {
    const queryParams = queryStringUtil.parse(location.search);
    const regionTimeZone = window.regionSettings.timezone;
    if (queryParams.from && queryParams.to) {
      return new ChartRange().changeDates(moment.tz(queryParams.from, regionTimeZone), moment.tz(queryParams.to, regionTimeZone));
    }
    return new ChartRange();
  }

  const [focusedInput, setFocusedInput] = useState(null);
  const [liveChartData, setLiveChartData] = useState(null);
  const [chartData, setChartData] = useState(null);
  const [chartRange, _setChartRange] = useState(getInitialChartRange());

  const [chartDataType, setChartDataType] = useState(GageChartDataType.LEVEL);

  const setChartRange = (range) => {
    chartRangeRef.current = range;
    _setChartRange(range);
  }

  const chartListenerRef = useRef(null);
  const removeChartListener = () => {
      if (chartListenerRef.current) {
        chartListenerRef.current.delete();
        chartListenerRef.current = null;
      }
  }

  // on mount/unmount
  useEffect(() => {

    chartRangeRef.current = chartRange;

    return () => {
      removeChartListener();
    }
  }, []);  // eslint-disable-line react-hooks/exhaustive-deps

  const onLiveChartSuccess = (requestData, json) => {
    const response = {chartData:new ChartDataModel(gage, chartDataType, json.readings, json.predictions, json.predictedFeetPerHour, json.actualReadings, json.noaaForecast)};
    setLiveChartData(response);
    onLiveStatusUpdate(response.status);
  }

  const onLiveChartFailure = (requestData, message) => {

    //$ TODO: what do we do
    setGageNotFound(true);
  }

  const onHistoricalChartSuccess = (requestData, json) => {
    const response = new ChartDataModel(gage, chartDataType, json.readings, null, 0, null);
    setChartData(response);

    const status = new GageStatusModel(json);
    delete status.currentStatus;
    onLiveStatusUpdate(status);
  }

  const onHistoricalChartFailure = (requestData, message) => {

    //$ TODO: what do we do
    setGageNotFound(true);
  }

  // if we navigate to a different gage, or if our range changes:
  useEffect(() => {
    if (!chartRange || focusedInput) {
      return;
    }

    //$ TODO: Put in a serial number so we don't process out-of-order responses incorrectly
    let requestData = {
    };

    setChartData(null);
    if (chartRange.isNow) {
      if (chartListenerRef.current) {
        removeChartListener();
      }
      chartListenerRef.current
          = gageData.registerLiveChartDataListener(gage,
                                                   requestData,
                                                   moment.duration(chartRange.days, 'days'),
                                                   LIVE_CHART_DATA_REFRESH_INTERVAL,
                                                   onLoading,
                                                   onLiveChartSuccess,
                                                   onLiveChartFailure);
    } else {
      removeChartListener();
      gageData.getChartData(gage,
                            requestData,
                            chartRange.apiStartDateString,
                            chartRange.apiEndDateString,
                            onLoading,
                            onHistoricalChartSuccess,
                            onHistoricalChartFailure);
    }

  // we don't want to have gage, gageData, or session in our deps list; the first one is one-to-one with gageId, and the last 2 don't meaningfully change
  }, [gageId, chartRange, focusedInput]);  // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (liveChartData && chartRange && chartRange.isNow) {
      setChartData(liveChartData.chartData);
    }
  }, [liveChartData, chartRange]);

  const changeRangeDays = function(days, chartRange) {
    const newRange = chartRange.clone().changeDays(days);
    if (!newRange.isNow) {
      setUrlFromRange(newRange);
    }
    setChartRange(newRange);
  };

  const setUrlFromRange = (range) => {
    let queryParams = queryStringUtil.parse(location.search);
    queryParams = {...queryParams,
                   from: range.inputStartDate.format('YYYY-MM-DD'),
                   to: range.inputEndDate.format('YYYY-MM-DD')};
    history.push(location.pathname + '?' + queryStringUtil.stringify(queryParams));
  }

  const clearUrlDateParams = () => {
    let queryParams = queryStringUtil.parse(location.search);
    queryParams = {...queryParams,
                   from: undefined,
                   to: undefined};
    history.push(location.pathname + '?' + queryStringUtil.stringify(queryParams));
  }

  //$ TODO: this uses existing chartRange just to preserve timezone, right?
  const onDatePickerChange = function({ startDate, endDate, chartRange }) {
    const newRange = chartRange.clone().changeDates(startDate, endDate);
    if (newRange.isNow) {
      clearUrlDateParams();
    } else {
      setUrlFromRange(newRange);
    }
    setChartRange(newRange);
  };

  const onCalendarFocus = function(input, isMobile) {
    if (isMobile) {
      document.activeElement.blur();
    }
    setFocusedInput(input);
  };

  const onEventSelected = (startDate, endDate) => {
    const newRange = chartRange.clone().changeDates(startDate, endDate);
    if (newRange.isNow) {
      clearUrlDateParams();
    } else {
      setUrlFromRange(newRange);
    }
    setChartRange(newRange);
  }

  const checkForUpdates = () => {
    gageData.forceLiveDataRefresh(chartListenerRef.current);
  }

  // positioned above chart for web and below for mobile
  const datePicker = (
    <ChartDatePicker
      chartRange={chartRange}
      isMobile={isMobile}
      focusedInput={focusedInput}
      onDatePickerChange={onDatePickerChange}
      onCalendarFocus={onCalendarFocus}
    />
  );

  return (
    <div className="row" id="chart-content">
      <span id="lbl_Msg"></span>
      <div className="card gage-shadow" id="chart-panel">
        <div className="card-body">
          <div className="row" id="filter-criteria">
            <div className="col-lg-10 col-md-10 col-sm-10 col-xs-10 date-range d-none d-sm-flex">
              {gage.hasDischarge
                ?<ul className="chart-data-type nav navbar-nav chart-menu">
                  <li className={(chartDataType === GageChartDataType.LEVEL) ? "active" : ""} onClick={() => setChartDataType(GageChartDataType.LEVEL)}>
                    <button className="link-button">Water Level</button>
                  </li>
                  <li className={(chartDataType === GageChartDataType.DISCHARGE) ? "active" : ""} onClick={() => setChartDataType(GageChartDataType.DISCHARGE)}>
                    <button className="link-button">Discharge</button>
                  </li>
                 </ul>
                :<ul className="chart-data-type nav navbar-nav chart-menu">
                  <li className="active">
                    <button className="link-button no-pointer">Water Level</button>
                  </li>
                 </ul>
              }
            </div>

            <div className="col-lg-2 col-md-2 col-sm-2 col-xs-2 d-none d-sm-flex">
              {!isMobile && <RefreshButton chartRange={chartRange} checkForUpdates={checkForUpdates} />}
            </div>
          </div>
          <div className="row filter-criteria">
            <div className="col-10 col-sm-7 date-range date-range-days">
              <ul className="nav navbar-nav chart-menu">
                {CHART_RANGE_OPTIONS.map(days => (
                  <li
                    key={days}
                    onClick={() => changeRangeDays(days, chartRange)}
                    className={(chartRange && (chartRange.days === days)) ? "active" : ""}
                    style={{ cursor: "pointer", display: 'block', float:'left'}}
                  >
                    <a id={days}>
                      {days} Day{days > 1 ? "s" : ""}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
            <div className="col-2 col-sm-5 date-range date-range-date text-right">
              <div style={{ marginTop: 15 }}>{isMobile && <RefreshButton chartRange={chartRange} checkForUpdates={checkForUpdates} />}</div>
              {!isMobile && datePicker}
            </div>
          </div>
          {chartData && !chartData.hasData && (
            <div id="div_chart-no-record">No data available</div>
          )}
          {chartData && chartData.hasData && (
            <div id="chartContainer">
              <GageChart chartDataType={chartDataType} gage={gage} gageStatus={gageStatus} chartData={chartData} range={chartRange} />
            </div>
          )}
          {!chartData && <div id="div_chart-loading">loading...</div>}
          <div className="visible-xs date-picker-below">
            {isMobile && datePicker}
          </div>
          <CrestInfo chartData={chartData} chartRange={chartRange} />
          <RoadForecast gage={gage} gageStatus={gageStatus} chartData={chartData} />
          <FloodEventList gage={gage} chartRange={chartRange} onEventSelected={onEventSelected} />
        </div>
      </div>
    </div>
  );
}

function CrestInfo({ chartData, chartRange }) {
  const [crest, setCrest] = useState(null);
  useEffect(() => {
    setCrest(
      chartData && chartData.calcCrest({ startDate: chartRange.chartStartDate })
    );
  }, [chartData, chartRange]);
  if (!crest) return null;
  return (
    <div style={{ textAlign: "center", color: "#707070" }}>
      <b>Max: </b>
      {crest.reading.toFixed(2)} ft. / {moment(crest.timestamp).format("llll")}
    </div>
  );
}

function RefreshButton({ chartRange, checkForUpdates }) {
  return (
    <div onClick={checkForUpdates} className="ml-auto">
      <img
        src={`${Constants.RESOURCE_BASE_URL}/images/DashboardIcons/baseline-refresh-24px.png`}
        className={(chartRange.isNow) ? "pull-right gcc-btn-refresh btn-refresh" : "pull-right gcc-btn-refresh btn-refresh-hide"}
        alt="Refresh chart"
      />
    </div>
  );
}

function RoadForecast({ gage, gageStatus, chartData }) {
  const [rate, setRate] = useState(null);
  const [timeToRoad, setTimeToRoad] = useState(null);

  useEffect(() => {
    if (chartData) {
      let rate = chartData.predictedFeetPerHour;
      if (rate > -0.01 && rate < 0.01) {
        rate = null;
      }
      setRate(rate);

      let crossingTime = null;
      if (chartData.predictions && gage.roadSaddleHeight) {
        for (var i = 0; i < chartData.predictions.length - 1; i++) {
          let p = chartData.predictions[i];
          let pNext = chartData.predictions[i + 1];
          if (pNext.waterHeight === gage.roadSaddleHeight) {
            crossingTime = moment(pNext.timestamp);
            break;
          }
          if ((pNext.waterHeight > gage.roadSaddleHeight && gage.roadSaddleHeight > p.waterHeight) ||
              (pNext.waterHeight < gage.roadSaddleHeight && gage.roadSaddleHeight < p.waterHeight)) {
            let waterDelta = (gage.roadSaddleHeight - p.waterHeight) / (pNext.waterHeight - p.waterHeight);
            let msec = moment(pNext.timestamp).diff(moment(p.timestamp)) * waterDelta;
            crossingTime = moment(p.timestamp).add(msec, 'milliseconds');
            break;
          }
        }
      }
      setTimeToRoad(crossingTime);
    }
  }, [gage, chartData]);

  if (rate === null) return null;

  return (
    <div style={{ textAlign: "center", color: "#707070" }}>
      <b>Rate of change: </b>
      {rate > 0 ? "+" : ""}
      {rate.toFixed(2)} feet/hour
      {timeToRoad && (
        <span>
          <br/><b> Road level @ </b>
          {timeToRoad.format("llll")}
        </span>
      )}
    </div>
  );
}

function FloodEventList({ gage, chartRange, onEventSelected }) {

  const [selected, setSelected] = useState(0);
  
  const getOptions = () => {
    let options = [];
    options.push(<option id="0" key="0">- choose event -</option>);
    if (!gage || !gage.floodEvents) {
      return options;
    }
    gage.floodEvents.forEach(e => {
      options.push(<option id={e.id} value={e.id} key={e.id}>{e.eventName}</option>);
    });
    return options;
  }

  useEffect(() => {
    const regionTimeZone = window.regionSettings.timezone;
    setSelected(0);
    if (chartRange && gage.floodEvents) {
      gage.floodEvents.forEach(e => {
        const fromDate = moment.tz(e.fromDate, regionTimeZone);
        const toDate = moment.tz(e.toDate, regionTimeZone);
        if (fromDate.isSame(chartRange.inputStartDate) && toDate.isSame(chartRange.inputEndDate)) {
          setSelected(e.id);
        }
      });
      fixupLoadingIcons(!chartRange.isNow);
    }
  }, [chartRange, gage.floodEvents]);

  const onSelected = (e) => {

    if (!gage || !gage.floodEvents || gage.floodEvents.count === 0) {
      return null;
    }

    const regionTimeZone = window.regionSettings.timezone;
    const item = e.target.options[e.target.selectedIndex];
    if (item && (item.id !== '0')) {
      gage.floodEvents.forEach(e => {
        if ('' + e.id === item.id) {
          onEventSelected(moment.tz(e.fromDate, regionTimeZone), moment.tz(e.toDate, regionTimeZone));
        }
      });
    }
  }

  return (
    <div className="flood-event-list">
      {gage && gage.floodEvents &&
      <>
      Historical Events:&nbsp;&nbsp;
      <select value={selected} onChange={onSelected} className="flood-event-select">
        { getOptions() }
      </select>
      </>
      }
    </div>
  );
}

function fixupLoadingIcons(hideWhenDone) {
  if (hideWhenDone) {
    for (const img of (document.getElementsByClassName("gcc-btn-refresh") || [])) {
      img.classList.replace("btn-refresh", "btn-refresh-hide");
    }
  } else {
    for (const img of (document.getElementsByClassName("gcc-btn-refresh") || [])) {
      img.classList.replace("btn-refresh-hide", "btn-refresh");
    }
  }
}

