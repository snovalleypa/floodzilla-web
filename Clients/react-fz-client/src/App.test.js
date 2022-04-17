import React from "react";
import ReactDOM from "react-dom";
import App from "./App";
import { usgsGage, svpaGage, gageList } from "./test/mockData";

fetch.once("[]").once("[]");

it("renders without crashing", () => {
  const div = document.createElement("div");
  ReactDOM.render(<App />, div);
});
