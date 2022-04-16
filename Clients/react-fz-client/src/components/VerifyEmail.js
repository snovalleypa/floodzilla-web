import React, { useContext, useEffect, useState } from "react";
import Card from "react-bootstrap/Card";
import { Link, useHistory, useLocation } from "react-router-dom";
import { SessionContext } from "./SessionContext";
import Loading from "./Loading";
import RequireLogin from "./RequireLogin";
import VerifyEmailModal from "./VerifyEmailModal";

export default function VerifyEmail() {

  const session = useContext(SessionContext);
  const [ showVerifyEmail, setShowVerifyEmail] = useState(false);
  const [ isLoading, setIsLoading ] = useState(true);
  const [ error, setError ] = useState(null);

  const handleVerifyEmailShow = (e) => {e.preventDefault(); setShowVerifyEmail(true);}
  const onShowVerifyEmailChange = (show) => setShowVerifyEmail(show);

  const history = useHistory();
  const location = useLocation();
  function useQuery() {
    return new URLSearchParams(location.search);
  }
  const query = useQuery();
  const userId = query.get('userId');
  const token = query.get('token');

  // on mount/unmount
  useEffect(() => {
    if (!userId || !token) {
      history.push('/');
    }

    session.verifyEmail(token,
                        () => {
                          setIsLoading(false);
                          setError(null);
                        },
                        (message) => {
                          setIsLoading(false);
                          setError(message);
                        }
                        );
    
  }, []);  // eslint-disable-line react-hooks/exhaustive-deps

  return (
  <div className="profile-page">
    <RequireLogin />
    <VerifyEmailModal show={showVerifyEmail} onShowChange={onShowVerifyEmailChange} />
    {isLoading
    ? <Loading />
    : <Card className="profile-card">
        <Card.Header className="text-center">Verify Email Address</Card.Header>
        <Card.Body>
          {error
            ?<>
               <div>An error has occurred: {error}</div>
               <div><Link to='/' onClick={handleVerifyEmailShow}>Click here</Link> to try again.</div>
             </>
            :<div>
              Your email address has been verified! <br />
              <Link to="/">Continue to Floodzilla</Link>
            </div>
          }
        </Card.Body>
      </Card>
    }
   </div>
  );
}