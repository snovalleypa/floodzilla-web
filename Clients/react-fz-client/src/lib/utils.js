import { useRef, useEffect } from "react";
import queryStringUtil from "query-string";
import moment from "moment-timezone";

export function generateGagePath({ gage, queryParams = {} }) {
  let queryString = queryStringUtil.stringify(queryParams);
  if (queryString.length) {
    queryString = "?" + queryString;
  }
  if (!gage) return `/${queryString}`;
  return `/gage/${gage.id}${queryString}`;
}

export function formatHeight(height) {
  return height.toLocaleString(undefined, { maximumFractionDigits: 2 }) + " ft";
}
export function formatFlow(flow) {
  return flow.toLocaleString(undefined, { maximumFractionDigits: 0 }) + " cfs";
}
export function formatTrend(trend) {
  return trend.toLocaleString(undefined, { maximumFractionDigits: 2 }) + " ft/hr";
}

export function formatReadingTime(timeZone, timestamp) {
  const timeAgo = moment() - moment.tz(timestamp, timeZone);
  let formatString;
  if (timeAgo < moment.duration(12, "h")) {
    formatString = "h:mm a";
  } else if (timeAgo < moment.duration(2, "months")) {
    formatString = "ddd MM/DD h:mm a";
  } else {
    formatString = "YYYY/MM/DD h:mm a";
  }
  return moment(timestamp).format(formatString);
}

export function formatLastReadingTime(gage, gageStatus) {
  return formatReadingTime(gage.timeZoneName, gageStatus.lastReading.timestamp);
}

// https://stackoverflow.com/questions/53446020/how-to-compare-oldvalues-and-newvalues-on-react-hooks-useeffect
export function usePrevious(value) {
  const ref = useRef();
  useEffect(() => {
    ref.current = value;
  });
  return ref.current;
}

export function useCompare(val) {
  const prevVal = usePrevious(val);
  return prevVal !== val;
}

export function isNullOrUndefined(val) {
  return val === null || val === undefined;
}
