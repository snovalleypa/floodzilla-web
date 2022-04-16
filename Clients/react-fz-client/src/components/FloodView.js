import React, { useContext, useEffect, useState, useReducer } from "react";

import ChartDatePicker from "./ChartDatePicker";
import GageChart from "./GageChart";
import ChartRange from "../lib/chartRange";
import dataManager from "../lib/gageDataManager";
import moment from "moment-timezone";
import "../style/FloodView.css";
import { SessionContext } from "./SessionContext";

const crestMapAccumulator = (crestMap, action) => {
  if (action.reset) {
    return {};
  }
  return Object.assign({}, crestMap, { [action.gageId]: action.crest });
};

export default function FloodView({ gageList }) {
  const [crestMap, crestMapDispatch] = useReducer(crestMapAccumulator, {});
  const [focusedInput, setFocusedInput] = useState(null);
  const [chartRange, setChartRange] = useState(null);

  // on mount/unmount
  useEffect(() => {
    async function onMount() {
      setChartRange(new ChartRange().changeDates(moment("2019-12-19"), moment("2019-12-23")));
    }
    onMount();
  }, []);  // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    crestMapDispatch({ reset: true });
  }, [chartRange]);

  const onDatePickerChange = function({ startDate, endDate, chartRange }) {
    setChartRange(chartRange.clone().changeDates(startDate, endDate));
  };

  const onCalendarFocus = function(input, isMobile) {
    if (isMobile) {
      document.activeElement.blur();
    }
    setFocusedInput(input);
  };

  const headers = ["ID", "Gage", "Crested", "Time Gap", "USGS Gap", "Height"];
  let prev, prevUsgs;

  if (!gageList) return null;
  return (
    <div id="mainArea">
      <div>
        <ChartDatePicker
          chartRange={chartRange}
          isMobile={false}
          focusedInput={focusedInput}
          onDatePickerChange={onDatePickerChange}
          onCalendarFocus={onCalendarFocus}
        />
      </div>
      <div className="container-fluid body-content">
        <div className="table-responsive">
          <table className="table">
            <thead>
              <tr>
                {headers.map(h => (
                  <th key={h}>{h}</th>
                ))}
              </tr>
            </thead>
            {gageList
              .filter(gage => crestMap[gage.id] !== null)
              .map(gage => {
                const row = (
                  <GageRow
                    gage={gage}
                    chartRange={focusedInput ? null : chartRange}
                    key={gage.id}
                    previousGage={prev}
                    previousUsgsGage={prevUsgs}
                    crestMapDispatch={crestMapDispatch}
                  />
                );
                prev = crestMap[gage.id];
                if (gage.isUsgs) {
                  prevUsgs = crestMap[gage.id];
                }
                return row;
              })}
          </table>
        </div>
      </div>
    </div>
  );
}

function GageRow({
  gage,
  chartRange,
  previousGage,
  previousUsgsGage,
  crestMapDispatch,
}) {
  const session = useContext(SessionContext);
  const [chart, setChart] = useState(null);
  const [crestData, setCrestData] = useState(null);
  const [showChart, setShowChart] = useState(false);

  useEffect(() => {
    if (!gage || !chartRange) return;
    setChart(null);
    setCrestData(null);
    dataManager
      .getHistoricalChartData({
        session,
        gage,
        gageId: gage.id,
        apiStartDateString: chartRange.apiStartDateString,
        apiEndDateString: chartRange.apiEndDateString,
      })
      .then(data => {
        setChart(data);
      });
  }, [gage, chartRange, session]);

  useEffect(() => {
    if (!chart) return;
    const { chartData } = chart;
    const crest = chartData.calcCrest();
    crestMapDispatch({ gageId: gage.id, crest });
    if (!crest) return;
    const diff = previousGage ? crest.timestamp - previousGage.timestamp : null;
    const diffUsgs = previousUsgsGage
      ? crest.timestamp - previousUsgsGage.timestamp
      : null;
    setCrestData(Object.assign({}, crest, { diff, diffUsgs }));
  }, [chart, previousUsgsGage, previousGage, crestMapDispatch, gage.id]);

  const toggleChart = showChart => {
    setShowChart(!showChart);
  };

  if (!crestData) {
    return (
      <tbody>
        <tr className={gage.isUsgs ? "usgs-row" : "svpa-row"}>
          <td>{gage.id}</td>
          <td>{gage.locationName}</td>
          <td></td>
          <td></td>
          <td></td>
          <td></td>
        </tr>
      </tbody>
    );
  }
  return (
    <tbody>
      <tr
        className={
          (gage.isUsgs ? "usgs-row" : "svpa-row") +
          (crestData ? " clickable" : "")
        }
        onClick={() => {
          toggleChart(showChart);
        }}
      >
        <td>{gage.id}</td>
        <td>{gage.locationName}</td>
        <td className="text-right">
          {moment(crestData.timestamp).format("LT")}{" "}
          {moment(crestData.timestamp).format("ddd")}
          {", "}
          {moment(crestData.timestamp).format("L")}
        </td>
        <td className="text-right">{_formatDuration(crestData.diff)}</td>
        <td className="text-right">{_formatDuration(crestData.diffUsgs)}</td>
        <td className="text-right">{crestData.reading.toFixed(2)}</td>
      </tr>
      {showChart === true && (
        <tr>
          <td colSpan="6">
            <GageChart
              gage={chart && chart.gage}
              chartData={chart && chart.chartData}
              range={chartRange}
            />
          </td>
        </tr>
      )}
    </tbody>
  );
}

function _formatDuration(duration) {
  var d = moment.duration(duration);
  if (!duration) return null;
  return (
    (d.days() ? d.days() + "d " : "") +
    (d.hours() ? d.hours() + "h " : "") +
    ("0" + d.minutes()).slice(-2) +
    "m"
  );
}
