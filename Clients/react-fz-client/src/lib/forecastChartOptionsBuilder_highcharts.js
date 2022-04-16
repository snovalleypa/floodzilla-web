import moment from "moment-timezone";
import * as Highcharts from "highcharts/highstock";

export default class ForecastChartOptionsBuilder {
  constructor(gageInfo, forecast, timezone, daysBefore, daysAfter) {
    this._gageInfo = gageInfo;
    this._forecast = forecast;
    this._timezone = timezone;
    this._daysBefore = daysBefore;
    this._daysAfter = daysAfter;
    this._options = {};

    this.setOptions(forecast);

    return this;
  }

  getOptions() {
    return this._options;
  }

  setOptions(forecast) {

    const now = moment();

    //$ TODO: make this general; don't hardcode numbers
    let floodBand = [];
    let floodLine = [];

    floodBand[0]={ // Flooding
      from: 19400,
      to:  10000000,
      color: 'rgba(68, 170, 213, 0.1)'
    };
    floodLine[0]={
      color: '#999',
      width: 1,
      value: 19400,
      dashStyle: "dash",
      label: {
        text: 'Flood Stage (Carnation & Falls)',
        style: {
          color:  '#606060'
        }
      }
    };

    this._options = {
      chart: {
        type: "spline",
        spacingLeft: 0,
        spacingRight: 5,
        animation: false,
      },
      time: {
        useUTC: true,
        timezone: this._timezone,
      },
      title: {
        text: null,
      },
      plotOptions: {
        series: {
          animation: { duration: 0 },
          states: {
            inactive: { opacity: 1},
          },
          turboThreshold: 2000,
        },
      },
      tooltip: {
        formatter: function () {
          let stageDisplay = ''
          if(this.point?.options?.stage){
            stageDisplay = ` / ${this.point?.options?.stage} ft`
          }
          return '<b>' + this.series.name + '</b><br/>' +
              this.point?.options?.xLabel + ': ' + this.y + ' cfs' + stageDisplay;
        }
      },
      xAxis: {
        type: "datetime",
        min: now.clone().subtract(this._daysBefore, 'days').valueOf(),
        max: now.clone().add(this._daysAfter, 'days').valueOf(),
        dateTimeLabelFormats: {
          second: "%H:%M:%S",
          minute: "%a, %l:%M %p",
          hour: "%a, %l %p",
          day: "%a, %b %e",
          week: "%e. %b",
          month: "%b '%y",
        },
        plotLines: [{
          color: '#999',
          width: 1,
          value: now,
          label: {
            text : 'now'
          }
        }],
      },
      yAxis: {
        startOnTick: false,
        endOnTick: false,
        plotBands: floodBand,
        plotLines: floodLine,
        title: {
          text: "Discharge (cfs)",
        },
      },
    };


    this._createDataFromForecast(forecast);
  }

  _createDataFromForecast(forecast) {
    this._options.series = [];

    if (forecast) {
      this._gageInfo.forEach(g => {
        this._addSeries(g.id, forecast.forecasts[g.id], g.color);
      });

    }
    return 0;
  }

  _addSeries(gageId, resp, color) {

    const gage = this._gageInfo.filter(g => g.id === gageId)[0];
    
    let data = resp.dataPoints.map(r => {
      return {
        x: r.timestamp.valueOf(),
        xLabel: moment.tz(r.timestamp.valueOf(), this._timezone).format("ddd, MMM D, h:mm A"),
        y: r.waterDischarge,
        stage: r.reading,
        isForecast: false
      };
    }).slice().reverse();
    this._options.series.push({
      animation:false,
      name: "Observed: " + gage.title,
      data: data,
      color: color,
      fillOpacity: 0.5,
      threshold: 0,
      lineWidth: 2,
      states: {
        hover: {
          lineWidth: 3,
        },
      },

      //$ todo
      marker: {
        enabled: false,
        radius: 2,
        states: {
          hover: {
            enabled: true,
          },
        },
      },
    });

    this._options.series.push({
      animation:false,
      name: "Forecast: " + gage.title,
      data: resp.forecastDataPoints.map(r => {
        return {
          x: r.timestamp.valueOf(),
          xLabel: moment.tz(r.timestamp.valueOf(), this._timezone).format("ddd, MMM D, h:mm A"),
          y: r.waterDischarge,
          stage: r.reading,
          isForecast: false};
      }),
      fillOpacity: 0,
      color: color,
      threshold: 0,
      lineWidth: 2,
      states: {
        hover: {
          lineWidth: 3,
        },
      },
      marker: {
        symbol: 'circle'
      }
    });
  }
}

