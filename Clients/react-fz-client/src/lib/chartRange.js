import moment from "moment-timezone";
const CHART_DEFAULT_RANGE = 2;

export default class ChartRange {

  // inputStartDate and inputEndDate are start-of-day for the dates involved.  They're
  // used to initialize the date picker control and for URLs.

  // chartStartDate and chartEndDate are the actual start/end of the chart.  For "now"
  // charts, they represent [days ago, now].  For non-"now" charts, they're
  // [start of first day, end of last day].

  // apiStartDateString and apiEndDateString are the same as chartStartDate/chartEndDate
  // converted to UTC and formatted for use in API calls.

  static _debug;
  static setDebug(debug) {
    ChartRange._debug = debug;
  }

  constructor(chartRange) {
    if (chartRange) {
      Object.assign(this, chartRange);
    } else {
      this.timeZone = window.regionSettings.timezone;
      this._days = CHART_DEFAULT_RANGE;
      this._isNow = true;
      this._inputEndDate = ChartRange._debug ? ChartRange._debug.getNow() : new moment();
    }
  }
  get isNow() {
    return this._isNow;
  }
  get days() {
    return this._days;
  }

  get inputStartDate() {
    return this.inputEndDate.clone().startOf("day").subtract(this.days - 1, "d");
  }
  get inputEndDate() {
    const end = (this.isNow ? ChartRange._debug.getNow() : this._inputEndDate.clone());
    return end.startOf("day");
  }
  
  get apiStartDateString() {
    return this.chartStartDate.clone().utc().format();
  }
  get apiEndDateString() {
    return this.chartEndDate.clone().utc().format();
  }

  // These are the actual date/times to be used when charting
  get chartStartDate() {
    if (this.isNow) {
      return ChartRange._debug.getNow().subtract(this.days, "d");
    } else {
      return this.inputStartDate;
    }
  }
  get chartEndDate() {
    if (this.isNow) {
      return ChartRange._debug.getNow();
    } else {
      return this._inputEndDate.clone().endOf("day");
    }
  }
  changeDays(days) {
    this._days = days;
    return this;
  }

  // This assumes that incoming dates are in the same timezone as this range (i.e. region time).
  changeDates(inputStartDate, inputEndDate) {
    const now = (ChartRange._debug ? ChartRange._debug.getNow() : new moment());

    // if end date is null, it's because user chose a start date after the most recent end date.
    // make the range be empty and don't worry about it; generally the user will select an
    // end date eventually, but if not just have this range be empty for now.
    if (!inputEndDate) {

      this._isNow = false;
      this._days = -(inputStartDate.clone().diff(this._inputEndDate.clone(), "days") - 1);
      
    } else {
      if (inputEndDate.clone().endOf("day") >= now) {
        this._isNow = true;
        this._inputEndDate = now;
      } else {
        this._isNow = false;
        this._inputEndDate = inputEndDate.clone().startOf("day");
      }
      this._days = this._inputEndDate.clone().startOf("day").diff(inputStartDate.clone().startOf("day"), "days") + 1;
    }
    return this;
  }

  clone() {
    return new ChartRange(this);
  }
}
