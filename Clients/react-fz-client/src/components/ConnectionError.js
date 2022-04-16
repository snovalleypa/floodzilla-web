import React, { useContext, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { GageDataContext } from "./GageDataContext";

//$ move this to .. uh .. gage context? session context?
export const ConnectionState = {
  OK:     'ok',
  ERROR:  'error',
  TRYING: 'trying',
};
    
export default function ConnectionError() {

  const [ connState, setConnState ] = useState(ConnectionState.OK);
  const gageData = useContext(GageDataContext);

  const connError = gageData.getConnError();
  useEffect(() => {
    if (connError) {
      setConnState(ConnectionState.ERROR);
    } else {
      setConnState(ConnectionState.OK);
    }
  }, [gageData, connError]);

  const onRetry = (e) => {
    e.preventDefault();
    setConnState(ConnectionState.TRYING);
    gageData.forceReloadGageInfo();
  }

  const renderNotice = () => {
    switch (connState) {
      default:
      case ConnectionState.OK:
        return <></>;
      case ConnectionState.ERROR:
        return (
          <div className="connNotice">
            <span>Connection Error</span>&nbsp;&nbsp;&nbsp;<Link to="/" onClick={onRetry}>retry</Link>
          </div>
        );
      case ConnectionState.TRYING:
        return (
          <div className="connNotice">
            <span>Connection Error. Retrying...</span>
          </div>
        );
    }
  }

  return renderNotice();
}
