import React, { useContext, useEffect, useState } from "react";
import { useHistory } from "react-router-dom";
import Button from "react-bootstrap/Button";
import Form from "react-bootstrap/Form";
import Image from "react-bootstrap/Image";
import Modal from "react-bootstrap/Modal";
import { SessionContext } from "./SessionContext";
import "../style/Login.css";

export default function LoginCreateModal(props) {

  const [ isLoading, setIsLoading ] = useState(false);
  const [ error, setError ] = useState('');

  const session = useContext(SessionContext);
  const history = useHistory();

  const onLoginSuccess = () => {
    setIsLoading(false);
    closeModal();
    if (props.destPath) {
      history.push(props.destPath);
    }
  }

  const onLoginFail = (errorMessage) => {
    setIsLoading(false);
    setError(errorMessage);
  }

  const onFacebookLogin = (response) => {
    setIsLoading(true);
    session.processFacebookResponse(response.authResponse, onLoginSuccess, onLoginFail);
  }
  
  window.onFacebookLogin = (response, event) => {
    onFacebookLogin(response);
    return false;
  }

  const onGoogleLogin = (guser) => {
    setIsLoading(true);
    session.processGoogleToken(guser.getAuthResponse(true).id_token, onLoginSuccess, onLoginFail)
  }
  
  window.onGoogleLogin = (response, event) => {
    onGoogleLogin(response);
    return false;
  }

  const closeModal = () => {
    if (props.onShowChange !== undefined) {
      props.onShowChange(false);
    }
  }

  const onLogin = (e) => {
    e.preventDefault();
    if (props.onShowLogin !== undefined) {
      props.onShowLogin();
    }
  }
  const onCreate = (e) => {
    e.preventDefault();
    if (props.onShowCreate !== undefined) {
      props.onShowCreate();
    }
  }

  useEffect(() => {
    props.show && window.FB && window.FB.XFBML && window.FB.XFBML.parse();
    props.show && window.gapi && window.gapi.signin2 &&
      window.gapi.signin2.render('google-signin', {
        scope: 'profile email',
        width: 240,
        longtitle: true,
        onsuccess: onGoogleLogin,
        });
  }, [props.show]);  // eslint-disable-line react-hooks/exhaustive-deps
  
  return (

<Modal show={props.show} onHide={closeModal} backdrop="static">
  <Form>
    <Modal.Header closeButton>
      <Modal.Title>Login / Create Account</Modal.Title>
    </Modal.Header>

    <Modal.Body>
      {isLoading && (
        <div className="form-group">
          <div className="row justify-content-center">
            <Image className="login-loading" src="/img/loading.gif" />
          </div>
        </div>
      )}
      {error && (
        <div className="form-group">
          <div className="row justify-content-center">
            {error}
          </div>
        </div>
      )}
{false &&      <div className="form-group">
        <div className="row justify-content-center">
          <div
             className="fb-login-button" data-size="large" data-button-type="login_with" data-scope="public_profile,email"
             data-onlogin="onFacebookLogin" data-layout="default" data-auto-logout-link="false" data-use-continue-as="false" data-width="240">
          </div>
        </div>
      </div>}
      <div className="form-group">
        <div className="row justify-content-center">
          <div className="g-signin2" id="google-signin"></div>
        </div>
      </div>
      <div className="form-group login-form-spacer">
      </div>
      <div className="form-group">
        <div className="row justify-content-center">
          <Button onClick={onLogin} variant="primary" className="login-form-mainbtn">Login with Email</Button>
        </div>
        <div className="row justify-content-center">
          or
        </div>
        <div className="row justify-content-center">
          <Button onClick={onCreate} variant="primary" className="login-form-mainbtn">Create Account</Button>
        </div>
      </div>
    </Modal.Body>
  </Form>
</Modal>

);
}

