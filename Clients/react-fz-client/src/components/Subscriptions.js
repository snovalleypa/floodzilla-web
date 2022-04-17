import React, { useContext, useEffect, useState } from "react";
import Card from "react-bootstrap/Card";
import { Link, useLocation } from "react-router-dom";
import { SessionContext } from "./SessionContext";
import { GageDataContext, GageDataResult } from "./GageDataContext";
import "../style/Profile.css";
import "../style/Login.css";
import Loading from "./Loading";
import RequireLogin from "./RequireLogin";
import VerifyEmailModal from "./VerifyEmailModal";
import SubscriptionManager from "../lib/SubscriptionManager";
import Footer from "./Footer";

export default function Subscriptions() {

  const session = useContext(SessionContext);
  const gageData = useContext(GageDataContext);
  const location = useLocation();
  function useQuery() {
    return new URLSearchParams(location.search);
  }
  const query = useQuery();
  const addGageId = query.get('add');

  const [ showVerifyEmail, setShowVerifyEmail ] = useState(false);
  const [ subscriptionManager, setSubscriptionManager ] = useState(null);
  const [ isLoading, setIsLoading ] = useState(true);
  const [ gageList, setGageList ] = useState(null);
  const [ loadingGage, setLoadingGage ] = useState(null);

  const [ subscribedGages, setSubscribedGages ] = useState([]);
  const [ useEmail, setUseEmail ] = useState(false);
  const [ useSms, setUseSms ] = useState(false);
  const [ notifyDailyForecasts, setNotifyDailyForecasts ] = useState(false);
  const [ notifyForecastAlerts, setNotifyForecastAlerts ] = useState(false);

  const handleVerifyEmailShow = (e) => {e.preventDefault(); setShowVerifyEmail(true);}
  const onShowVerifyEmailChange = (show) => setShowVerifyEmail(show);

  const handlePhoneShow = (e) => {
    e.preventDefault();
    session.showPhoneModal();
  }

  const handleUseEmailChange = (event) => {
    setUseEmail(event.target.checked);
    updateSettings(event.target.checked, useSms, notifyForecastAlerts, notifyDailyForecasts);
  }

  const handleUseSmsChange = (event) => {
    setUseSms(event.target.checked);
    updateSettings(useEmail, event.target.checked, notifyForecastAlerts, notifyDailyForecasts);
  }

  const handleNotifyForecastAlertsChange = (event) => {
    setNotifyForecastAlerts(event.target.checked);
    updateSettings(useEmail, useSms, event.target.checked, notifyDailyForecasts);
  }

  const handleNotifyDailyForecastsChange = (event) => {
    setNotifyDailyForecasts(event.target.checked);
    updateSettings(useEmail, useSms, notifyForecastAlerts, event.target.checked);
  }

  const updateSettings = (useEmail, useSms, notifyForecastAlerts, notifyDailyForecasts) => {
    const newSettings = {
      notifyViaEmail: useEmail,
      notifyViaSms: useSms,
      notifyForecastAlerts: notifyForecastAlerts,
      notifyDailyForecasts: notifyDailyForecasts,
    };
    subscriptionManager.updateSettings(newSettings);
  }

  // on mount/unmount
  useEffect(() => {
    async function onMount() {
      const subManager = new SubscriptionManager(session, window.regionSettings.id);
      setSubscriptionManager(subManager);

      const s = await subManager.getSettings();
      setUseEmail(s.notifyViaEmail);
      setUseSms(s.notifyViaSms);
      setNotifyForecastAlerts(s.notifyForecastAlerts);
      setNotifyDailyForecasts(s.notifyDailyForecasts);

      setSubscribedGages((await subManager.getSubscribedGages()).slice(0));
      setIsLoading(false);

      if (addGageId) {
        if (!await subManager.isSubscribed(addGageId)) {
          setGageSubscription(subManager, addGageId, true);
        }
      }
    }
    onMount();
  }, [addGageId]);  // eslint-disable-line react-hooks/exhaustive-deps
  // don't add 'session' to previous dependency list

  useEffect(() => {
    const result = gageData.gageListResult;
    switch (result.result) {
      case GageDataResult.OK:
        setGageList(result.value);
        break;
      case GageDataResult.PENDING:
        // 
        break;
      default:
      case GageDataResult.ERROR:
        //$ TODO: How do we handle this error?
        break;
    }
  }, [gageData.gageListResult]);

  const isSubscribed = (gageid) => {
    return (subscribedGages.includes(gageid));
  }

  const setGageSubscription = async (subManager, gageid, enabled) => {
    setLoadingGage(gageid);
    await subManager.setGageSubscription(gageid, enabled);
    setSubscribedGages((await subManager.getSubscribedGages()).slice(0));
    setLoadingGage(null);
  }

  const onSubscribedChanged = async (event) => {
    await setGageSubscription(subscriptionManager, event.target.value, event.target.checked);
  }

  return (
  <div className="profile-page">
    <RequireLogin />
    <VerifyEmailModal show={showVerifyEmail} onShowChange={onShowVerifyEmailChange} />
    {isLoading
    ? <Loading />
    : <>
        <Card className="profile-card">
          <Card.Header className="text-center">Floodzilla Alerts Beta</Card.Header>
          <Card.Body>
            <div className="container">
              <div className="profile-welcome">
                 <p>Welcome to the Floodzilla Alerts Beta!  We will send you alerts via
                 email or SMS Text message when we detect flood conditions.</p>
                 <p>We need your feedback. <a href="mailto:info@svpa.us?Subject=Alerts+Feedback">Let us know</a> how we're doing.</p>
              </div>
            </div>
          </Card.Body>
        </Card>
        <Card className="profile-card">
          <Card.Header className="text-center">Alert Settings</Card.Header>
          <Card.Body>
            <div className="container">
              <div className="form-group">
                <div className="row profile-checkrow">
                  <div className="col justify-content-center">
                    {(!session.getEmailVerified()) &&
                    <div>
                      <Link to="/" onClick={handleVerifyEmailShow}>Verify your email address</Link> to receive email alerts
                    </div>
                    }
                  </div>
                </div>
                <div className="row profile-checkrow">
                  <div className="col justify-content-center">
                    <input type="checkbox" id="useEmail" className="login-form-check" checked={useEmail} onChange={handleUseEmailChange} disabled={!session.getEmailVerified()} />
                    <label htmlFor="useEmail" className="profile-sublabel"><span className={session.getEmailVerified() ? "login-label-inline" : "login-label-disabled-inline"}>  Send alerts via email to: {session.getUsername()}</span></label>
                  </div>
                </div>

                <div className="row profile-checkrow">
                  <div className="col justify-content-center">
                    {(!session.getPhoneVerified()) &&
                    <div>
                      <Link to="/" onClick={handlePhoneShow}>Enter a phone number</Link> to receive Sms alerts
                    </div>
                    }
                  </div>
                </div>
                <div className="row profile-checkrow">
                  <div className="col justify-content-center">
                    <input type="checkbox" id="useSms" className="login-form-check" checked={useSms} onChange={handleUseSmsChange} disabled={!session.getPhoneVerified()} />
                    <label htmlFor="useSms" className="profile-sublabel"><span className={session.getPhoneVerified() ? "login-label-inline" : "login-label-disabled-inline"}>  Send alerts via SMS to: {session.getPhone()}&nbsp;&nbsp;<Link to="/" onClick={handlePhoneShow}>(Change)</Link></span></label>
                  </div>
                </div>
              </div>
            </div>
          </Card.Body>
        </Card>
        <Card className="profile-card">
          <Card.Header className="text-center">Forecasts</Card.Header>
          <Card.Body>
            <div className="container">
              <div className="profile-welcome">
                 <p>Floodzilla can send you river forecasts.</p>
              </div>
            </div>
            <div className="container">
              <div className="form-group">
                <div className="row profile-checkrow">
                  <div className="col justify-content-center">
                    <input type="checkbox" id="notifyForecastAlerts" className="login-form-check" checked={notifyForecastAlerts} onChange={handleNotifyForecastAlertsChange} disabled={!session.getEmailVerified()} />
                    <label htmlFor="notifyForecastAlerts" className="profile-sublabel">
                      <span className={session.getEmailVerified() ? "login-label-inline" : "login-label-disabled-inline"}>
                            Send me flood forecast alerts (typically once or twice a day during flood events).
                      </span></label>
                  </div>
                </div>
                <div className="row profile-checkrow">
                  <div className="col justify-content-center">
                    <input type="checkbox" id="notifyDailyForecasts" className="login-form-check" checked={notifyDailyForecasts} onChange={handleNotifyDailyForecastsChange} disabled={!session.getEmailVerified()} />
                    <label htmlFor="notifyDailyForecasts" className="profile-sublabel">
                      <span className={session.getEmailVerified() ? "login-label-inline" : "login-label-disabled-inline"}>
                            Send me daily river status and crest forecasts.
                      </span></label>
                  </div>
                </div>
              </div>
            </div>
          </Card.Body>
        </Card>
        <Card className="profile-card">
          <Card.Header className="text-center">Gage Alerts</Card.Header>
          <Card.Body>
            <div className="container">
              <div className="form-group">
                <div className="profile-label">Alert me about status changes for these gages:</div>
                {gageList &&
                  <ul className="profile-sublist">
                    {gageList.map(gage => (
                    <GageSubscription
                       key={gage.id}
                       gage={gage}
                       isLoading={loadingGage === gage.id}
                       isSubscribed={isSubscribed(gage.id)}
                       onSubscribedChanged={onSubscribedChanged}
                    />
                    ))}
                  </ul>
                }
              </div>
            </div>
          </Card.Body>
        </Card>
      </>
    }
    <Footer />
  </div>
  );
}

function GageSubscription({gage, isSubscribed, onSubscribedChanged, isLoading}) {
  return (
    <li className="profile-gagesub" key={gage.id}><div className="profile-gagesub">
      <div className={isLoading ? "profile-gagesubloading" : "profile-gagesubloadinghidden"}></div>
      <input type="checkbox" id={'ck-' + gage.id} value={gage.id} className="login-form-check" checked={isSubscribed} onChange={onSubscribedChanged} />
      <label className="profile-sublabel" htmlFor={'ck-' +gage.id}>{'  ' + gage.id + ' ' + gage.locationName}</label>
    </div></li>
  );
}