import React, { useContext, useEffect, useState } from "react";
import {
  useLocation,
  useRouteMatch,
  Switch,
  Route,
  Redirect,
} from "react-router-dom";
import 'bootstrap/dist/css/bootstrap.min.css';
import "../style/Dashboard.css";
import "../style/DashboardMenu.css";
import Dashboard from "./Dashboard";
import FloodView from "./FloodView";
import GageView from "./GageView";
import Forecast from "./Forecast";
import Header from "./Header";
import PrivacyPolicy from "./Privacy";
import TermsOfService from "./Terms";
import queryStringUtil from "query-string";
import Constants from "../constants";
import * as utils from "../lib/utils";
import ChangeEmail from "./ChangeEmail";
import SetPassword from "./SetPassword";
import VerifyEmail from "./VerifyEmail";
import UserProfile from "./UserProfile";
import Subscriptions from "./Subscriptions";
import Unsubscribe from "./Unsubscribe";
import { GageDataContext, GageDataResult } from "./GageDataContext";
import ChartRange from "../lib/chartRange";
import { DebugContext } from "./DebugContext";
import ConnectionError from "./ConnectionError";

// This is a hack.
var _showDeletedReadings = false;
export function showDeletedReadings() {
  return _showDeletedReadings;
}

export default function Main() {
  const gageData = useContext(GageDataContext);
  const debug = useContext(DebugContext);

  // gageList/gageStatusList hold last response from api
  const [gageList, setGageList] = useState();
  const [gageStatusList, setGageStatusList] = useState();
  // filteredGageList is the list shown and passed down
  const [filteredGageList, setFilteredGageList] = useState();
  const [filter, setFilter] = useState({ active: true });
  const [isMobile, setIsMobile] = useState(false);
  const [loadError, setLoadError] = useState(null);
  const location = useLocation();
  const locationChanged = utils.useCompare(location);
  const { pathname } = useLocation();
  const { gageId } = (
    useRouteMatch({
      path: "/gage/:gageId/",
    }) || { params: {} }
  ).params;

  //$ how does this get retriggered when the list is loaded
  useEffect(() => {
    const result = gageData.gageListResult;
    switch (result.result) {
      case GageDataResult.OK:
        setGageList(result.value);
        break;
      case GageDataResult.PENDING:
        break;
      default:
      case GageDataResult.ERROR:
        setLoadError(result.error || "An error occurred.");
        break;
    }
  }, [gageData.gageListResult]);

  useEffect(() => {
    const result = gageData.gageStatusResult;
    switch (result.result) {
      case GageDataResult.OK:
        setGageStatusList(result.value);
        break;
      case GageDataResult.PENDING:
        break;
      default:
      case GageDataResult.ERROR:
        setLoadError(result.error || "An error occurred.");
        break;
    }
  }, [gageData.gageStatusResult]);

  // called once
  useEffect(() => {
    ChartRange.setDebug(debug);
    window.addEventListener("resize", onResize);
    onResize();
    return () => {
      window.removeEventListener("resize", onResize);
    };
  }, [debug]);

  useEffect(() => {
    if (loadError && !gageList) {
    }
  }, [loadError, gageList]);

  // for everything but / scroll to the top when our route changes
  useEffect(() => {
    if (pathname !== "/") {
      window.scrollTo(0, 0);
    }
  }, [pathname]);

  // manage page title
  useEffect(() => {
    let title;
    if (pathname === "/") {
      title = Constants.HOME_PAGE_TITLE;
    } else if (gageId && gageList) {
      const gage = gageList.find(
        g => gageId === g.id
      );
      if (gage) {
        title = gage.locationName + Constants.PAGE_TITLE_SUFFIX;
      }
    }
    if (!title) {
      title = Constants.HOME_PAGE_TITLE;
    }
    document.title = title;
  }, [pathname, gageId, gageList]);

  useEffect(() => {
    // load without location data (fast load)
    // if loading a detail page only load that gage
    if (!gageList) {
      loadGageList({
        locationDataOnly: true,
        gageIdFilter: gageId || "",
      });
    }
  }, [gageList, gageId]);  // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {});

  useEffect(() => {
    if (gageList && gageList._locationDataOnly) {
      // load with full location data
      loadGageList({ locationDataOnly: false });
    }
  }, [gageList]);  // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (gageList) {
      setFilteredGageList(applyClientGageListFilter(gageList, filter));
    }
  }, [filter, gageList]);  // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (locationChanged) {
      const queryParams = queryStringUtil.parse(location.search);
      if ((queryParams.showAll === "true") === filter.active) {
        // toggle
        setFilteredGageList(null);
        setFilter(Object.assign({}, filter, { active: !filter.active }));
      }
      if (queryParams.showDeleted === "true") {
        _showDeletedReadings = true;
      } else {
          _showDeletedReadings = false;
      }
    }
  }, [locationChanged, location, filter]);

  const loadGageList = async function({ locationDataOnly, gageIdFilter }) {
    let newGageList = gageList;
    if (!newGageList || newGageList.error) {
//      console.error("failed to load gages (json error)", newGageList.error);
//      console.error('returned null');
      setLoadError(newGageList);
      return;
    }
    newGageList._activeFilter = false;
    newGageList._locationDataOnly = locationDataOnly;
    setGageList(newGageList);
  };

  const reloadGageList = function() {
    loadGageList({ locationDataOnly: false });
  };

  const onSearchChange = function({ target }) {
    setFilter(Object.assign({}, filter, { search: target.value }));
  };

  const onResize = function() {
    setIsMobile(window.innerWidth <= Constants.MOBILE_SCREEN_LIMIT);
  };

  const applyClientGageListFilter = function(gageList, filter) {
    return gageList.filter(
      gage =>
        (!filter.search ||
          `${gage.id} ${gage.locationName}`.match(
            new RegExp(filter.search, "i")
          )) &&
        (!filter.active || gageIsRecentlyActive(gage))
    );
  };

  // Currently the API is setting isCurrentlyOffline for gages
  // with no readings in the past 24 hours...
  const gageIsRecentlyActive = gage => {
    return !gage.isCurrentlyOffline;
  };

  return (
    <div>
      <Header filter={filter} onSearchChange={onSearchChange} />
      <Switch>
        <Redirect from="/river/snoqualmie" to="/forecast" />
        <Redirect from="/station/*" to="/forecast" />
        <Redirect exact from="/index.html" to="/" />
        <Route path={["/forecast/:gageIds","/forecast"]}>
          <Forecast />
        </Route>
        <Route path="/privacy">
          <PrivacyPolicy />
        </Route>
        <Route path="/terms">
          <TermsOfService />
        </Route>
        <Route path="/floods">
          <FloodView gageList={gageList} />
        </Route>
        <Route path="/user/profile">
          <UserProfile />
        </Route>
        <Route path="/user/alerts">
          <Subscriptions />
        </Route>
        <Route path="/user/unsubscribe">
          <Unsubscribe />
        </Route>
        <Route path="/user/verifyemail">
          <VerifyEmail />
        </Route>
        <Route path={["/user/setpassword", "/user/createpassword", "/user/resetpassword"]}>
          <SetPassword />
        </Route>
        <Route path="/user/changeemail">
          <ChangeEmail />
        </Route>
        <Route path="/gage/:gageId">
          {(!loadError || gageList) && (
            <GageView
              gageId={gageId}
              gageList={filteredGageList}
              gageStatusList={gageStatusList}
              isMobile={isMobile}
              reloadGageList={reloadGageList}
            />
          )}
          {loadError && !gageList && (
            <div className="error-message">Error loading page.</div>
          )}
        </Route>
        <Route path="/">
          {(!loadError || gageList) && (
            <Dashboard
              gageList={filteredGageList}
              gageStatusList={gageStatusList}
              isMobile={isMobile}
              reloadGageList={reloadGageList}
            />
          )}
          {loadError && !gageList && (
            <div className="error-message">Error loading page.</div>
          )}
        </Route>
      </Switch>
    <ConnectionError />
    </div>
  );
}
