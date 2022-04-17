import React, { useContext, useState } from "react";
import { Link, useHistory } from "react-router-dom";
import Alert from "react-bootstrap/Alert";
import Button from "react-bootstrap/Button";
import Form from "react-bootstrap/Form";
import Image from "react-bootstrap/Image";
import Modal from "react-bootstrap/Modal";
import LoginField from "./LoginField";
import LoginSingleRowField from "./LoginSingleRowField";
import { SessionContext } from "./SessionContext";

export default function LoginModal(props) {

  const session = useContext(SessionContext);
  const history = useHistory();

  const [ email, setEmail ] = useState('');
  const [ password, setPassword ] = useState('');
  const [ rememberMe, setRememberMe ] = useState(true);
  const [ isLoading, setIsLoading ] = useState(false);
  const [ renderedCaptcha, setRenderedCaptcha ] = useState(false);
  const [ error, setError ] = useState('');

  const closeModal = () => {
    setError('');
    if (props.onShowChange !== undefined) {
      props.onShowChange(false);
    }
  }

  const onLoginSuccess = () => {
    window.grecaptcha.reset();
    setIsLoading(false);
    closeModal();
    if (props.destPath) {
      history.push(props.destPath);
    }
  }

  const onLoginFail = (errorMessage) => {
    window.grecaptcha.reset();
    setIsLoading(false);
    setError(errorMessage);
  }

  const handleForgotPassword = (event) => {
    event.preventDefault();

    setError('');
    if (props.onForgotPassword !== undefined) {
      props.onForgotPassword();
    }
  }

  const handleSubmit = (event) => {
    event.preventDefault();
    setError('');
    setIsLoading(true);
    if (!renderedCaptcha) {
      window.grecaptcha.render(document.getElementById('recaptchaContainer'), {
        sitekey: window.GoogleInvisibleCaptchaSiteKey,
      }, true);
      setRenderedCaptcha(true);
    }
    window.grecaptcha.execute();
  }

  const onRecaptcha = (captchaToken) => {
    setError('');
    setIsLoading(true);
    session.login(email, password, rememberMe, captchaToken, onLoginSuccess, onLoginFail);
  }

  const handleEmailChange = (event) => {
    setEmail(event.target.value);
  }
  const handlePasswordChange = (event) => {
    setPassword(event.target.value);
  }
  const handleRememberMeChange = (event) => {
    setRememberMe(event.target.checked);
  }

  window.loginrecaptcha = (response) => {
    onRecaptcha(response);
  }

  return (

<Modal show={props.show} onHide={closeModal} backdrop="static">
  <Form onSubmit={handleSubmit}>
    <Modal.Header closeButton>
      <Modal.Title>Log In</Modal.Title>
    </Modal.Header>

    <Modal.Body>
      <div
         id='recaptchaContainer'
         className="g-recaptcha"
         data-sitekey={window.GoogleInvisibleCaptchaSiteKey}
         data-callback="loginrecaptcha"
         data-size="invisible"
      >
      </div>
      {error &&
        <div className="form-group">
          <Alert variant="danger">{error}</Alert>
        </div>
      }
      <div className="container">
        <div className="form-group">
          <LoginField
             label={<>Email:</>}
             ctrl={<input spellCheck="false" type="text" name="email" className="form-control login-form-control" value={email} onChange={handleEmailChange} />}
           />
          <LoginField
             label={<>Password:</>}
             ctrl={<input type="password" name="password" className="form-control login-form-control" value={password} onChange={handlePasswordChange}/>}
           />
          <LoginSingleRowField
             label={<></>}
             ctrl={<Link to="/" onClick={handleForgotPassword}>Forgot password</Link>}
           />
          <LoginSingleRowField
             label={<>Remember Me:&nbsp;&nbsp;</>}
             labelFor="rememberMe"
             ctrl={<input type="checkbox" id="loginModalRememberMe" className="login-form-check" checked={rememberMe} onChange={handleRememberMeChange} />}
          />
        </div>
      </div>
    </Modal.Body>

    <Modal.Footer className="justify-content-center">
      <div className="form-group">
        {isLoading
         ? <Image className="login-loading" src="/img/loading.gif" />
         : <Button type="submit" variant="primary">Login</Button>
        }
      </div>
    </Modal.Footer>
  </Form>
</Modal>

);
}

