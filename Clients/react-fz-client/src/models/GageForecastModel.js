const moment = require("moment-timezone");

export default class GageForecastModel {
  constructor(apiResponse, timezone) {

    this._timezone = timezone;
    this.forecasts = {};

    for (const gageId in apiResponse) {
      this.forecasts[gageId] = this._processDataForDisplay(apiResponse[gageId], this._timezone);
    }
  }

  getLatestReading(forecast) {
    return forecast && forecast.dataPoints && forecast.dataPoints[0];
  }

  getRecentMax(forecast) {
    if (!forecast || !forecast.dataPoints || !forecast.dataPoints[0]) {
      return null;
    }
    let cutoff = moment().subtract(24, 'hours')
    let max = forecast.dataPoints[0].waterDischarge;
    let maxReading = forecast.dataPoints[0];
    for (let i = 1; i < forecast.dataPoints.length; i++) {
      const reading = forecast.dataPoints[i];
      if (reading.timestamp < cutoff) {
        break;
      }
      if (reading.waterDischarge >= max) {
        maxReading = reading;
        max = reading.waterDischarge;
      }
    }
    return maxReading;
  }

  _processDataForDisplay(response, timezone) {
    response.dataPoints = response.readings.map(reading => {
      return {
        reading: reading.waterHeight,
        waterDischarge: reading.waterDischarge,
        timestamp: moment.tz(reading.timestamp, timezone),
      };
    });
    response.forecastDataPoints = response.noaaForecast.data.map(forecast => {
      return {
        reading: forecast.stage,
        waterDischarge: forecast.discharge,
        timestamp: moment.tz(forecast.timestamp, timezone),
      };
    });
    response.noaaForecast.peaks.forEach(peak => {
      peak.reading = peak.stage;
      peak.waterDischarge = peak.discharge;
      peak.timestamp = moment.tz(peak.timestamp, timezone);
    });
    response.noaaForecast.created = moment.tz(response.noaaForecast.created, timezone);
    return response;
  }
}
