export default class GageStatusModel {
  constructor(apiResponse) {

    Object.assign(this, apiResponse);

    //$ TODO: rename 'status' in the API and remove this.
    this.currentStatus = this.status;
    delete this.status;
    
    this.processStatus(this.currentStatus);
    this.processStatus(this.peakStatus);
  }

  // pull out some individual fields for convenience, and make stuff
  // human-readable
  processStatus(s) {
    if (!s) {
      return;
    }
    if (s.lastReading) {
      s.waterLevel = s.lastReading.waterHeight;
      s.waterDischarge = s.lastReading.waterDischarge;
      if (s.waterDischarge === 0) {
        s.waterDischarge = null;
      }
      s.roadSaddleHeight = s.lastReading.roadSaddleHeight;
    } else {
      s.waterLevel = null;
      s.waterDischarge = null;
      s.roadSaddleHeight = null;
    }

    s.floodStatus = s.floodLevel;
    switch (s.floodLevel) {
      default:
      case 'Offline':
        s.floodLevelIndicator = 0;
        s.boxStyle = 'water-status-box-offline water-status-box-offline-text';
        break;
      case 'Online':
        s.floodLevelIndicator = 0;
        s.boxStyle = 'water-status-box-normal water-status-box-normal-text';
        break;
      case 'Dry':
      case 'Normal':
        s.floodLevelIndicator = 1;
        s.boxStyle = 'water-status-box-normal water-status-box-normal-text';
        break;
      case 'NearFlooding':
        s.floodLevelIndicator = 2;
        s.floodStatus = 'Near Flooding';
        s.boxStyle = 'water-status-box-warning water-status-box-warning-text';
        break;
      case 'Flooding':
        s.floodLevelIndicator = 3;
        s.boxStyle = 'water-status-box-danger water-status-box-danger-text';
        break;
    }

    s.waterStatus = s.levelTrend;
    // If these need special handling...
/*  switch (s.levelTrend) {
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

  get isUsgs() {
    return this.id.includes("USGS");
  }

  calcRoadStatus(gage, waterLevel) {
    if (!gage.roadSaddleHeight || !gage.roadDisplayName || !waterLevel) return null;
    const level = waterLevel - gage.roadSaddleHeight;
    const preposition =
      gage.roadSaddleHeight - waterLevel > 0 ? "below" : "over";
    const deltaFormatted = Math.abs(level).toFixed(1) + " ft.";
    return { name: gage.roadDisplayName, level, preposition, deltaFormatted };
  }
}
