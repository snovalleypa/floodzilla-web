import Gage from "../gage";
import { svpaGage } from "../../test/mockData";

let testGage;

beforeEach(() => {
  testGage = Object.assign({}, svpaGage);
});

it("constructs from json", async () => {
  const gage = new Gage(testGage);
  expect(gage.locationName).not.toEqual(undefined);
  expect(gage.locationName).toEqual(testGage.locationName);
});

it("generates road status", async () => {
  const testName = "test name";
  testGage.roadSaddleHeight = "50";
  testGage.roadDisplayName = "test name";
  const gage = new Gage(testGage);
  const roadStatus = gage.roadStatus;
  expect(roadStatus.name).toEqual(testName);
  expect(roadStatus.level).toBeCloseTo(16.77);
});

it("generates null road status", async () => {
  testGage.roadSaddleHeight = undefined;
  testGage.roadDisplayName = undefined;
  const gage = new Gage(testGage);
  const roadStatus = gage.roadStatus;
  expect(roadStatus).toEqual(null);
});
