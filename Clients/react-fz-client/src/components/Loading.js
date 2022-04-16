import React from "react";
import Constants from "../constants";

export default function Loading({ style }) {
  return (
    <div className="loading" style={{ ...style }}>
      <img
        src={`${Constants.RESOURCE_BASE_URL}/images/DashboardIcons/baseline-refresh-24px.png`}
        alt="Loading..."
      />
    </div>
  );
}
