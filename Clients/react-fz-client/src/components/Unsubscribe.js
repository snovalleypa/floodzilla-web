import React, { useContext, useEffect, useState } from "react";
import Card from "react-bootstrap/Card";
import Constants from "../constants";
import Button from "react-bootstrap/Button";
import { Link, useHistory, useLocation } from "react-router-dom";
import { SessionContext } from "./SessionContext";
import "../style/Profile.css";
import "../style/Login.css";

export default function Unsubscribe() {

  const session = useContext(SessionContext);
  const location = useLocation();
  const history = useHistory();
  function useQuery() {
    return new URLSearchParams(location.search);
  }
  const query = useQuery();
  const userId = query.get('user');
  const email = query.get('email');

  const [ done, setDone ] = useState(false);
  const [ error, setError ] = useState(false);

  // on mount/unmount
  useEffect(() => {
    async function onMount() {
      if (!userId || !email) {
        history.push('/');
      }
    }
    onMount();
  }, [history, userId, email]);

  // note: this page has to work without login, so authFetch must pass false for withAuth
  const handleUnsubscribe = () => {
    session.authFetch(Constants.subscriptionApi.UNSUBEMAIL_URL,
                      'POST',
                      JSON.stringify({
                        userId: userId,
                      }),
                      false,
                      (result) => { setDone(true) },
                      (status, message) => { setError(true) }
                      );
  }

  return (
  <div className="profile-page">
    <Card className="profile-card">
      <Card.Header className="text-center">Unsubscribe</Card.Header>
      <Card.Body>
        <div className="row justify-content-center">
          <div className="col justify-content-center">
            {!error && !done &&
            <>
              <div className="row profile-text justify-content-center">
                If you unsubscribe, you will no longer receive Floodzilla Alerts at {email}.
              </div>
              <div className="row profile-text justify-content-center">
                <Button onClick={handleUnsubscribe}>Unsubscribe All</Button>
              </div>
            </>
            }
            {!error && done &&
            <>
              <div className="profile-text">
                Your preferences have been updated.
              </div>
              <div className="profile-text">
                <Link to="/user/alerts">Manage your alerts</Link>
              </div>
              <div className="profile-text">
                <Link to="/">Continue to Floodzilla</Link>
              </div>
            </>
            }
            {error &&
            <>
              <div className="row profile-text">
                We're sorry, but an error has occurred while processing your unsubscribe request.
              </div>
              <div className="row profile-text">
                Please&nbsp;<a href="mailto:floodzilla.support@svpa.us">contact us</a>&nbsp;so we can remove your subscription.
              </div>
            </>
            }
          </div>
        </div>
      </Card.Body>
    </Card>
  </div>
  );
}
