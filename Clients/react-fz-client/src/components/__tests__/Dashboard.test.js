import React from "react";
import ReactDOM from "react-dom";
import { MemoryRouter } from "react-router";
import Gage from "../../models/gage";
import Dashboard from "../Dashboard";
import { svpaGage, usgsGage } from "../../test/mockData";

const reloadGageList = jest.fn();
let gageList;
beforeEach(() => {
  gageList = [new Gage(svpaGage), new Gage(usgsGage)];
});

it("renders without crashing", () => {
  const div = document.createElement("div");
  ReactDOM.render(
    <MemoryRouter>
      <Dashboard reloadGageList={reloadGageList} />
    </MemoryRouter>,
    div
  );
});

it("renders empty gageList", () => {
  const div = document.createElement("div");
  ReactDOM.render(
    <MemoryRouter>
      <Dashboard gageList={[]} reloadGageList={reloadGageList} />
    </MemoryRouter>,
    div
  );
});

it("renders mock gageList", () => {
  const div = document.createElement("div");
  ReactDOM.render(
    <MemoryRouter>
      <Dashboard gageList={gageList} reloadGageList={reloadGageList} />
    </MemoryRouter>,
    div
  );
});
