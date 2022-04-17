import React from "react";

export default function GageStatus({ gage, currentStatus }) {
  return (
    <span className={"water-status-box " + currentStatus.boxStyle}>
      {currentStatus.floodStatus || ""}
    </span>
  );
}
