import moment from "moment-timezone";
import Constants from "../constants";
import { GageChartDataType } from "../models/ChartDataModel";

//$ TODO: formalize chart timespan margins for debugging
const DEBUGGING_TIMESPAN_MARGIN = 0; // 300;

//$ TODO: move this someplace shared between back/front end?
const PREDICTION_WINDOW_MINUTES = 60 * 6; // 6 hours of predictions

export default class ChartOptionsBuilder {
  constructor({ debug, chartDataType, chartData, gage, gageStatus }) {
    this._debug = debug;
    this._chartDataType = chartDataType;
    this._chartData = chartData;
    this._gage = gage;
    this._gageStatus = gageStatus;
    this._options = {};
    return this;
  }

  dashboardOptions() {
    this._applyBaseOptions();
    this._options.chart.height = 182;
    this._options.xAxis.labels = { enabled: false };
    this._options.xAxis.tickLength = 0;
    this._options.yAxis.labels = { enabled: false };
    this._options.yAxis.gridLineWidth = 0;
    this._options.yAxis.title = null;

    for (const line of this._options.yAxis.plotLines || []) {
      line.label.style.fontSize = "11px";
      line.label.align = "center";
      line.label.x = 0;
    }

    this._options.xAxis.max = this._options._now
      .clone()
      .add(0, "minute")
      .valueOf();

    const chartBeginTime = this._options._now.clone().subtract(Constants.FRONT_PAGE_CHART_DURATION);

    this._options.xAxis.min = chartBeginTime
      .clone()
      .subtract(20, "m")
      .valueOf();

    this._options.xAxis.plotLines.push({
      value: chartBeginTime.valueOf(),
      dashStyle: "dot",
      color: "#9a9a9a",
      label: {
        text: Constants.FRONT_PAGE_CHART_DURATION_LABEL,
        style: { color: "#9a9a9a" },
        align: "left",
      },
    });

    return this._options;
  }

  gageDetailsOptions({ range }) {
    this._applyBaseOptions();

    let predictionWindow = 0;
    if (this._chartData.predictedPoints) {
      predictionWindow = PREDICTION_WINDOW_MINUTES;
    }

    this._options.xAxis.max = range.chartEndDate.clone().add(predictionWindow,"m").add(DEBUGGING_TIMESPAN_MARGIN,"m").valueOf();

    this._options.xAxis.min = range.chartStartDate.clone().subtract(DEBUGGING_TIMESPAN_MARGIN,"m").valueOf();

    const crest = this._chartData.calcCrest({
      startDate: range.chartStartDate,
    });
    if (crest) {
      this._options.xAxis.plotLines = this._options.xAxis.plotLines || [];
      this._options.xAxis.plotLines.push(
        makePlotLine({
          value: crest.timestamp.valueOf(),
          label: "Max " + crest.reading.toFixed(2) + " ft",
        })
      );
    }

    return this._options;
  }

  _createDataFromDataPointsAndReturnMin() {
    this._options.series = [];
    let hasPredictions = false;
    if (this._chartData.predictedPoints) {
      this._createPredictionSeries(this._chartData.predictedPoints, Constants.GAGE_CHART_PREDICTIONS_LINE_COLOR);
      hasPredictions = true;
    }
    if (this._chartData.actualPoints) {
      this._createActualDataSeries(this._chartData.actualPoints, Constants.GAGE_CHART_ACTUAL_DATA_LINE_COLOR);
    }
    if (this._chartData.noaaForecast) {
      this._createForecastDataSeries(this._chartData.noaaForecastData, Constants.GAGE_CHART_FORECAST_DATA_LINE_COLOR);
    }
    const dataPoints = this._chartData.dataPoints
      .slice()
      .filter(d => !d.isDeleted)
      .reverse();
    let min = this._createSeriesAndReturnMin(dataPoints, Constants.GAGE_CHART_LINE_COLOR, hasPredictions);
    const delData = this._chartData.dataPoints
      .slice()
      .filter(d => d.isDeleted)
      .reverse();
    if (delData && delData.length > 0) {
      let delMin = this._createSeriesAndReturnMin(delData, Constants.GAGE_CHART_DELETED_LINE_COLOR);
      if (delMin < min) {
        min = delMin;
      }
    }
    return min;
  }

  _createSeriesAndReturnMin(dataPoints, color, setIsPrediction) {
    let data = null;
    let min = Math.min.apply(null, dataPoints.map(d => d.reading));
    if (setIsPrediction) {
      if (this._chartDataType === GageChartDataType.DISCHARGE) {
        data = dataPoints.map(d => {
          return {x: d.timestamp.valueOf(), y: d.waterDischarge, isPrediction: false};
        })
      } else {
        data = dataPoints.map(d => {
          return {x: d.timestamp.valueOf(), y: d.reading, isPrediction: false};
        })
      }
    } else {
      if (this._chartDataType === GageChartDataType.DISCHARGE) {
        data = dataPoints.map(d => {
          return [d.timestamp.valueOf(), d.waterDischarge];
        });
      } else {
        data = dataPoints.map(d => {
          return [d.timestamp.valueOf(), d.reading];
        });
      }
    }
    this._options.series.push({
      animation:false,
      name: "gage height",
      data: data,
      color: color,
      fillOpacity: 0.5,
      threshold: this._gage.groundHeight || 0,
      lineWidth: 2,
      gapUnit: "value",
      gapSize: moment.duration(2, "h").valueOf(),
      states: {
        hover: {
          lineWidth: 3,
        },
      },
      marker: {
        enabled: true,
        radius: 2,
        states: {
          hover: {
            enabled: true,
          },
        },
      },
    });
    return min;
  }

  _createPredictionSeries(dataPoints, color) {
    this._options.series.push({
      animation:false,
      name: "predicted gage height",
      data: dataPoints.map(d => {
        return {x: d.timestamp.valueOf(), y: d.reading, isPrediction: true};
      }),
      fillOpacity: 0,
      color: color,
      threshold: this._gage.groundHeight || 0,
      lineWidth: 1,
      states: {
        hover: {
          lineWidth: 1,
        },
      },
    });
  }

  _createActualDataSeries(dataPoints, color) {
    this._options.series.push({
      animation:false,
      name: "actual gage height",
      data: dataPoints.map(d => {
        return {x: d.timestamp.valueOf(), y: d.reading, isPrediction: false};
      }),
      fillOpacity: 0,
      color: color,
      threshold: this._gage.groundHeight || 0,
      lineWidth: 1,
      states: {
        hover: {
          lineWidth: 1,
        },
      },
    });
  }

  _createForecastDataSeries(dataPoints, color) {
    this._options.series.push({
      animation:false,
      name: "forecast gage height",
      data: dataPoints.map(d => {
        return {x: d.timestamp.valueOf(), y: d.reading, isPrediction: true};
      }),
      fillOpacity: 0,
      color: color,
      threshold: this._gage.groundHeight || 0,
      lineWidth: 1,
      states: {
        hover: {
          lineWidth: 1,
        },
      },
    });
  }

  _applyBaseOptions(options) {
    this._options = {
      chart: {
        height: 300,
        type: "area",
        spacingLeft: 0,
        spacingRight: 5,
        animation: false,
      },
      time: {
        useUTC: true,
        timezone: this._gage.timeZoneName,
      },
      title: {
        text: null,
      },
      legend: { enabled: false },
      plotOptions: {
        series: {
          animation: { duration: 0 },
          states: {
            inactive: { opacity: 1},
          },
          turboThreshold: 2000,
        },
        area: { fillOpacity: 0.5, animation: false }
      },
      tooltip: {
        useHTML: true,
        formatter: dataPointPopup(this._gage, this._gageStatus),
      },
      xAxis: {
        type: "datetime",
        dateTimeLabelFormats: {
          second: "%H:%M:%S",
          minute: "%a, %l:%M %p",
          hour: "%a, %l %p",
          day: "%a, %b %e",
          week: "%e. %b",
          month: "%b '%y",
        },
      },
      yAxis: {
        type: (this._chartDataType === GageChartDataType.DISCHARGE) ? "logarithmic" : "linear",
        startOnTick: false,
        endOnTick: false,
        // minPadding: 0,
        // maxPadding: 0,
        title: {
          text: (this._chartDataType === GageChartDataType.DISCHARGE) ? "Discharge (cfs)" : "Water Level (ft.)",
        },
      },
    };
    const minVal = this._createDataFromDataPointsAndReturnMin();
    this._options.yAxis.min = Math.max(
      this._gage.groundHeight || 0,
      this._chartData.yMinimum
    );
    this._options.yAxis.min = Math.min(minVal, this._options.yAxis.min);
    this._options.yAxis.max = this._chartData.yMaximum;

    this._options.yAxis.plotLines = (this._chartData.roads || []).map(cat => {
      return {
        value: cat.elevation,
        label: {
          text: cat.name,
          style: {
            color: Constants.FLOODZILLA_ORANGE,
            fontFamily: "'Open Sans', sans-serif",
            fontSize: "14px",
          },
          align: "right",
          x: -10,
        },
        color: Constants.FLOODZILLA_ORANGE,
        dashStyle: "dot",
      };
    });

    //$ TODO: when do we not have a _debug?

    if (this._debug) {
      this._options._now = this._debug.getNow();
    } else {
      this._options._now = moment();
    }

    this._options.xAxis.plotLines = [];
    this._options.xAxis.plotLines.push(
      makePlotLine({ value: this._options._now.valueOf(), label: "Now" })
    );

    return this._options;
  }
}

function dataPointPopup(gage, gageStatus) {
  return function() {
    const roadStatus = gageStatus.calcRoadStatus(gage, this.y);
    let roadDesc = "";
    if (roadStatus) {
      roadDesc = `<br />
        <span class="data-point-content">${roadStatus.deltaFormatted}</span>
        <span class="data-point-title"> ${roadStatus.preposition} road</span>`;
    }
    return ` <div class="data-point">
        <span class="data-point-title">${this.point.isPrediction ? "Predicted" : "Water"} level: </span>
        <span class="data-point-content">
          ${this.y.toFixed(2)} ft.
        </span>
        <br />
        <span class="data-point-content">
          ${moment.tz(this.x, gage.timeZoneName).format("ddd, MMM D, h:mm A")}
        </span>
        ${roadDesc}
      </div>`;
  };
}

function makePlotLine({ value, label, color = "#9a9a9a" }) {
  return {
    value,
    dashStyle: "dot",
    color,
    label: {
      text: label,
      style: { color },
      rotation: 270,
      align: "right",
      x: -5,
    },
  };
}
