import Constants from "../../constants";
import GageApi from "../gageApi";
import GageDataManager from "../gageDataManager";
import queryStringUtil from "query-string";
import { gageReadings, gageReadingsNoData } from "../../test/mockData";

beforeEach(() => {
  fetch.resetMocks();
  GageApi.clearCache();
  GageDataManager.deleteAllListeners();
});
const testGageId1 = "SVPA-26";
const testGageId2 = "TEST-456";
const gageReadings2 = Object.assign({}, gageReadings, {
  gage: Object.assign({}, gageReadings.gage, {
    id: testGageId2,
  }),
});
const chartParams = {
  gageId: testGageId1,
  force: false,
  apiStartDateString: "2019-12-09T00:00:00",
  apiEndDateString: "2019-12-11T00:00:00",
};

it("getHistoricalChartData ", async () => {
  fetch.once(JSON.stringify(gageReadings));
  const resp = await GageDataManager.getHistoricalChartData(chartParams);
  expect(fetch.mock.calls.length).toEqual(1);
  const requestUrl = fetch.mock.calls[0][0];
  const callPath = queryStringUtil.parseUrl(requestUrl);
  expect(callPath.query.fromDateTime).toEqual("2019-12-09T00:00:00");
  expect(callPath.query.toDateTime).toEqual("2019-12-11T00:00:00");
  expect(callPath.query.id).toEqual(testGageId1);
  expect(callPath.query.lastReadingId).toEqual(undefined);
  expect(Object.keys(GageApi._requestCache)[0]).toEqual(requestUrl);
});

it("getLiveChartData", async () => {
  fetch.once(JSON.stringify(gageReadings));
  const resp = await GageDataManager.getLiveChartData(chartParams);
  expect(fetch.mock.calls.length).toEqual(1);
  const requestUrl = fetch.mock.calls[0][0];
  const callPath = queryStringUtil.parseUrl(requestUrl);
  expect(callPath.query.fromDateTime).toEqual("2019-12-09T00:00:00");
  expect(callPath.query.toDateTime).toEqual(
    Constants.CHART_API_NOW_DATE_STRING
  );
  expect(callPath.query.id).toEqual(testGageId1);
  expect(callPath.query.lastReadingId).toEqual(undefined);
  expect(Object.keys(GageApi._requestCache)[0]).toEqual(requestUrl);
});

it("getLiveChartData uses cache with more recent start date", async () => {
  fetch.once(JSON.stringify(gageReadings)).once(JSON.stringify(gageReadings));
  const resp = await GageDataManager.getLiveChartData(chartParams);
  const resp2 = await GageDataManager.getLiveChartData(
    Object.assign({}, chartParams, {
      apiStartDateString: "2019-12-10T00:00:00",
    })
  );
  expect(fetch.mock.calls.length).toEqual(1);
  // expect(fetch.mock.calls[0][0]).toEqual(fetch.mock.calls[1][0]);
});

it("getLiveChartData skips cache with less recent start date", async () => {
  fetch.once(JSON.stringify(gageReadings)).once(JSON.stringify(gageReadings));
  const resp = await GageDataManager.getLiveChartData(chartParams);
  const resp2 = await GageDataManager.getLiveChartData(
    Object.assign({}, chartParams, {
      apiStartDateString: "2019-12-08T00:00:00",
    })
  );
  expect(fetch.mock.calls.length).toEqual(2);
});

it("creates and deletes listener", () => {
  const listenerRef = GageDataManager.addListener({
    type: GageDataManager.types.Gage,
    gageId: testGageId1,
    callback: data => {
      return data;
    },
  });
  expect(GageDataManager._gageListeners.size).toEqual(1);
  listenerRef.delete();
  expect(GageDataManager._gageListeners.size).toEqual(0);
});

it("checks for updates", async () => {
  fetch
    .once(JSON.stringify(gageReadings))
    .once(JSON.stringify(gageReadings2))
    .once(JSON.stringify(gageReadingsNoData))
    .once(JSON.stringify(gageReadingsNoData));
  const resp1 = await GageDataManager.getLiveChartData({
    ...chartParams,
    gageId: testGageId1,
  });
  const resp2 = await GageDataManager.getLiveChartData({
    ...chartParams,
    gageId: testGageId2,
  });
  const chartCallback = jest.fn(x => x.gage.id);
  const gageCallback = jest.fn(x => x.id);

  GageDataManager.addListener({
    type: GageDataManager.types.Gage,
    gageId: testGageId1,
    callback: gageCallback,
  });
  GageDataManager.addListener({
    type: GageDataManager.types.Chart,
    gageId: testGageId1,
    callback: chartCallback,
  });
  GageDataManager.addListener({
    type: GageDataManager.types.Chart,
    gageId: testGageId2,
    callback: chartCallback,
  });
  await GageDataManager.checkForUpdates();
  expect(gageCallback.mock.calls.length).toBe(1);
  expect(chartCallback.mock.calls.length).toBe(2);
  expect(gageCallback.mock.results[0].value).toBe(testGageId1);
  expect(chartCallback.mock.results[0].value).toBe(testGageId1);
  expect(chartCallback.mock.results[1].value).toBe(testGageId2);
});
