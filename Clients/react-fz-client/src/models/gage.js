export default class Gage {
  constructor(apiResponse) {
    Object.assign(this, apiResponse);

    if (this.currentStatus && this.currentStatus.lastReading) {
      this.waterLevel = this.currentStatus.lastReading.waterHeight;
      this.waterDischarge = this.currentStatus.lastReading.waterDischarge;
      this.lastReading = this.currentStatus.lastReading.timestamp;
      //$ anything else?
    } else {
      this.waterLevel = 0;
      this.waterDischarge = 0;
    }

    if (this.currentStatus) {
      this.status = this.currentStatus.floodLevel;
      switch (this.currentStatus.floodLevel) {
        default:
        case 'Offline':
          this.floodLevelIndicator = 0;
          this.boxStyle = 'water-status-box-offline water-status-box-offline-text';
          break;
        case 'Online':
          this.floodLevelIndicator = 0;
          this.boxStyle = 'water-status-box-normal water-status-box-normal-text';
          break;
        case 'Dry':
        case 'Normal':
          this.floodLevelIndicator = 1;
          this.boxStyle = 'water-status-box-normal water-status-box-normal-text';
          break;
        case 'NearFlooding':
          this.floodLevelIndicator = 2;
          this.status = 'Near Flooding';
          this.boxStyle = 'water-status-box-warning water-status-box-warning-text';
          break;
        case 'Flooding':
          this.floodLevelIndicator = 3;
          this.boxStyle = 'water-status-box-danger water-status-box-danger-text';
          break;
      }

      this.waterStatus = this.currentStatus.levelTrend;
      // If these need special handling...
/*      switch (this.currentStatus.levelTrend) {
        case 'Offline':
          break;
        case 'Rising':
          break;
        case 'Steady':
          break;
        case 'Falling':
          break;
      }
*/
    }
  }

  get roadStatus() {
    return this.calcRoadStatus();
  }

  get isUsgs() {
    return this.id.includes("USGS");
  }

  calcRoadStatus(waterLevel) {
    const gage = this;
    if (!gage.roadSaddleHeight || !gage.roadDisplayName) return null;
    waterLevel = waterLevel || gage.waterLevel;
    const level = waterLevel - gage.roadSaddleHeight;
    const preposition =
      gage.roadSaddleHeight - waterLevel > 0 ? "below" : "over";
    const deltaFormatted = Math.abs(level).toFixed(1) + " ft.";
    return { name: gage.roadDisplayName, level, preposition, deltaFormatted };
  }
}
