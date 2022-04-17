const moment = require("moment-timezone");

export default class ChartData {
  constructor(gage, recentReadings, timeZoneName) {

    this.timeZoneName = timeZoneName;
    if (recentReadings && recentReadings.length) {
      this.dataPoints = this._mapAndAdjustTimestampsForDisplay(recentReadings);
    } else {
      this.dataPoints = null;
    }

    this.yMinimum = gage.yMin;
    this.yMaximum = gage.yMax;
    this.roads = [];
    if (gage.roadSaddleHeight && gage.roadDisplayName) {
      var road = {
        elevation: gage.roadSaddleHeight,
        name: gage.roadDisplayName
      };
      this.roads.push(road);
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

  _mapAndAdjustTimestampsForDisplay(dataPoints) {
    return dataPoints.map(point => {
      return {
        reading: point.waterHeight,
        timestamp: moment.tz(point.timestamp, this.timeZoneName),
        isDeleted: point.isDeleted
      };
    });
  }
}
