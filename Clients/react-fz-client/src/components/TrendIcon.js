import React from "react";
import Constants from "../constants";

export default function getTrendIcon({ trend, ...elementProps }) {
  let fileName;
  if (trend === "Rising") {
    fileName = "round_green-trending_up-24px.png";
  } else if (trend === "Falling") {
    fileName = "round_green-trending_down-24px.png";
  } else if (trend === "Cresting" || trend === "Steady") {
    fileName = "round_green-arrow_right_alt-24px.png";
  }
  return (
    (trend === "Offline")
    ? <></>
    : <img
        src={`${Constants.RESOURCE_BASE_URL}/Images/DashboardIcons/` + fileName}
        alt={trend}
        {...elementProps}
      />
  );
}
