import React, { useContext, useEffect, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import "../style/Forecast.css";
import ForecastChartOptionsBuilder from "../lib/forecastChartOptionsBuilder_highcharts";
import { GageDataContext, GageDataResult } from "./GageDataContext";
import Footer from "./Footer";
import Loading from "./Loading";
import * as Highcharts from "highcharts/highstock";
import HighchartsReact from "highcharts-react-official";
import moment from "moment-timezone";
import { DebugContext } from "./DebugContext";
import ChartRange from "../lib/chartRange";
import Constants from "../constants";
import Card from "react-bootstrap/Card";
import * as utils from "../lib/utils";

const ForecastRanges = {
  'DF': {
    label: 'Full',
    before: 3,
    after: 10,
  },
  'D8': {
    label: '8 days',
    before: 4,
    after: 4,
  },
  'D6': {
    label: '6 days',
    before: 3,
    after: 3,
  },
  'D4': {
    label: '4 days',
    before: 2,
    after: 2,
  },
  'D2': {
    label: '2 days',
    before: 1,
    after: 1,
  },
};
const ForecastRangeOptions = ['DF', 'D8', 'D6', 'D4', 'D2'];

const ChartColorsHex = ['#0000FF', '#008000', '#800000', '#800080', '#FF4500', '#00FF00'];

export default function Forecast() {

  const debug = useContext(DebugContext);
  const gageData = useContext(GageDataContext);
  const location = useLocation();
  function useQuery() {
    return new URLSearchParams(location.search);
  }
  const query = useQuery();
  const queryGages = query.get('gageIds');
  const queryGagesChanged = utils.useCompare(queryGages)

  const [options, setOptions] = useState(null);
  const [forecast, setForecast] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [forecastRange, setForecastRange] = useState('DF');
  const [gageIds, setGageIds] = useState([]);
  const [forecastGages, setForecastGages] = useState([]);
  const [showDetailedForecast, setShowDetailedForecast] = useState(false);

  const FORECAST_REFRESH_INTERVAL = 60 * 60 * 1000;   // milliseconds
  const FORECAST_RECENT_DATA_DAYS = 4;

  useEffect(() => {
    if (queryGagesChanged) {
      if (queryGages) {
        setGageIds(queryGages.split(','));
      } else {
        setGageIds(Constants.defaultForecastGageIds);
      }
    }
  }, [gageIds, query, queryGagesChanged]);

  useEffect(() => {
    setShowDetailedForecast(gageIds.length === 1);
  }, [gageIds])

  const buildForecastGage = (metagages, gageList, id, color) => {
    const meta = metagages.find(m => m.id === id);
    const gage = gageList.find(g => g.id === id);
    if (meta) {
      return {
        id: id,
        nwrfcId: meta.siteId,
        title: meta.name,
        warningDischarge: meta.stageOne,
        floodDischarge: meta.stageTwo,
        color: color,
        isMetagage: true,
      };
    }
    if (gage) {
      return {
        id: id,
        nwrfcId: gage.noaaSiteId,
        title: gage.shortName,
        warningDischarge: gage.dischargeStageOne,
        floodDischarge: gage.dischargeStageTwo,
        color: color,
        isMetagage: false,
      };
    }
    return undefined;
  }

  useEffect(() => {
    const metagages = gageData.metagagesResult;
    const gageList = gageData.gageListResult;
    let i = 0
    if (metagages && metagages.result === GageDataResult.OK && gageList && gageList.result === GageDataResult.OK) {
      const gages = gageIds.map(id => buildForecastGage(metagages.value, gageList.value, id, ChartColorsHex[i++]))
      setForecastGages(gages.filter(g => g !== undefined))
    }
  }, [gageIds, gageData.gageListResult, gageData.metagagesResult]);

  useEffect(() => {

    if (gageIds.length > 0) {
      ChartRange.setDebug(debug);

      let requestData = {
      };

      setForecast(null);
      gageData.registerForecastListener(requestData,
                                        gageIds,
                                        moment.duration(FORECAST_RECENT_DATA_DAYS, 'days'),
                                        FORECAST_REFRESH_INTERVAL,
                                        onLoading,
                                        onForecastSuccess,
                                        onForecastFailure);
    }
  }, [gageIds]);  // eslint-disable-line react-hooks/exhaustive-deps
  
  const onLoading = (requestData, isLoading) => {
    setIsLoading(isLoading);
  }

  const onForecastSuccess = (requestData, response) => {
    setForecast(response);
  }

  const onForecastFailure = (requetsData, errorMessage) => {
  }

  const changeForecastRange = (opt) => {
    setForecastRange(opt);
    const range = ForecastRanges[opt];
    setOptions(
      new ForecastChartOptionsBuilder(forecastGages, forecast, window.regionSettings.timezone, range.before, range.after).getOptions()
    );
  }

  useEffect(() => {
    const range = ForecastRanges[forecastRange];
    setOptions(
      new ForecastChartOptionsBuilder(forecastGages, forecast, window.regionSettings.timezone, range.before, range.after).getOptions()
    );
  }, [forecastGages, forecast, forecastRange]);

  return (
    <div>
      <div className="container" id="forecastmainArea">
        {isLoading
        ?<Loading />
        :<>
          <div className="forecast-box row justify-content-center">
            <div className="col justify-content-center forecast-chart-content">
              <div className="row forecast-filter filter-criteria justify-content-center">
                <div className="date-range date-range-days">
                  <ul className="nav navbar-nav chart-menu">
                    {ForecastRangeOptions.map(opt => (
                      <li
                        key={opt}
                        onClick={() => changeForecastRange(opt)}
                        className={(opt === forecastRange) ? "active" : ""}
                        style={{ cursor: "pointer", display: 'block', float:'left'}}
                      >
                        <a id={opt}>
                          {ForecastRanges[opt].label}
                        </a>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
              <div className="forecast-chart gage-shadow">
                <HighchartsReact highcharts={Highcharts} options={options} />
              </div>
            </div>
          </div>
          <div className="forecast-details-ctr col justify-content-center">
            <div className="forecast-details-box row">
              {forecast && forecastGages.map(g => 
                <GageForecastCallout gageData={gageData} key={g.id} gageInfo={g} forecast={forecast} showDetailedForecast={showDetailedForecast} />
              )}
            </div>
          </div>
          {showDetailedForecast &&
            <GageForecastDetails gageData={gageData} gageInfo={forecastGages[0]} forecast={forecast} />
          }
        </>}
        <hr />
          <div className="col justify-content-center">
            <div className="row justify-content-center">
              <div className="forecast-footer">
                Flood gage data supplied by the <br /> <a href="http://www.nwrfc.noaa.gov">National Weather Service Northwest River Forecast Center</a>.
              </div>
            </div>
          </div>
        <hr />
        <Footer />
      </div>
    </div>
  );
}

function GageForecastReadingRow({ isWarning, reading, delta }) {
  if (!reading) return null;
  let colspan = "1";
  let tdWidth="110";
  if (!reading.reading) {
    colspan = "2";
    tdWidth="220";
  }
  return (
  <tr className={isWarning ? "reading-warning" : "reading"}>
    <td width="180">{reading.timestamp.format('ddd M/D hh:mm a')}</td>
    {reading.reading && <td width="110">{utils.formatHeight(reading.reading)}</td>}
    <td colSpan={colspan} width={tdWidth}>{utils.formatFlow(reading.waterDischarge)} {(delta !== 0 && delta) && "(" + utils.formatFlowTrend(delta) + ")"}</td>
  </tr>
  );
}

function GageForecastCallout({ gageData, gageInfo, forecast, showDetailedForecast }) {

  const getNoaaLink = (gageInfo) => {
     return "http://www.nwrfc.noaa.gov/river/station/flowplot/flowplot.cgi?" + gageInfo.nwrfcId
  }

  const gageForecast = forecast.forecasts[gageInfo.id];
  const latestReading = forecast.getLatestReading(gageForecast);
  const recentMax = forecast.getRecentMax(gageForecast);
  const peaks = gageForecast.noaaForecast.peaks || [];
  if (!gageData || !gageData.gageListResult || !gageData.gageListResult.value) {
    return null;
  }

  // default to 10000 for sum-of-forks metagage
  const threshold = gageInfo.warningDischarge;

  return (
  <div className="col-lg-4 col-md-6 col-sm-12 col-xs-12">
    <Card className="forecast-details-card gage-shadow">
      <table cellSpacing="0" width="100%" cellPadding="0" className="forecast-details">
        <thead>
          <tr className="title"><td colSpan="3">{gageInfo.title} Summary
            {showDetailedForecast && !gageInfo.isMetagage &&
              <span className="detailsLink">&nbsp;&nbsp;&nbsp;- <Link to={"/gage/" + gageInfo.id}>View Gage</Link>&nbsp;&nbsp;&nbsp;- <a target="_blank" href={getNoaaLink(gageInfo)}>NOAA gage {gageInfo.nwrfcId}</a></span>
            }
            {!showDetailedForecast &&
              <span className="detailsLink">&nbsp;&nbsp;&nbsp;- <Link to={"/forecast?gageIds=" + gageInfo.id}>Forecast Details</Link></span>
            }
          </td></tr>
        </thead>
        <tbody>
          {latestReading ?
          <>
            <tr className="subtitle"><td colSpan="3">Latest reading:</td></tr>
            <GageForecastReadingRow reading={latestReading} delta={gageForecast.predictedCfsPerHour}/>
          </>:null}
          {recentMax ?
          <>
            <tr className="subtitle"><td colSpan="3">Past 24hr max:</td></tr>
            <GageForecastReadingRow reading={recentMax} />
          </>:null}
          <tr className="subtitle"><td colSpan="3">Forecasted crests: <span className="pubdate">(published {gageForecast.noaaForecast.created.format('ddd M/D hh:mm a')})</span></td></tr>
          {peaks ?
          <>
            {peaks.map(peak => (
               <GageForecastReadingRow key={peak.timestamp.valueOf()} reading={peak} isWarning={peak.discharge > threshold} />
            ))}
          </>:null}
        </tbody>
      </table>
    </Card>
  </div>
  );
}

function GageForecastDetails({ gageData, gageInfo, forecast, showDetailedForecast }) {
  if (!gageInfo || !forecast) {
    return <></>
  }
  const gageForecast = forecast.forecasts[gageInfo.id];
  if (!gageForecast) {
    return <></>
  }
console.log(gageForecast)
  return (
    <div className="forecast-details-ctr col justify-content-center">
      <div className="forecast-details-box row">
        <div className="col-12">
          <Card className="forecast-readings-card gage-shadow">
            <div className="forecast-readings-header">{gageInfo.title} Details</div>
            <div className="row">
              <div className="col-sm-12 col-md-6">
                <table cellSpacing="0" width="100%" cellPadding="0" className="forecast-details">
                  <tbody>
                    <tr className="subtitle"><td colSpan="3">Last 100 readings:</td></tr>
                    {gageForecast.dataPoints.slice(0, 100).map(r =>
                      <GageForecastReadingRow key={r.timestamp.valueOf()} reading={r} isWarning={r.waterDischarge > gageInfo.warningDischarge} />
                    )}
                  </tbody>
                </table>
              </div>
              <div className="col-sm-12 col-md-6">
                <table cellSpacing="0" width="100%" cellPadding="0" className="forecast-details">
                  <tbody>
                    <tr className="subtitle"><td colSpan="3">Currently Forecasted Readings:</td></tr>
                    {gageForecast.forecastDataPoints.slice(0, 100).map(r =>
                      <GageForecastReadingRow key={r.timestamp.valueOf()} reading={r} isWarning={r.waterDischarge > gageInfo.warningDischarge} />
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </Card>
        </div>
      </div>
    </div>
  )
}

