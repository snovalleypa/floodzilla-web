import React, { useEffect, useState } from "react";
import { useHistory, useLocation } from "react-router-dom";
import moment from "moment-timezone";
import useInterval from "../lib/useInterval"
import Constants from "../constants";

export const DebugContext = React.createContext(null);

export function DebugContextProvider(props) {

  const URL_FAKE_NOW = "fakeNow";

  const MOVE_MINUTES = 60;

  const SLOW_URL_DELAY = 7500;
  const SLOW_URL_PREFIXES = [
//    "https://prodplanreadingsvc.azurewebsites.net/api/GetGageStatusAndRecentReadings?regionId=1",
  ];

  const ERROR_URL_PREFIXES = [
//    "https://prodplanreadingsvc.azurewebsites.net/api/GetGageStatusAndRecentReadings?regionId=1",
  ];

  const location = useLocation();
  const history = useHistory();
  const regionTimeZone = window.regionSettings.timezone;

  // we want to make sure to capture this before anything else happens on the page...
  const [ initialQuery, setInitialQuery ] = useState(new URLSearchParams(location.search));

  const [ fakeNow, _setFakeNow ] = useState(null);
  const [ fastforward, setFastforward ] = useState(false);

  const setFakeNow = (fn) => {
    _setFakeNow(fn);
  }

  const processArgs = () => {
    const query = initialQuery;
    if (!query) {
      return;
    }
    const f = query.get(URL_FAKE_NOW);
    if (f) {
      if (f === 'now') {
        setFakeNow(null);
      } else {
        const time = moment.tz(f, regionTimeZone);
        setFakeNow(time);

        // remove the query string from the URL so it doesn't accidentally get copy/pasted
        history.replace(location.pathname);
      }
    }
    setInitialQuery(null);
  }

  useEffect(() => {
    processArgs();
  }, [initialQuery]);  // eslint-disable-line react-hooks/exhaustive-deps

  useInterval(() => {
    if (fakeNow && fastforward) {
      moveFakeNowRight();
    }
  }, 5000);

  const isDebugMode = () => {
    if (initialQuery) {
      processArgs();
    }
    return (fakeNow !== null);
  }

  const moveFakeNowLeft = () => {
    setFakeNow(fakeNow.clone().subtract(moment.duration(MOVE_MINUTES, 'minutes')));
  }
  const moveFakeNowRight = () => {
    setFakeNow(fakeNow.clone().add(moment.duration(MOVE_MINUTES, 'minutes')));
  }
  const clearFakeNow = () => {
    setFakeNow(null);
  }
  const getDebugUrl = () => {
    return "/?" + URL_FAKE_NOW + "=" + fakeNow.format('YYYY-MM-DDTHH:mm:ss');
  }

  const getNow = () => {
    if (initialQuery) {
      processArgs();
    }
    if (fakeNow !== null) {
      return fakeNow.clone();
    }
    return moment().tz(regionTimeZone);
  }

  const shouldDelay = (url) => {
    let delay = false;
    SLOW_URL_PREFIXES.forEach(u => {
      if (url.startsWith(u)) {
        delay = true;
      }
    });
    return delay;
  }

  const shouldFail = (url) => {
    let delay = false;
    ERROR_URL_PREFIXES.forEach(u => {
      if (url.startsWith(u)) {
        delay = true;
      }
    });
    return delay;
  }

  const sleep = (delay) => {
    return new Promise(resolve => setTimeout(resolve, delay));
  }

  const logCall = (msg) => {
    if (Constants.LOG_FETCH_CALLS) {
      console.log(msg);
    }
  }

  const debugFetch = async (url, options) => {
    logCall('== debug.debugFetch == ' + options.method + ' ' + url);
    if (shouldFail(url)) {
      logCall('== FAILING         == ' + options.method + ' ' + url);
      throw new Error("An error occurred");
    }
    if (shouldDelay(url)) {
      logCall('== DELAYING         == ' + options.method + ' ' + url);
      await sleep(SLOW_URL_DELAY);
    }
    return fetch(url, options);
  }

  return (
  <DebugContext.Provider
          value = { {
            processArgs: processArgs,
            isDebugMode: isDebugMode(),
            fakeNow: fakeNow,
            setFakeNow: setFakeNow,
            fastforward: fastforward,
            setFastforward: setFastforward,
            moveFakeNowLeft: () => { moveFakeNowLeft(); },
            moveFakeNowRight: () => { moveFakeNowRight(); },
            clearFakeNow: () => { clearFakeNow(); },
            getDebugUrl: () => { return getDebugUrl(); },
            getNow: () => { return getNow(); },
            debugFetch: debugFetch,
          } }
  >
    {props.children}
  </DebugContext.Provider>
  );
};
