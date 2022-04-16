import React from 'react';
import Main from "./components/Main";
import 'bootstrap/dist/css/bootstrap.min.css';
import { BrowserRouter as Router } from "react-router-dom";

import { DebugContextProvider } from "./components/DebugContext";
import { SessionContextProvider } from "./components/SessionContext";
import { GageDataContextProvider } from "./components/GageDataContext";

export default function App() {

  // Suppress compilation warnings because they're currently too noisy.
  //$ TODO: fix compilation warnings and re-enable this
  console.warn = () => {};

  return (
    <div>
      <Router>
        <DebugContextProvider>
          <SessionContextProvider>
            <GageDataContextProvider>
              <Main />
            </GageDataContextProvider>
          </SessionContextProvider>
        </DebugContextProvider>
      </Router>
    </div>
    );
}
