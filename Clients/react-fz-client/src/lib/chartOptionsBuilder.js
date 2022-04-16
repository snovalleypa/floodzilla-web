import moment from "moment";
import Constants from "../constants";
import * as utils from "../lib/utils";

export default class ChartOptionsBuilder {
  constructor({ chartData, gage, gageStatus }) {
    this.chartData = chartData;
    this.gage = gage;
    this.gageStatus = gageStatus;
    return this;
  }

  dashboardOptions() {
    let options = {
      height: 177,
      axisX: {
        tickLength: 0,
        lineThickness: 0,
        labelFormatter: function() {
          return " ";
        },
      },
      axisY: {
        tickLength: 0,
        gridThickness: 0,
        lineThickness: 0,
        stripLines: [],
        labelFormatter: function() {
          return " ";
        },
      },
    };

    options = this._applyBaseOptions(options);

    for (const line of options.axisY.stripLines || []) {
      line.labelFontSize = 11;
      line.labelAlign = "center";
    }

    options.axisX.maximum = options._now
      .clone()
      .add(30, "minute")
      .toDate();

    options.axisX.minimum = options._now
      .clone()
      .subtract(48, "hours")
      .toDate();

    return options;
  }

  gageDetailsOptions(range) {
    let options = {
      height: 300,
      axisX: {
        labelFontColor: "rgba(109,109,109,1)",
        lineColor: "#d8d8d8",
        labelFontSize: 11,
        labelFontFamily: "'Open Sans', sans-serif",
      },
      axisY: {
        lineColor: "#d8d8d8",
        gridColor: "#d8d8d8",
        titleFontColor: "rgba(68, 68, 68, 0.78)",
        titleFontSize: 14,
        titleFontFamily: "'Open Sans', sans-serif",
        labelFontColor: "gray",
        lineThickness: 0,
        tickThickness: 0,
      },
    };
    options.axisY.title = "Water Level (ft.)";
    options.axisX.maximum = range.chartEndDate.clone();
    options.axisX.minimum = range.chartStartDate.clone();
    if (range.days === 1) {
      options.axisX.interval = 6;
      options.axisX.intervalType = "hour";
      options.axisX.valueFormatString = "h tt";
    } else {
      options.axisX.interval = range.days > 7 ? 2 : 1;
      options.axisX.intervalType = "day";
      options.axisX.valueFormatString = "DDD M/D";
    }
    options = this._applyBaseOptions(options);
    const crest = this.chartData.calcCrest();
    if (crest) {
      options.axisX.stripLines = options.axisX.stripLines || [];
      options.axisX.stripLines.push({
        value: crest.timestamp,
        lineDashType: "dot",
        label: "Max " + crest.reading.toFixed(2) + " ft",
        labelFontColor: "#9a9a9a",
        color: "#444444",
      });
    }

    return options;
  }

  _createDataFromDataPoints(options) {
    let segment = [];
    const chartSegments = [segment];
    const dataPoints = this.chartData.dataPoints;
    for (let i = 0; i < dataPoints.length; i++) {
      const point = dataPoints[i];
      const prevPoint = i === dataPoints.length - 1 ? null : dataPoints[i + 1];
      segment.push(point);
      if (
        prevPoint &&
        point.timestamp - prevPoint.timestamp > moment.duration(2, "h")
      ) {
        segment = [];
        chartSegments.push(segment);
      }
    }
    options.data = [];
    for (segment of chartSegments) {
      options.data.push({
        xValueType: "dateTime",
        type: "area",
        lineColor: "#44b5f2",
        color: "#44b5f2",
        fillOpacity: 0.5,
        axisYType: "primary",
        dataPoints: segment.map(d => {
          return { x: d.timestamp, y: d.reading };
        }),
      });
    }
  }

  _applyBaseOptions(options) {
    options = Object.assign(
      {
        exportEnabled: false,
        animationEnabled: false,
        zoomEnabled: false,
        toolTip: {
          borderThickness: 0,
          contentFormatter: dataPointPopup(this.gage),
        },
      },
      options
    );
    this._createDataFromDataPoints(options);
    if (
      !utils.isNullOrUndefined(this.chartData.yMinimum) &&
      !utils.isNullOrUndefined(this.chartData.yMaximum)
    ) {
      options.axisY.viewportMinimum = this.chartData.yMinimum;
      options.axisY.viewportMaximum = this.chartData.yMaximum;
    }

    options.axisY.stripLines = (this.chartData.roads || []).map(cat => {
      return {
        value: cat.elevation,
        label: cat.name,
        color: Constants.FLOODZILLA_ORANGE,
        labelFontColor: Constants.FLOODZILLA_ORANGE,
        lineDashType: "dot",
        labelPlacement: "inside",
        labelAlign: "far",
        labelFontFamily: "'Open Sans', sans-serif",
      };
    });

    if (debug) {
      options._now = debug.getNow();
    } else {
      options._now = moment();
    }

    options.axisX.stripLines = [];
    options.axisX.stripLines.push({
      value: options._now,
      lineDashType: "dot",
      label: "Now",
      labelFontColor: "#9a9a9a",
      color: "#444444",
    });

    return options;
  }
}

function dataPointPopup(gage) {
  return e => {
    const roadStatus = gage.calcRoadStatus(e.entries[0].dataPoint.y);
    let roadDesc = "";
    if (roadStatus) {
      roadDesc = `<br />
        <span class="data-point-content">${roadStatus.deltaFormatted}</span>
        <span class="data-point-title"> ${roadStatus.preposition} road</span>`;
    }
    return ` <div class="data-point">
        <span class="data-point-title">Water Level: </span>
        <span class="data-point-content">
          ${e.entries[0].dataPoint.y.toFixed(2)} ft.
        </span>
        <br />
        <span class="data-point-content">
          ${moment(e.entries[0].dataPoint.x).format("ddd, MMM D h:mm A")}
        </span>
        ${roadDesc}
      </div>`;
  };
}
