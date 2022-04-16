import GageApi from "../gageApi";
import queryStringUtil from "query-string";
import Gage from "../../models/gage";
import ChartData from "../../models/chartData";

import { gageReadings, gageListWithReadings } from "../../test/mockData";
const consoleTmp = console;
beforeEach(() => {
  fetch.resetMocks();
  GageApi.clearCache();
});
afterEach(() => {
  console = consoleTmp;
});
describe("getGageList", () => {
  it("getGageList defaults", async () => {
    fetch.once("[]");
    const resp = await GageApi.getGageList();
    expect(fetch.mock.calls.length).toEqual(1);
    const callPath = queryStringUtil.parseUrl(fetch.mock.calls[0][0]);
    expect(callPath.query.isActiveSensors).toEqual("true");
    expect(callPath.query.locationDataOnly).toEqual("false");
    expect(callPath.query.id).toEqual("");
  });

  it("constructs objects", async () => {
    fetch.once(JSON.stringify(gageListWithReadings));
    const resp = await GageApi.getGageList();
    expect(fetch.mock.calls.length).toEqual(1);
    expect(resp[0]).toBeInstanceOf(Gage);
    expect(resp[0].chartData).toBeInstanceOf(ChartData);
    expect(resp[0].chart).toEqual(undefined);
  });

  it("handles 200 response with error object", async () => {
    fetch.once(JSON.stringify({ error: "test error" }));
    let resp;
    let error;
    global.console = { error: jest.fn() };
    try {
      resp = await GageApi.getGageList();
    } catch (e) {
      error = e;
    }
    expect(resp).toEqual(undefined);
    expect(error.status).toEqual(500);
  });

  it("handles 500 response", async () => {
    fetch.mockResponse("System error", { status: 500 });
    let resp;
    let error;
    global.console = { error: jest.fn() };
    try {
      resp = await GageApi.getGageList();
    } catch (e) {
      error = e;
    }
    expect(resp).toEqual(undefined);
    expect(error.status).toEqual(500);
  });

  it("handles 404 response", async () => {
    fetch.mockResponse("Not found", { status: 404 });
    let resp;
    let error;
    global.console = { error: jest.fn() };
    try {
      resp = await GageApi.getGageList();
    } catch (e) {
      error = e;
    }
    expect(resp).toEqual(undefined);
    expect(error.status).toEqual(404);
  });

  it("getGageList with params", async () => {
    fetch.once("[]");
    const resp = await GageApi.getGageList({
      active: false,
      locationDataOnly: true,
      gageIdFilter: "USGS-1234",
      force: false,
    });
    expect(fetch.mock.calls.length).toEqual(1);
    const callPath = queryStringUtil.parseUrl(fetch.mock.calls[0][0]);
    expect(callPath.query.isActiveSensors).toEqual("false");
    expect(callPath.query.locationDataOnly).toEqual("true");
    expect(callPath.query.id).toEqual("USGS-1234");
  });

  it("getGageList with cache", async () => {
    fetch.once("[]");
    const resp = await GageApi.getGageList();
    const resp2 = await GageApi.getGageList();
    expect(fetch.mock.calls.length).toEqual(1);
  });

  it("getGageList skips cache with force=true", async () => {
    fetch.once("[]").once("[]");
    const resp = await GageApi.getGageList();
    const resp2 = await GageApi.getGageList({ force: true });
    expect(fetch.mock.calls.length).toEqual(2);
    expect(resp.length).toEqual(resp2.length);
  });
});

describe("getChartData", () => {
  const chartParams = {
    gageId: "TEST-123",
    force: false,
    apiStartDateString: "2019-09-09T00:00:00",
    apiEndDateString: "2019-12-09T00:00:00",
    isNow: true,
  };

  it("getChartData isNow=false", async () => {
    fetch.once(JSON.stringify(gageReadings));
    const resp = await GageApi.getChartData(
      Object.assign({}, chartParams, { isNow: false })
    );
    expect(fetch.mock.calls.length).toEqual(1);
    const requestUrl = fetch.mock.calls[0][0];
    const callPath = queryStringUtil.parseUrl(requestUrl);
    expect(callPath.query.fromDateTime).toEqual("2019-09-09T00:00:00");
    expect(callPath.query.toDateTime).toEqual("2019-12-09T00:00:00");
    expect(callPath.query.id).toEqual("TEST-123");
    expect(callPath.query.showDeletedReadings).toEqual("false");
    expect(Object.keys(GageApi._requestCache)[0]).toEqual(requestUrl);
  });

  it("getChartData isNow=true cache hit", async () => {
    fetch.once(JSON.stringify(gageReadings));
    const resp = await GageApi.getChartData(
      Object.assign({}, chartParams, { isNow: true })
    );
    const resp2 = await GageApi.getChartData(
      Object.assign({}, chartParams, { isNow: true })
    );
    expect(fetch.mock.calls.length).toEqual(1);
    expect(resp).toHaveProperty("chartData");
    expect(resp2).toHaveProperty("chartData");
  });

  it("getChartData cache hit", async () => {
    fetch.once(JSON.stringify(gageReadings));
    const resp = await GageApi.getChartData(
      Object.assign({}, chartParams, { isNow: false })
    );
    const resp2 = await GageApi.getChartData(
      Object.assign({}, chartParams, { isNow: false })
    );
    expect(fetch.mock.calls.length).toEqual(1);
    expect(resp).toHaveProperty("chartData");
    expect(resp).toHaveProperty("gage");
    expect(resp2).toHaveProperty("chartData");
    expect(resp2).toHaveProperty("gage");
  });

  it("getChartData force cache miss", async () => {
    fetch.once(JSON.stringify(gageReadings)).once(JSON.stringify(gageReadings));
    const resp = await GageApi.getChartData(
      Object.assign({}, chartParams, { isNow: false })
    );
    const resp2 = await GageApi.getChartData(
      Object.assign({}, chartParams, { isNow: false, force: true })
    );
    expect(fetch.mock.calls.length).toEqual(2);
    expect(resp).toHaveProperty("chartData");
    expect(resp).toHaveProperty("gage");
    expect(resp2).toHaveProperty("chartData");
    expect(resp2).toHaveProperty("gage");
  });
});
