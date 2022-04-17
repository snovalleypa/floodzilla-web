import React from "react";
import GageStatus from "./GageStatus";
import TrendIcon from "./TrendIcon";
import GageChart from "./GageChart";
import * as utils from "../lib/utils";

export default function DashboardGage({ gage, gageStatus, viewDetails, resize, isMobile }) {

  return (
    <div
      onClick={() => {
        viewDetails();
      }}
      style={{ cursor: "pointer" }}
      id={"gage_" + gage.id}
    >
      <div className="row row-hover gage-row">
        <div className="col-lg-8 col-md-12 col-sm-12 col-xs-12 box">
          <div className="Title">
            <span className="types types-text" style={{ float: "right" }}>
              {gage.id}
            </span>
            {gage.locationName}
          </div>
          <br />
          <div
            className="col-lg-12 col-md-12 col-sm-12 col-xs-12"
            style={{ padding: 0 }}
          >
            <div
              className="col-lg-5 col-md-6 col-sm-5 col-xs-5 float-left"
              style={{ marginLeft: "-16px", width: "auto"}}
            >
              {!gageStatus
                ? <span className="gage-status-loading">Loading...</span>
                : <>
                    <GageStatus gage={gage} currentStatus={gageStatus.currentStatus}/>
                    <TrendIcon
                       trend={gageStatus.currentStatus.waterStatus}
                       style={{ paddingLeft: "15px" }}
                    />
                  </>
               }
            </div>

            <div style={{ display: "grid" }}>
              {!(gageStatus && gageStatus.currentStatus && gageStatus.currentStatus.waterLevel)
              ? <></>
              : <span>
                <span style={{ fontWeight: "bold" }}>
                  {utils.formatHeight(gageStatus.currentStatus.waterLevel)}
                  {gageStatus.currentStatus.waterDischarge !== null && gageStatus.currentStatus.waterDischarge > 0 && (
                    <span>
                      {" / "}
                      {utils.formatFlow(gageStatus.currentStatus.waterDischarge)}{" "}
                    </span>
                  )}
                </span>
                {" @ "}
                {utils.formatLastReadingTime(gage, gageStatus.currentStatus)}
              </span>
              }
            </div>
          </div>
        </div>
        <div className="col-lg-4 col-md-12 col-sm-12 col-xs-12 chart-box">
          <GageChart
            gage={gage}
            gageStatus={gageStatus}
            resize={resize}
            optionType="dashboardOptions"
          />
        </div>
      </div>
    </div>
  );
}
