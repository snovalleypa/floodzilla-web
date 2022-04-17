export const usgsGage = {
  id: "USGS-38",
  locationName: "Snoqualmie River - Below the Falls",
  latitude: 47.5451019,
  longitude: -121.842336,
  isOffline: false,
  rank: 22,
  yMin: 0,
  yMax: 18,
  groundHeight: 0,
  deviceTypeName: "USGS",
  timeZoneName: "America/Los_Angeles",
  currentStatus: {
    lastReading: {
      timestamp: "2020-02-09T15:40:00",
      waterHeight: 9.16,
      groundHeight: 0,
      waterDischarge: 8050,
      isDeleted: false,
    },
    floodLevel: "Normal",
    levelTrend: "Steady",
    waterTrend: {
      trendValues: [
        0.23999999999999488,
        -0.05999999999999872,
        0,
        -0.02999999999999936,
      ],
      trendValue: -0.02999999999999936,
    },
  },
};

export const svpaGage = {
  id: "SVPA-26",
  locationName: "W Snoqualmie River Rd NE at Blue Heron Golf Course",
  latitude: 47.6251582559601,
  longitude: -121.933527253568,
  isOffline: false,
  rank: 55,
  yMin: 61.3,
  yMax: 74.3,
  groundHeight: 63.02,
  roadSaddleHeight: 66.34,
  roadDisplayName: "W Snoqualmie River Rd NE",
  deviceTypeName: "Senix",
  timeZoneName: "America/Los_Angeles",
  locationImages: ["29/b811050f-fe31-4435-b477-d34e1bac85cd.jpeg"],
  currentStatus: {
    lastReading: {
      timestamp: "2020-02-09T15:50:08",
      waterHeight: 66.77,
      groundHeight: 63.02,
      batteryMillivolt: 3614,
      roadSaddleHeight: 66.34,
      isDeleted: false,
    },
    floodLevel: "Flooding",
    levelTrend: "Falling",
    waterTrend: {
      trendValues: [
        -0.24000000000012278,
        -0.24000000000003752,
        -0.19999999999998863,
        -0.18000000000000682,
      ],
      trendValue: -0.18000000000000682,
    },
  },
};

export const gageList = [usgsGage, svpaGage];

export const mockReadings = [
  {
    timestamp: "2019-12-20T23:45:03",
    waterHeight: 71.42,
    groundHeight: 63.02,
    batteryMillivolt: 3891,
    roadSaddleHeight: 66.34,
    isDeleted: false,
  },
  {
    timestamp: "2019-12-20T23:30:38",
    waterHeight: 71.37,
    groundHeight: 63.02,
    batteryMillivolt: 3885,
    roadSaddleHeight: 66.34,
    isDeleted: false,
  },
  {
    timestamp: "2019-12-20T23:15:02",
    waterHeight: 71.29,
    groundHeight: 63.02,
    batteryMillivolt: 3885,
    roadSaddleHeight: 66.34,
    isDeleted: false,
  },
  {
    timestamp: "2019-12-20T23:00:03",
    waterHeight: 71.23,
    groundHeight: 63.02,
    batteryMillivolt: 3881,
    roadSaddleHeight: 66.34,
    isDeleted: false,
  },
];

export const gageReadings = {
  readings: mockReadings,
  gage: svpaGage,
  lastReadingId: 132759,
};
// export const chartDataWithDetails = Object.assign({}, chartData, {
//   locationData: svpaGage,
// });

export const gageReadingsNoData = {
  noData: true,
};

export const gageListWithReadings = [
  { ...svpaGage, recentReadings: mockReadings },
];
