import React, { useContext, useEffect, useState } from "react";
import "../style/LocationDetails.css";
import GageDetails from "./GageDetails";
import Constants from "../constants";
import * as utils from "../lib/utils";
import queryStringUtil from "query-string";
import useInterval from "../lib/useInterval";
import { GageDataContext } from "./GageDataContext";

import {
  useLocation,
  useRouteMatch,
  useHistory,
} from "react-router-dom";

export default function GageView({ gageList, isMobile, reloadGageList }) {
  const [_gageSelected, setGageSelected] = useState(null);
  const { pathname, search } = useLocation();
  const pathnameChanged = utils.useCompare(pathname);
  const queryParams = queryStringUtil.parse(search);
  const routeMatch = useRouteMatch("/gage/:gageId");
  const routeMatchChanged = utils.useCompare(routeMatch);
  const history = useHistory();
  const [lastGageSelected, setLastGageSelected] = useState();

  const gageData = useContext(GageDataContext);

  useEffect(() => {
    if (
      routeMatchChanged &&
      routeMatch &&
      routeMatch.params.gageId &&
      gageList
    ) {
      const gageId = routeMatch.params.gageId;
      setLastGageSelected(gageId);
      const selGage = gageList.find(g => g.id === gageId);
      if (!selGage) {
        // Invalid gage ID.  Just force a reset.
        window.location = '/';
      }

      setGageSelected(selGage);
    } else {
      setGageSelected(null);
    }
  }, [routeMatchChanged, routeMatch, gageList]);

  useEffect(() => {
    if (!pathnameChanged) return;
    if (pathname === "/" && gageList) {
      if (lastGageSelected) {
        const gage = gageList.find(
          g =>
            g.id === lastGageSelected
        );
        if (gage) {
          const el = document.getElementById("gage_" + lastGageSelected);
          if (el) {
            el.scrollIntoView({ block: "center" });
          }
        }
        setLastGageSelected(null);
      }
    } else {
      setLastGageSelected(null);
      window.scrollTo(0, 0);
    }
  }, [pathnameChanged, pathname, gageList, lastGageSelected]);

  // reload gageList when user navigates back to /
  useEffect(() => {
    if (pathnameChanged && pathname === "/" && gageList) {
      reloadGageList();
    }
  }, [pathnameChanged, pathname, gageList, reloadGageList]);

  // reload gageList after timeout
  useInterval(() => {
    if (pathname === "/") {
      if (!document.hidden) {
        reloadGageList();
      }
    }
  }, Constants.DASHBOARD_DATA_REFRESH_RATE);

  const viewGageDetails = function(gage) {
    history.push(utils.generateGagePath({ gage, queryParams }));
  };

  return (
    <div className="Dashboard">
      <div className="container-fluid body-content">
        <div
          id="gageDetailsMainArea"
        >
          <div id="div_detail">
            {gageList && <GageDetails
              gageFromDashboard={
                gageList &&
                gageList.find(
                  g =>
                    g.id === routeMatch.params.gageId
                )
              }
              gageStatus={gageData.getGageStatus(routeMatch.params.gageId)}
              gageList={gageList}
              viewGageDetails={viewGageDetails}
              isMobile={isMobile}
            />
            }
          </div>
        </div>
      </div>
    </div>
  );
}
