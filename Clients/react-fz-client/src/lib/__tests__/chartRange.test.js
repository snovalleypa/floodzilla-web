import ChartRange from "../chartRange";
import moment from "moment";

it("default range constructs", () => {
  const range = new ChartRange();
  expect(range.isNow).toEqual(true);
  expect(range.days).toEqual(2);
  expect(
    moment
      .duration(
        moment(range.apiEndDateString) - moment(range.apiStartDateString)
      )
      .asDays()
  ).toBeCloseTo(3, 1);
  expect(
    moment.duration(range.inputEndDate - range.inputStartDate).asDays()
  ).toBeCloseTo(1, 1);
  expect(
    moment.duration(range.chartEndDate - range.chartStartDate).asDays()
  ).toBeCloseTo(2, 1);
});

it("changes dates", () => {
  const range = new ChartRange();
  range.changeDates(moment("2019-11-03"), moment("2019-11-06")); // daylight saving time ended durning this range
  expect(range.isNow).toEqual(false);
  expect(range.days).toEqual(4);
  expect(
    moment
      .duration(
        moment(range.apiEndDateString) - moment(range.apiStartDateString)
      )
      .asDays()
  ).toBeCloseTo(4, 1);
  expect(
    moment.duration(range.inputEndDate - range.inputStartDate).asDays()
  ).toBeCloseTo(3, 1);
  expect(
    moment.duration(range.chartEndDate - range.chartStartDate).asDays()
  ).toBeCloseTo(4, 1);
});

it("changes days, now", () => {
  const range = new ChartRange();
  range.changeDays(4);
  expect(range.isNow).toEqual(true);
  expect(range.days).toEqual(4);
  expect(
    moment
      .duration(
        moment(range.apiEndDateString) - moment(range.apiStartDateString)
      )
      .asDays()
  ).toBeCloseTo(5, 1);
  expect(
    moment.duration(range.inputEndDate - range.inputStartDate).asDays()
  ).toBeCloseTo(3, 1);
  expect(
    moment.duration(range.chartEndDate - range.chartStartDate).asDays()
  ).toBeCloseTo(4, 1);
});

it("changes dates and days", () => {
  const range = new ChartRange();
  range.changeDates(moment("2019-12-01"), moment("2019-12-03"));
  range.changeDays(8);
  expect(range.apiEndDateString).toEqual("2019-12-04T00:00:00");
  expect(range.isNow).toEqual(false);
  expect(range.days).toEqual(8);
  expect(
    moment
      .duration(
        moment(range.apiEndDateString) - moment(range.apiStartDateString)
      )
      .asDays()
  ).toBeCloseTo(8, 1);
  expect(
    moment.duration(range.inputEndDate - range.inputStartDate).asDays()
  ).toBeCloseTo(7, 1);
  expect(
    moment.duration(range.chartEndDate - range.chartStartDate).asDays()
  ).toBeCloseTo(8, 1);
});

it("is cloned", () => {
  const range0 = new ChartRange();
  range0.changeDates(moment("2019-12-01"), moment("2019-12-03"));
  range0.changeDays(8);
  const range = range0.clone();
  range0.changeDays(2); // should have no effect on clone
  expect(range.apiEndDateString).toEqual("2019-12-04T00:00:00");
  expect(range.isNow).toEqual(false);
  expect(range.days).toEqual(8);
  expect(
    moment
      .duration(
        moment(range.apiEndDateString) - moment(range.apiStartDateString)
      )
      .asDays()
  ).toBeCloseTo(8, 1);
  expect(
    moment.duration(range.inputEndDate - range.inputStartDate).asDays()
  ).toBeCloseTo(7, 1);
  expect(
    moment.duration(range.chartEndDate - range.chartStartDate).asDays()
  ).toBeCloseTo(8, 1);
});
