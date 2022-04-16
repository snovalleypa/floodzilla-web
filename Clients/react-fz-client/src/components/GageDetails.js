import React, { useContext, useEffect, useRef, useState } from "react";
import { useHistory, useLocation, useParams, Link, Redirect } from "react-router-dom";

import GageChartController from "./GageChartController";
import "../style/LocationDetails.css";
import GageStatus from "./GageStatus";
import TrendIcon from "./TrendIcon";
import Loading from "./Loading";
import TimeAgo from "./TimeAgo";
import Constants from "../constants";
import * as utils from "../lib/utils";
import USGS_INFO from "../lib/usgsInfo";
import "react-dates/initialize";
import "react-dates/lib/css/_datepicker.css";
import queryStringUtil from "query-string";
import Map from "./Map";
import moment from "moment";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowLeft, faArrowCircleLeft, faArrowCircleRight } from '@fortawesome/free-solid-svg-icons'
import { SessionContext, SessionState } from "./SessionContext";
import SubscriptionManager from "../lib/SubscriptionManager";

export default function GageDetails({
  gageFromDashboard,
  gageStatus,
  gageList,
  isMobile,
  viewGageDetails,
}) {

  const session = useContext(SessionContext);
  const history = useHistory();

  const [gage, setGage] = useState(gageFromDashboard);
  const [liveStatus, setLiveStatus] = useState(null);
  const [currentStatus, setCurrentStatus] = useState(null);
  const [gageNotFound, setGageNotFound] = useState();
  const [subscriptionManager] = useState(new SubscriptionManager(session, window.regionSettings.id));
  const [isSubscribed, setIsSubscribed] = useState(false);

  const { gageId } = useParams();
  const queryParams = queryStringUtil.parse(useLocation().search);

  const currentRange = useRef(null);

  useEffect(() => {
    async function effect() {
      setIsSubscribed(await subscriptionManager.isSubscribed(gageId));
    }
    effect();
  }, [gageId, subscriptionManager]);

  useEffect(() => {
    if (gageFromDashboard) {
      setGage(gageFromDashboard);
    }
  }, [gageFromDashboard]);

  useEffect(() => {
    if (liveStatus) {
      setCurrentStatus(liveStatus);
    }
    else {
      setCurrentStatus(gageStatus);
    }
  }, [gageStatus, liveStatus]);

  const onLoading = (requestData, isLoading) => {
    if (isLoading) {
      startRotatingLoadingIcons();
    } else {
      stopRotatingLoadingIcons();
    }
  }

  const onGetAlerts = (e) => {
    e.preventDefault();
    if (session.sessionState !== SessionState.LOGGED_IN) {
      session.showLoginCreateModal({});
    } else if (!session.getEmailVerified()) {
      history.push('/user/alerts');
    } else if (isSubscribed) {
      history.push('/user/alerts');
    } else {
      history.push('/user/alerts?add=' + gageId);
    }
  }

  const chart = GageChartController({
    chartRangeRef: currentRange,
    gageId,
    gage: gageFromDashboard,
    gageStatus,
    isMobile,
    setGageNotFound,
    onLoading: onLoading,
    onLiveStatusUpdate: setLiveStatus,
  });

  if (gageNotFound) return <Redirect to="/" />;
  if (!gage || !currentStatus) return <Loading />;

  const gageIndex = gageList && gage && gageList.findIndex(g => g.id === gage.id);
  const upstreamGage = gageList && gageIndex > 0 && gageList[gageIndex - 1];
  const downstreamGage =
    gageIndex >= 0 &&
    gageList &&
    gageIndex + 1 < gageList.length &&
    gageList[gageIndex + 1];
  return (
    <div id="detail-content">
      <div className="row" id="location-back">
        <div
          className="col-4"
          style={{ marginBottom: 10 }}
        >
          <Link to={utils.generateGagePath({ queryParams })}>
            <FontAwesomeIcon icon={faArrowLeft}
              style={{ color: "#069ba8" }}
            />{" "}
            Back to List
          </Link>
        </div>
        <div className="col-4 text-center">
          {upstreamGage && (
            <Link
              to={utils.generateGagePath({ gage: upstreamGage, queryParams })}
            >
              <FontAwesomeIcon icon={faArrowCircleLeft}
                style={{ color: "#069ba8" }}
              />{" "}
                <span className="d-none d-sm-inline">Upstream gage</span>
                <span className="d-inline d-sm-none">Up</span>
            </Link>
          )}
        </div>
        <div className="col-4 float-right text-right">
          {downstreamGage && (
            <Link
              to={utils.generateGagePath({ gage: downstreamGage, queryParams })}
            >
              <div>
                <span className="d-none d-sm-inline">Downstream gage </span>
                <span className="d-inline d-sm-none">Down </span>
              <FontAwesomeIcon icon={faArrowCircleRight}
                  style={{ color: "#069ba8" }}
                />
              </div>
            </Link>
          )}
        </div>
      </div>
      <div className="row" id="title-content">
        <h2 className="Title" style={{ marginLeft: 10 }}>
          <span className="types types-text" style={{ float: "right" }}>
            {gage.id}
          </span>
          {gage.locationName}
        </h2>
        {/* <span className="nearby-places bottem-padding">{gage.nearPlaces}</span> */}
      </div>

      <br />
      {chart}

      <div className="row">
        {currentRange.current && currentRange.current.isNow && currentStatus.currentStatus && currentStatus.currentStatus.lastReading && (
          <CalloutReadingBox
            label={'Last Reading'}
            showTimeAgo={true}
            gage={gage}
            gageStatus={currentStatus}
            currentStatus={currentStatus.currentStatus}
            reading={currentStatus.currentStatus.lastReading}
          />
        )}
        {currentRange.current && !(currentRange.current.isNow) && currentStatus.peakStatus && currentStatus.peakStatus.lastReading && (
          <CalloutReadingBox
            label={'Peak'}
            showTimeAgo={false}
            gage={gage}
            gageStatus={currentStatus}
            currentStatus={currentStatus.peakStatus}
            reading={currentStatus.peakStatus.lastReading}
          />
        )}
        {gage && gage.locationImages && gage.locationImages.length > 0 && (
        <div
          className="col-lg-6 col-md-6 col-sm-6 col-xs-12 center-text"
          style={{ paddingBottom: 10 }}
          id="images-gallery2"
        >
          <img
            className="img-responsive mx-auto"
            style={{ display:"block",maxHeight: 400 }}
            src={
              Constants.GAGE_IMAGE_BASE_URL +
              "medium/" +
              gage.locationImages[0]
            }
            alt={gage.locationName + " photo"}
          />
        </div>
        )}
        <GageInfoBox gage={gage} />
        <StatusLevels gage={gage} session={session} isSubscribed={isSubscribed} onGetAlerts={onGetAlerts} />
      </div>
      {isMobile && gageList && gage && (
        <div style={{ width: "100%", height: 400 }}>
          <Map
            gageList={gageList}
            gageSelected={gage}
            viewGageDetails={viewGageDetails}
            isMobile={isMobile}
          />
        </div>
      )}

      {(upstreamGage || downstreamGage) && (
        <div className="row" style={{ margin: "auto" }}>
          {!upstreamGage && (
            <div className="col-lg-5 col-md-5 col-sm-6 col-xs-12"></div>
          )}
          {upstreamGage && (
            <LinkToNeighbor
              gage={upstreamGage}
              downstream={false}
              queryParams={queryParams}
            />
          )}
          {!downstreamGage && (
            <div className="col-lg-5 col-md-5 col-sm-6 col-xs-12"></div>
          )}
          {downstreamGage && (
            <LinkToNeighbor
              gage={downstreamGage}
              downstream={true}
              queryParams={queryParams}
            />
          )}
        </div>
      )}
    </div>
  );
}

function LinkToNeighbor({ gage, downstream = false, queryParams }) {
  return (
    <Link
      className={`${downstream &&
        "offset-lg-2 offset-md-2"} col-lg-5 col-md-5 col-sm-6 col-xs-12 gage-shadow btn-box`}
      to={utils.generateGagePath({ gage, queryParams })}
    >
      <div className="row">
        {!downstream && (
          <div className="col-lg-2 col-md-2 col-sm-2 col-xs-12">
            <img
              style={{ padding: 0, margin: 0 }}
              src="//floodzilla.com/images/DashboardIcons/go-to-upstream-36px.png"
              alt="Go to upstream"
            />
          </div>
        )}
        <div className="col-lg-10 col-md-10 col-sm-10 col-xs-12">
          <span className="btn-box-title">
            Go to {downstream ? "Downstream" : "Upstream"} Gage
          </span>
          <br />
          <span className="btn-box-location-title">{gage.locationName}</span>
        </div>
        {downstream && (
          <div className="col-lg-2 col-md-2 col-sm-2 col-xs-12">
            <img
              src="//floodzilla.com/images/DashboardIcons/go-to-downstream-36px.png"
              alt="Go to downstream"
            />
          </div>
        )}
      </div>
    </Link>
  );
}

function GageInfoBox({ gage }) {
  const [usgsInfo, setUsgsInfo] = useState();
  const [riverMile, setRiverMile] = useState(null);
  useEffect(() => {
    if (gage) {
      setUsgsInfo(USGS_INFO[gage.id]);
      if (gage.id && gage.id.match(/[0-9]/)) {
        setRiverMile(gage.id.match(/[0-9]+/g)[0]);
      } else {
        setRiverMile(null);
      }
    }
  }, [gage]);
  if (!gage) return null;
  return (
    <div className="col-lg-6 col-md-12 col-sm-12 col-xs-12">
      <ul className="gage-list-group list-group gage-shadow">
        <li className="list-group-item">
          <b>Gage Info</b>
        </li>
        <li className="list-group-item">
          Gage ID{" "}
          <span className="float-right">
            <b>{gage.id}</b>
          </span>
        </li>
        <li className="list-group-item">
          Operated by{" "}
          <span className="float-right">
            <b>{gage.id.match("USGS") ? "USGS" : "SVPA"}</b>
          </span>
        </li>
        {riverMile !== null && (
          <li className="list-group-item">
            River Mile{" "}
            <span className="float-right">
              <b>{riverMile}</b>
            </span>
          </li>
        )}
        {usgsInfo && (
          <li className="list-group-item">
            USGS Website{" "}
            <span className="float-right">
              <a
                rel="noopener noreferrer"
                href={`https://waterdata.usgs.gov/monitoring-location/${usgsInfo.id}`}
                target="_blank"
              >
                <b>Gage {usgsInfo.id}</b>
              </a>
            </span>
          </li>
        )}
        <li className="list-group-item">
          <span className="float-right">
            <a
              rel="noopener noreferrer"
              href={`https://google.com/maps?q=${gage.latitude},${gage.longitude}`}
              target="_blank"
            >
              <b>
                {gage.latitude.toFixed(6)},
                <br />
                {gage.longitude.toFixed(6)}
              </b>
            </a>
          </span>
          Latitude, <br />
          Longitude{" "}
        </li>
      </ul>
    </div>
  );
}

function StatusLevels({ gage, session, isSubscribed, onGetAlerts }) {
  
  if (!gage) {
    return null;
  }

  const formatToRoadText = (diff) => {
    if (Math.abs(diff) <.1) {
      return '(road saddle level)';
    }
    return '(' + Math.abs(diff).toFixed(1) + ' ft ' + (diff > 0 ? "below" : "above") + ' road saddle)'
  }
  
  const roadLevel = gage.roadSaddleHeight;
  let yellow = gage.yellowStage;
  const red = gage.redStage;
  let roadToYellow = null;
  let roadToRed = null;
  if (!yellow && !red) {
    return null;
  }
  if (yellow.toFixed(2) === red.toFixed(2)) {
    yellow = null;
  }
  if (yellow && roadLevel) {
    roadToYellow = roadLevel - yellow;
  }
  if (red && roadLevel) {
    roadToRed = roadLevel - red;
  }

  let addUrl="/user/alerts/?add=" + gage.id;
  let link = <Link to={addUrl}>Get Alerts when status changes</Link>;
  if (session.sessionState !== SessionState.LOGGED_IN) {
    link = <Link to="/" onClick={onGetAlerts}>Log in to get Alerts when status changes</Link>;
  } else if (isSubscribed) {
    link = <Link to="/user/alerts">Manage Alerts</Link>;
  }
  
  return (
    <div className="col-lg-6 col-md-12 col-sm-12 col-xs-12">
      <ul className="gage-list-group list-group gage-shadow">
        <li className="list-group-item">
          <b>Status Levels</b>
        </li>
        <li className="list-group-item d-flex flex-row justify-content-between">
          <div className="d-flex flex-column justify-content-center">
            <span className="water-status-box water-status-box-normal water-status-box-normal-text">Normal{" "}</span>
          </div>
          <div className="d-flex flex-column justify-content-center">
            <span className="float-right">
              Below {utils.formatHeight(yellow ?? red)}
            </span>
          </div>
        </li>
        {yellow &&
        <li className="list-group-item d-flex flex-row justify-content-between">
          <div className="d-flex flex-column justify-content-center">
            <span className="water-status-box water-status-box-warning water-status-box-warning-text">Near Flooding{" "}</span>
          </div>
          <div className="d-flex flex-column justify-content-center">
            <span className="float-right">
              At and above {utils.formatHeight(yellow)}<br />
              {roadLevel && <span>{formatToRoadText(roadToYellow)}</span>}
            </span>
          </div>
        </li>
        }
        <li className="list-group-item d-flex flex-row justify-content-between">
          <div className="d-flex flex-column justify-content-center">
            <span className="water-status-box water-status-box-danger water-status-box-danger-text">Flooding{" "}</span>
          </div>
          <div className="d-flex flex-column justify-content-center">
            <span className="float-right">
              At and above {utils.formatHeight(red)}<br />
              {roadLevel && <span>{formatToRoadText(roadToRed)}</span>}
            </span>
          </div>
        </li>
        <li className="list-group-item d-flex flex-row justify-content-center">
          {link}
        </li>
      </ul>
    </div>
  );
}
        
function CalloutReadingBox({ label, showTimeAgo, gage, gageStatus, currentStatus, reading }) {

  return (
    <div className="col-lg-6 col-md-12 col-sm-12 col-xs-12">
      <ul className="gage-list-group list-group gage-shadow">
        <li className="list-group-item">
          <b>{label}</b>
          <div className="float-right">
            <img
              src={`${Constants.RESOURCE_BASE_URL}/images/DashboardIcons/baseline-refresh-24px.png`}
              className="btn-refresh-hide"
              alt="Refresh Icon"
            />
          </div>
          <br />
          <span style={{ color: "rgba(68, 68, 68, 0.54)" }} id="lblTimeAgo">
            {showTimeAgo &&
              <TimeAgo
                dateTime={moment.tz(
                  reading.timestamp,
                  gage.timeZoneName
                )}
              />
            }
            {showTimeAgo && ' / '}
            <span style={{ whiteSpace: "nowrap" }}>
              {utils.formatReadingTime(gage.timeZoneName, reading.timestamp)}
            </span>{" "}
          </span>
        </li>
        <li className="list-group-item">
          Water Level{" "}
          <span className="float-right">
            {!utils.isNullOrUndefined(
              reading.waterHeight
            ) && (
              <b>{utils.formatHeight(reading.waterHeight)}</b>
            )}
          </span>
        </li>
        {/* reading.waterDischarge returns 0 when not available. should return null */}
        {!utils.isNullOrUndefined(reading.waterDischarge) &&
          reading.waterDischarge > 0 && (
            <li className="list-group-item">
              Water Flow{" "}
              <span className="float-right">
                <b>
                  {utils.formatFlow(reading.waterDischarge)}
                </b>
              </span>
            </li>
          )}
        <li className="list-group-item">
          Status{" "}
          <span className="float-right" style={{ left: "20px" }}>
            <GageStatus gage={gage} currentStatus={currentStatus} />
          </span>
        </li>
        {currentStatus.levelTrend &&
          currentStatus.waterTrend &&
          !utils.isNullOrUndefined(currentStatus.waterTrend.trendValue) && (
            <li className="list-group-item">
              Trend{" "}
              <span className="float-right">
                <b>{utils.formatTrend(currentStatus.waterTrend.trendValue)}</b>{" "}
                <TrendIcon trend={currentStatus.levelTrend} />
              </span>
            </li>
          )}
        {gage.roadSaddleHeight && gage.roadDisplayName && (
          <li className="list-group-item">
            Road{" "}
            <span className="float-right">
              <RoadStatus gage={gage} gageStatus={gageStatus} currentStatus={currentStatus} />
            </span>
          </li>
        )}
      </ul>
    </div>
  );
}

function RoadStatus({ gage, gageStatus, currentStatus }) {
  const roadStatus = gageStatus.calcRoadStatus(gage, currentStatus.waterLevel);
  if (!roadStatus) return null;

  return (
    <span>
      <b>
        <span>{roadStatus.deltaFormatted}</span>
        <span> {roadStatus.preposition} road</span>
      </b>
    </span>
  );
}

function rotateIcons(images, hideWhenDone) {
  for (const img of (images || [])) {
    const newImg = img.cloneNode(true);
    newImg.style.display = "inline";
    newImg.style.animationName = "rotate";
    newImg.style.animationDuration = "1s";
    newImg.style.animationTimingFunction = "linear";
    newImg.style.animationIterationCount = 1;
    if (navigator.userAgent.match(/iPhone|iPad|iPod/i)) {
      //hack to overcome some ios issue
      newImg.style.animationIterationCount = 2;
    }
    if (hideWhenDone) {
      newImg.addEventListener("animationend", (e) => { newImg.style.display = "none"; });
    }
    img.parentElement.replaceChild(newImg, img);
  }
}

function startRotatingLoadingIcons() {
  if (!window.currentLoadingState) {
    window.currentLoadingState = {
      requestId: 1,
    };
  }
  window.currentLoadingState.requestId++;
  window.currentLoadingState.isLoading = true;
  const loop = (reqId) => {
    if ((window.currentLoadingState.requestId === reqId) && window.currentLoadingState.isLoading) {
      rotateIcons(document.getElementsByClassName("btn-refresh"), false);
      rotateIcons(document.getElementsByClassName("btn-refresh-hide"), true);
      setTimeout(function() { loop(reqId) }, 1000);
    }
  }
  loop(window.currentLoadingState.requestId);
}

function stopRotatingLoadingIcons() {
  window.currentLoadingState.isLoading = false;
}

