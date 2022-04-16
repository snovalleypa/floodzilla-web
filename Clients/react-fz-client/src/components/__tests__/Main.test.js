import React from "react";
import ReactDOM from "react-dom";
import { MemoryRouter } from "react-router";

import Main from "../Main";
import { usgsGage, svpaGage, gageList } from "../../test/mockData";

beforeEach(() => {
  fetch.resetMocks();
  fetch.once("[]").once("[]");
});

it("renders without crashing", () => {
  const div = document.createElement("div");
  ReactDOM.render(
    <MemoryRouter>
      <Main />
    </MemoryRouter>,
    div
  );
});
