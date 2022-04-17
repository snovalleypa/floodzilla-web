const moment = require("moment-timezone");

export const GageChartDataType = {
  LEVEL: 'level',
  DISCHARGE: 'discharge',
  FORECAST: 'forecast',
};

export default class ChartDataModel {

  constructor(gage, chartDataType, readings, predictions, predictedFeetPerHour, actualReadings, noaaForecast) {
    this.gage = gage;
    this.timeZoneName = gage.timeZoneName;
    this.predictedFeetPerHour = predictedFeetPerHour;
    this.readings = readings;
    this.predictions = predictions;
    this.noaaForecast = noaaForecast;
    if (noaaForecast) {
      this.noaaForecastData = this._mapAndAdjustForecastTimestampsForDisplay(noaaForecast.data);
    }
    if (readings && readings.length) {
      this.dataPoints = this._mapAndAdjustTimestampsForDisplay(readings);
      if (predictions && predictions.length) {
        let predWithNoGap = predictions;
        predWithNoGap.unshift(readings[0]);
        this.predictedPoints = this._mapAndAdjustTimestampsForDisplay(predWithNoGap);
      } else {
        this.predictedPoints = null;
      }
      if (actualReadings && actualReadings.length) {
        this.actualPoints = this._mapAndAdjustTimestampsForDisplay(actualReadings);
      } else {
        this.actualPoints = null;
      }
    } else {
      this.dataPoints = null;
      this.predictedPoints = null;
      this.actualPoints = null;
    }
    this.setChartDataType(chartDataType);

    this.roads = [];
    if (gage.roadSaddleHeight && gage.roadDisplayName) {
      var road = {
        elevation: gage.roadSaddleHeight,
        name: gage.roadDisplayName
      };
      this.roads.push(road);
    }
  }

  setChartDataType(chartDataType) {
    this.chartDataType = chartDataType;

    if (chartDataType === GageChartDataType.DISCHARGE) {
      this.yMinimum = this.gage.dischargeMin;
      this.yMaximum = this.gage.dischargeMax;
    } else {
      this.yMinimum = this.gage.yMin;
      this.yMaximum = this.gage.yMax;
    }
  }

  get hasData() {
    return this.dataPoints !== null && this.dataPoints.length > 0;
  }

  calcCrest({
    startDate = moment("1970-01-01"),
    endDate = moment("2170-01-01"),
  } = {}) {
    if (!this.hasData) return null;
    const points = this.dataPoints.filter(
      point => point.timestamp >= startDate && point.timestamp <= endDate
    );
    if (points.length < 3) return null;
    const [min, max] = points.reduce(
      (mm, d) => [Math.min(d.reading, mm[0]), Math.max(d.reading, mm[1])],
      [+Infinity, -Infinity]
    );
    // max has to be 1' greater than min
    if (max < min + 1) {
      return null;
    }
    //has to be greater then 1st and last points
    if (
      max === points[0].reading ||
      max === points[points.length - 1].reading
    ) {
      return null;
    }
    for (let i = points.length - 2; i > 0; i--) {
      const next = points[i - 1];
      const point = points[i];
      const prev = points[i + 1];
      if (
        point.reading === max &&
        point.timestamp - prev.timestamp < moment.duration(120, "m") &&
        next.timestamp - point.timestamp < moment.duration(120, "m")
      ) {
        return point;
      }
    }
    return null;
  }

  _mapAndAdjustForecastTimestampsForDisplay(forecastData) {
    return forecastData.map(point => {
      return {
        reading: point.stage,
        waterDischarge: point.waterDischarge,
        timestamp: moment.tz(point.timestamp, this.timeZoneName),
        isDeleted: false
      };
    });
  }
  _mapAndAdjustTimestampsForDisplay(dataPoints) {
    return dataPoints.map(point => {
      return {
        reading: point.waterHeight,
        waterDischarge: point.waterDischarge,
        timestamp: moment.tz(point.timestamp, this.timeZoneName),
        isDeleted: point.isDeleted
      };
    });
  }
}
