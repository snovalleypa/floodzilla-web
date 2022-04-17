import React, { useContext, useEffect } from "react";
import { SessionContext, SessionState } from "./SessionContext";
import { useHistory } from "react-router-dom";

export default function RequireLogin(props) {
  const session = useContext(SessionContext);
  const history = useHistory();

  useEffect(() => {
    if (session.sessionState !== SessionState.LOGGED_IN) {
      history.push('/');
    }
  }, [history, session]);

  return (<></>);
}
