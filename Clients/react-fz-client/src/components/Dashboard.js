import React, { useContext, useEffect, useState } from "react";
import DashboardGage from "./DashboardGage";
import GageDetails from "./GageDetails";
import Map from "./Map";
import Footer from "./Footer";
import Loading from "./Loading";
import Constants from "../constants";
import * as utils from "../lib/utils";
import queryStringUtil from "query-string";
import useInterval from "../lib/useInterval";
import { GageDataContext } from "./GageDataContext";

import {
  Switch,
  Route,
  useLocation,
  useRouteMatch,
  useHistory,
} from "react-router-dom";

export default function Dashboard({ gageList, gageStatusList, isMobile, reloadGageList }) {
  const [gageSelected, setGageSelected] = useState(null);
  const { pathname } = useLocation();
  const pathnameChanged = utils.useCompare(pathname);
  const queryParams = queryStringUtil.parse(useLocation().search);
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
        <div className="row"
        >
          {!isMobile && (
              <div
                className="col-md-4 col-lg-4 map-sticky d-sm-none d-md-block"
                id="map-area"
              >
                <span id="lbl_map"></span>
                <div id="map" className="map">
                  <Map
                    gageList={gageList}
                    gageStatusList={gageStatusList}
                    gageSelected={gageSelected}
                    viewGageDetails={viewGageDetails}
                  />
                </div>
              </div>
          )}
          <div
            className="col-lg-8 col-md-8 offset-lg-4 offset-md-4 col-sm-12 col-xs-12"
            id="mainArea"
          >
            <div className="overlay">
              <div className="loader"></div>
            </div>
            <span id="lbl_msg"></span>
            <input type="hidden" id="Region" value={window.regionSettings.id} />
            <div id="div_content">
              <Switch>
                <Route exact path="/">
                  <div id="div_dashboard">
                    {gageList &&
                      gageList.map(gage => (
                        <DashboardGage
                          gage={gage}
                          gageStatus={gageData.getGageStatus(gage.id)}
                          key={gage.id}
                          viewDetails={() => viewGageDetails(gage)}
                          resize={pathname === "/"}
                          isMobile={isMobile}
                        />
                      ))}
                    {!gageList && <Loading />}
                  </div>
                </Route>
                <Route
                  path="/gage/:gageId"
                  render={({ match }) => {
                    return (
                      <div id="div_detail">
                        {gageList && <GageDetails
                          gageFromDashboard={
                            gageList &&
                            gageList.find(
                              g =>
                                g.id === match.params.gageId
                            )
                          }
                          gageStatus={gageData.getGageStatus(match.params.gageId)}
                          gageList={gageList}
                          viewGageDetails={viewGageDetails}
                          isMobile={isMobile}
                        />
                        }
                      </div>
                    );
                  }}
                />
              </Switch>
            </div>
            {gageList && <Footer />}
          </div>
        </div>
      </div>
    </div>
  );
}
