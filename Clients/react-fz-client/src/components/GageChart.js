import React, { useContext, useEffect, useState } from "react";
import ChartOptionsBuilder from "../lib/chartOptionsBuilder_highcharts";
import * as Highcharts from "highcharts/highstock";
import BrokenAxis from "highcharts/modules/broken-axis";
import HighchartsReact from "highcharts-react-official";
import moment from "moment-timezone";
import ChartDataModel from "../models/ChartDataModel";
import { DebugContext } from "./DebugContext";
import useInterval from "../lib/useInterval";

BrokenAxis(Highcharts);
window.moment = moment; // needed for highcharts timezone property

export default function GageChart({
  gage,
  gageStatus,
  chartDataType,
  chartData,
  range,
  optionType = "gageDetailsOptions",
}) {
  const [options, setOptions] = useState(null);
  const [tick, setTick] = useState(0);
  const [realChartData, setRealChartData] = useState(chartData);

  const FORCE_UPDATE_NOW_INTERVAL = 60000;

  const debug = useContext(DebugContext);

  useEffect(() => {
    if (chartData) {
      chartData.setChartDataType(chartDataType);
      setRealChartData(chartData);
    } else if (gage && gageStatus) {
      setRealChartData(new ChartDataModel(gage, chartDataType, gageStatus.readings, null, 0, null));
    }
  }, [gage, chartDataType, gageStatus, chartData]);

  useEffect(() => {
    if (!gage || !realChartData || !realChartData.dataPoints) return;

    setOptions(
      new ChartOptionsBuilder({
        debug: debug,
        chartDataType: chartDataType,
        chartData: realChartData,
        gage: gage,
        gageStatus: gageStatus,
      })[optionType]({ range })
    );
  }, [debug, gage, gageStatus, chartDataType, realChartData, range, tick, optionType]);

  useInterval(() => {
    // use ticker to keep chart "Now" positioned when chart not updating
    setTick(tick + 1);
  }, (!range || range.isNow) ? FORCE_UPDATE_NOW_INTERVAL : 0);

  if (!gage || !gageStatus || !options) return null;
  return (
    <div>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </div>
  );
}
