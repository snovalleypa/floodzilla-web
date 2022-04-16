import React, { useContext, useEffect, useState } from "react";
import Constants from "../constants";
import { DebugContext } from "./DebugContext";
import LoginCreateModal from "./LoginCreateModal";
import LoginModal from "./LoginModal";
import CreateAccountModal from "./CreateAccountModal";
import ForgotPasswordModal from "./ForgotPasswordModal";
import PhoneModal from "./PhoneModal";

export const SessionContext = React.createContext(null);

export const SessionState = {
  NOT_LOGGED_IN: 'nli',
  LOGGING_IN   : 'busy',
  LOGGED_IN    : 'auth',
};

export function getGoogleAuthApi(clientId) {
  if (!window.googleAuthApi) {
    window.gapi.load('auth2', function() {
      const auth2 = window.gapi.auth2.init({
        client_id: clientId,
      });
      window.googleAuthApi = auth2;
    });
  }
  return window.googleAuthApi;
}

export function SessionContextProvider(props) {

  const debug = useContext(DebugContext);

  const LOCAL_STORAGE_TOKEN = 'fzAuthToken';
  const LOCAL_STORAGE_LOGINPROVIDER = 'fzLoginProvider';

  const [destPath, setDestPath] = useState(undefined);
  const [showLoginCreate, setShowLoginCreate] = useState(false);
  const [showCreateAccount, setShowCreateAccount] = useState(false);
  const [showForgotPassword, setShowForgotPassword] = useState(false);
  const [showLogin, setShowLogin] = useState(false);
  const [showPhone, setShowPhone] = useState(false);

  const showLoginCreateModal = ({destPath}) => {
    setDestPath(destPath);
    setShowLoginCreate(true);
  }
  const showPhoneModal = () => {
    setShowPhone(true);
  }
  const onShowLoginCreateChange = (show) => setShowLoginCreate(show);
  const onShowCreateAccountChange = (show) => setShowCreateAccount(show);
  const onShowForgotPasswordChange = (show) => setShowForgotPassword(show);
  const onShowLoginChange = (show) => setShowLogin(show);
  const onShowPhoneChange = (show) => setShowPhone(show);
  
  const [ firstName, setFirstName ] = useState('');
  const [ lastName, setLastName ] = useState('');
  const [ username, setUsername ] = useState(null);
  const [ emailVerified, setEmailVerified ] = useState(false);
  const [ phoneVerified, setPhoneVerified ] = useState(false);
  const [ phone, setPhone ] = useState('');
  const [ authToken, setAuthToken ] = useState(localStorage.getItem(LOCAL_STORAGE_TOKEN));
  const [ loginProvider, setLoginProvider ] = useState(localStorage.getItem(LOCAL_STORAGE_LOGINPROVIDER));
  const [ isAdmin, setIsAdmin ] = useState(false);
  const [ hasPassword, setHasPassword ] = useState(false);

  // Assume an initial state of LOGGED_IN if we've got a token; if the token has expired,
  // a call to reauthenticate() will downgrade our state to NOT_LOGGED_IN, but this way
  // we default to being able to use a stored token if we've got one.
  const [ sessionState, setSessionState ] = useState(authToken ? SessionState.LOGGED_IN : SessionState.NOT_LOGGED_IN);

  //$ TODO: Add a way for children to listen to login status changes?

  const onForgotPassword = () => {
    setShowLogin(false);
    setShowCreateAccount(false);
    setShowForgotPassword(true);
  }

  const onShowLogin = () => {
    setShowLoginCreate(false);
    setShowLogin(true);
  }

  const onShowCreate = () => {
    setShowLoginCreate(false);
    setShowCreateAccount(true);
  }

  useEffect(() => {
    getGoogleAuthApi(window.GoogleAuthClientID);
  }, []);
  
  function getUsername() {
    return username;
  }

  function getEmailVerified() {
    return emailVerified;
  }

  function getPhoneVerified() {
    return phoneVerified;
  }

  function getFirstName() {
    return firstName;
  }

  function getLastName() {
    return lastName;
  }

  function getPhone() {
    return phone;
  }

  function getHasPassword() {
    return hasPassword;
  }

  // This is just in case we end up not using readable strings for these...
  function getLoginProviderName() {
    switch (loginProvider) {
      case Constants.authApi.FACEBOOK_LOGIN_PROVIDER_NAME:
        return 'Facebook';
      case Constants.authApi.GOOGLE_LOGIN_PROVIDER_NAME:
        return 'Google';
      default:
        return 'n/a';
    }
  }

  function getIsAdmin() {
    return isAdmin;
  }

  function getAuthenticationHeaders() {
    if (authToken === null || authToken === '') {
      return {};
    }
    return {
      "Authorization": `Bearer ${authToken}`,
    }
  }

  const removeAuthToken = () => {
    localStorage.setItem(LOCAL_STORAGE_TOKEN, '');
    localStorage.setItem(LOCAL_STORAGE_LOGINPROVIDER, '');
    setAuthToken('');
  }

  const onAuthenticate = (result) => {
    setFirstName(result.firstName);
    setLastName(result.lastName);
    setUsername(result.username);
    setPhone(result.phone);
    setAuthToken(result.token);
    setLoginProvider(result.loginProvider);
    setIsAdmin(result.isAdmin);
    setHasPassword(result.hasPassword);
    setEmailVerified(result.emailVerified);
    setPhoneVerified(result.phoneVerified);
    localStorage.setItem(LOCAL_STORAGE_TOKEN, result.token);
    localStorage.setItem(LOCAL_STORAGE_LOGINPROVIDER, result.loginProvider);
    setSessionState(SessionState.LOGGED_IN);
  }

  const onAuthenticateFail = () => {
    removeAuthToken();
    setSessionState(SessionState.NOT_LOGGED_IN);
  }

  const authFetch = async (url, method, body, withAuth, onSuccess, onFail) => {

    var authHeaders = {};
    if (withAuth) {
      authHeaders = getAuthenticationHeaders();
    }
    var json;
    await debug.debugFetch(url, {
      method: method,
      body: body,
      headers: {
        ...authHeaders,
        "Content-Type": "application/json"
      },
    })
    .then(async response => {
      if (!await response.ok) {
        var body = await response.text();
        throw { body: body, statusText: response.statusText, status: response.status }; // eslint-disable-line no-throw-literal
      }
      return response;
    })
    .then(async response => {

      json = await response.json();
      if (Constants.LOG_FETCH_CALLS) {
        console.log('== AUTHFETCH DONE == ' + method + ' ' + url);
      }
      onSuccess && onSuccess(json);

      var token = response.headers.get(Constants.authApi.ID_TOKEN_HEADER);
      if (token) {
        setAuthToken(token);
        localStorage.setItem(LOCAL_STORAGE_TOKEN, token);
      }

      return json;
    })
    .catch(err => {
      if (onFail) {
        onFail(err.status, err.body || err.statusText || 'An error occurred. Please try again later.');
      } else {
        throw { statusText: 'An error occurred. Please try again later.' }; // eslint-disable-line no-throw-literal
      }
    });
    return json;
  }

  // onSuccess will be called with ()
  // onFail will be called with (errorMessage)
  const login = (username, password, rememberMe, captchaToken, onSuccess, onFail) => {

    setSessionState(SessionState.LOGGING_IN);
    
    const url = `${Constants.authApi.AUTHENTICATE_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify({
                username: username,
                password: password,
                rememberMe: rememberMe,
                captchaToken: captchaToken,
              }),
              false,
              (result) => {
                onAuthenticate(result);
                onSuccess();
              },
              (status, message) => {
                onAuthenticateFail();
                onFail(message);
              });
  }

  // onSuccess will be called with ()
  // onFail will be called with (errorMessage)
  const createAccount = (firstName, lastName, username, phone, password, rememberMe, captchaToken, onSuccess, onFail) => {

    setSessionState(SessionState.LOGGING_IN);
    
    const url = `${Constants.authApi.CREATEACCOUNT_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify({
                username: username,
                password: password,
                firstName: firstName,
                lastName: lastName,
                phone: phone,
                rememberMe: rememberMe,
                captchaToken: captchaToken,
              }),
              false,
              (result) => {
                onAuthenticate(result);
                onSuccess();
              },
              (status, message) => {
                if (status === 409) {
                  //$ TODO: Decide whether we want to explicitly surface 'user already exists'
                  onAuthenticateFail();
                  onFail('A user with this email address already exists.');
                } else {
                  onAuthenticateFail();
                  onFail(message);
                }
              }
              );
  }

  // onSuccess will be called with ()
  // onFail will be called with (errorMessage)
  const forgotPassword = (email, captchaToken, onSuccess, onFail) => {
    const url = `${Constants.authApi.FORGOTPASSWORD_URL}`;
    authFetch(url,
              'post',
              JSON.stringify({
                email:email,
                captchaToken: captchaToken,
              }),
              false,
              (result) => {
                onSuccess();
              },
              (status, message) => {
                onFail(message);
              }
              );
  }
  
  // onSuccess will be called with ()
  // onFail will be called with (errorMessage)
  const setPassword = (oldPassword, newPassword, onSuccess, onFail) => {
    const url = `${Constants.authApi.SETPASSWORD_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify({
                oldPassword:oldPassword,
                newPassword:newPassword,
              }),
              true,
              (result) => {
                onSuccess();
              },
              (status, message) => {
                onFail(message);
              });
  }

  // onSuccess will be called with ()
  // onFail will be called with (errorMessage)
  const createPassword = (newPassword, onSuccess, onFail) => {
    const url = `${Constants.authApi.CREATEPASSWORD_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify({
                newPassword:newPassword,
              }),
              true,
              (result) => {
                onAuthenticate(result);
                onSuccess();
              },
              (status, message) => {
                if (status === 409) {
                  onFail('This user already has a password.');
                } else {
                  onFail(message);
                }
              });
  }
  
  // onSuccess will be called with ()
  // onFail will be called with (errorMessage)
  const resetPassword = (userId, code, newPassword, onSuccess, onFail) => {
    const url = `${Constants.authApi.RESETPASSWORD_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify({
                userId:userId,
                code:code,
                newPassword:newPassword,
              }),
              true,
              (result) => {
                onSuccess();
              },
              (status, message) => {
                onFail(message);
              });
  }
  
  // onSuccess will be called with ()
  // onFail will be called with (errorMessage)
  const updateProfile = (firstName, lastName, email, onSuccess, onFail) => {

    const url = `${Constants.authApi.UPDATEACCOUNT_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify({
                firstName: firstName,
                lastName: lastName,
                username: email,
              }),
              true,
              (result) => {
                onAuthenticate(result);
                onSuccess();
              },
              (status, message) => {
                onFail(message);
              }
              );
  }

  // onSuccess will be called with ()
  // onFail will be called with (errorMessage)
  const changeEmail = (email, onSuccess, onFail) => {

    const url = `${Constants.authApi.UPDATEACCOUNT_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify({
                Username: email,
              }),
              true,
              (result) => {
                onAuthenticate(result);
                onSuccess();
              },
              (status, message) => {
                //$ TODO: Decide whether we want to explicitly surface 'user already exists'
                if (status === 409) {
                  onFail('A user with this email address already exists.');
                } else {
                  onFail(message);
                }
              }
              );
  }

  const reauthenticate = () => {
    if (!authToken) {
      return;
    }

    const url = `${Constants.authApi.REAUTHENTICATE_URL}`;
    authFetch(url,
              'GET',
              null,
              true,
              (result) => {
                onAuthenticate(result);
              },
              (status, message) => {
                onAuthenticateFail();
              }
              );
  }

  //$ TODO: What does error handling for this look like?
  const sendVerificationEmail = () => {
    if (!authToken) {
      return;
    }

    const url = `${Constants.authApi.SENDVERIFICATIONEMAIL_URL}`;
    authFetch(url,
              'GET',
              null,
              true,
              () => {},
              (status, message) => {
                throw message;
              }
              );
  }

  const verifyEmail = (token, onSuccess, onFailure) => {
    if (!authToken) {
      return;
    }

    const url = `${Constants.authApi.VERIFYEMAIL_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify(token),
              true,
              () => {
                setEmailVerified(true);
                onSuccess();
              },
              (status, message) => {
                onFailure(message);
              }
              );
  }

  //$ TODO: What does error handling for this look like?
  const sendPhoneVerificationSms = (phone, onSuccess, onFailure) => {
    if (!authToken) {
      return;
    }

    const url = `${Constants.authApi.SENDPHONEVERIFICATION_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify(phone),
              true,
              onSuccess,
              onFailure
              );
  }

  const verifyPhoneCode = (phone, code, onSuccess, onFailure) => {
    if (!authToken) {
      return;
    }

    const request = {
      phone: phone,
      code: code,
    }
    
    const url = `${Constants.authApi.VERIFYPHONE_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify(request),
              true,
              () => {
                setPhone(phone);
                setPhoneVerified(true);
                onSuccess();
              },
              (status, message) => {
                onFailure(status, message);
              }
              );
  }

  const processGoogleToken = (idToken, onSuccess, onFail) => {
    const url = `${Constants.authApi.AUTHENTICATE_WITH_GOOGLE_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify(idToken),
              false,
              (result) => {
                onAuthenticate(result);
                onSuccess();
              },
              (status, message) => {
                onAuthenticateFail();
                onFail(message);
              }
              );
  }

  const processFacebookResponse = (authResponse, onSuccess, onFail) => {
    const url = `${Constants.authApi.AUTHENTICATE_WITH_FACEBOOK_URL}`;
    authFetch(url,
              'POST',
              JSON.stringify({
                userId: authResponse.userID,
                token: authResponse.accessToken,
              }),
              false,
              (result) => {
                onAuthenticate(result);
                onSuccess();
              },
              (status, message) => {
                onAuthenticateFail();
                onFail(message);
              }
              );
  }

  const logOut = () => {
    removeAuthToken();
    switch (loginProvider) {
      default:
        break;
      case Constants.authApi.FACEBOOK_LOGIN_PROVIDER_NAME:
        window.FB.getLoginStatus(function(response) {
          if (response.status === 'connected') {
            window.FB.logout();
          }
        });
        break;
      case Constants.authApi.GOOGLE_LOGIN_PROVIDER_NAME:
        window.googleAuthApi.signOut();
        break;
    }

    setSessionState(SessionState.NOT_LOGGED_IN);
  }

  return (
  <SessionContext.Provider
          value = { {
            login: (username, password, rememberMe, captchaToken, onSuccess, onFail) => { login(username, password, rememberMe, captchaToken, onSuccess, onFail) },
            createAccount: (firstName, lastName, username, phone, password, rememberMe, captchaToken, onSuccess, onFail) => { createAccount(firstName, lastName, username, phone, password, rememberMe, captchaToken, onSuccess, onFail) },
            forgotPassword: (email, captchaToken, onSuccess, onFail) => { forgotPassword(email, captchaToken, onSuccess, onFail) },
            setPassword: (oldPassword, newPassword, onSuccess, onFail) => { setPassword(oldPassword, newPassword, onSuccess, onFail) },
            createPassword: (newPassword, onSuccess, onFail) => { createPassword(newPassword, onSuccess, onFail) },
            resetPassword: (userId, code, newPassword, onSuccess, onFail) => { resetPassword(userId, code, newPassword, onSuccess, onFail) },
            updateProfile: (firstName, lastName, email, onSuccess, onFail) => { updateProfile(firstName, lastName, email, onSuccess, onFail) },
            changeEmail: (email, onSuccess, onFail) => { changeEmail(email, onSuccess, onFail) },
            processGoogleToken: (idToken, onSuccess, onFail) => { processGoogleToken(idToken, onSuccess, onFail); },
            processFacebookResponse: (authResponse, onSuccess, onFail) => { processFacebookResponse(authResponse, onSuccess, onFail); },
            authFetch: (url, method, body, withAuth, onSuccess, onFail) => { return authFetch(url, method, body, withAuth, onSuccess, onFail); },
            logOut: () => { logOut() },
            reauthenticate: () => { reauthenticate() },
            sendVerificationEmail: sendVerificationEmail,
            verifyEmail: verifyEmail,
            sendPhoneVerificationSms: sendPhoneVerificationSms,
            verifyPhoneCode: verifyPhoneCode,
            sessionState: sessionState,
            getFirstName: () => { return getFirstName(); },
            getLastName: () => { return getLastName(); },
            getPhone: () => { return getPhone(); },
            getUsername: () => { return getUsername(); },
            getEmailVerified: () => { return getEmailVerified(); },
            getPhoneVerified: () => { return getPhoneVerified(); },
            getHasPassword: () => { return getHasPassword(); },
            getLoginProviderName: () => { return getLoginProviderName(); },
            getIsAdmin: () => { return getIsAdmin(); },
            getAuthenticationHeaders: () => { return getAuthenticationHeaders(); },
            showLoginCreateModal: showLoginCreateModal,
            showPhoneModal: showPhoneModal,
          } }
  >
    {props.children}
    <LoginCreateModal show={showLoginCreate} onShowChange={onShowLoginCreateChange} onShowLogin={onShowLogin} onShowCreate={onShowCreate} destPath={destPath}/>
    <LoginModal show={showLogin} onShowChange={onShowLoginChange} onForgotPassword={onForgotPassword} destPath={destPath} />
    <CreateAccountModal show={showCreateAccount} onShowChange={onShowCreateAccountChange} destPath={destPath} />
    <ForgotPasswordModal show={showForgotPassword} onShowChange={onShowForgotPasswordChange} />
    <PhoneModal show={showPhone} onShowChange={onShowPhoneChange} />
  </SessionContext.Provider>
  );
};
