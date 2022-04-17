import ChartData from "../chartData";
import moment from "moment-timezone";
import { mockReadings, svpaGage } from "../../test/mockData";

let mockGage;

beforeEach(() => {
  mockGage = Object.assign({}, svpaGage);
});

it("constructs from json", async () => {
  const chartData = new ChartData(
    mockGage,
    mockReadings,
    svpaGage.timeZoneName
  );
  expect(chartData.hasData).toBeTruthy();
});

it("calculates crest", async () => {
  const readings = [1, 2, 2, 1]
    .map((waterHeight, i) => {
      return {
        waterHeight,
        timestamp: moment
          .tz("2019-12-26T01:00:00", svpaGage.timeZoneName)
          .add(i, "minutes")
          .valueOf(),
      };
    })
    .reverse();
  const chartData = new ChartData(mockGage, readings, mockGage.timeZoneName);
  const crest = chartData.calcCrest();
  expect(crest.reading).toEqual(2);
  expect(crest.timestamp.valueOf()).toEqual(
    moment
      .tz("2019-12-26T01:00:00", svpaGage.timeZoneName)
      .add(1, "m")
      .valueOf()
  );
});

it("doesn't calculates crest when time gap large", async () => {
  const readings = [1, 2, 2, 1]
    .map((waterHeight, i) => {
      return {
        waterHeight,
        timestamp: moment("2019-12-26T01:00:00Z")
          .add(i * 130, "minutes") // 70 minutes between reading
          .valueOf(),
      };
    })
    .reverse();
  const chartData = new ChartData(mockGage, readings, mockGage.timeZoneName);
  const crest = chartData.calcCrest();
  expect(crest).toEqual(null);
});

it("doesn't calculates crest at beginning of range", async () => {
  const readings = [3, 3, 2, 1]
    .map((waterHeight, i) => {
      return {
        waterHeight,
        timestamp: moment("2019-12-26T01:00:00Z")
          .add(i, "minutes")
          .valueOf(),
      };
    })
    .reverse();
  const chartData = new ChartData(mockGage, readings, mockGage.timeZoneName);
  const crest = chartData.calcCrest();
  expect(crest).toEqual(null);
});

it("doesn't calculates crest at end of range", async () => {
  const readings = [1, 2, 3, 3]
    .map((waterHeight, i) => {
      return {
        waterHeight,
        timestamp: moment("2019-12-26T01:00:00Z")
          .add(i, "minutes")
          .valueOf(),
      };
    })
    .reverse();
  const chartData = new ChartData(mockGage, readings, mockGage.timeZoneName);
  const crest = chartData.calcCrest();
  expect(crest).toEqual(null);
});

it("doesn't calculates crest if flat", async () => {
  const readings = [2, 2, 2, 2]
    .map((waterHeight, i) => {
      return {
        waterHeight,
        timestamp: moment("2019-12-26T01:00:00Z")
          .add(i, "minutes")
          .valueOf(),
      };
    })
    .reverse();
  const chartData = new ChartData(mockGage, readings, mockGage.timeZoneName);
  const crest = chartData.calcCrest();
  expect(crest).toEqual(null);
});

it("filters to calculates crest", async () => {
  const readings = [1, 4, 1, 1, 3, 2, 1, 1, 2, 1]
    .map((waterHeight, i) => {
      return {
        waterHeight,
        timestamp: moment("2019-12-26T01:00:00Z")
          .add(i * 10, "minutes")
          .valueOf(),
      };
    })
    .reverse();
  const chartData = new ChartData(mockGage, readings, mockGage.timeZoneName);
  const crest = chartData.calcCrest({
    startDate: moment("2019-12-26T01:20:00Z").valueOf(),
  });
  expect(crest.timestamp.valueOf()).toEqual(
    moment("2019-12-26T01:40:00Z").valueOf()
  );
  expect(crest.reading).toEqual(3);
});
