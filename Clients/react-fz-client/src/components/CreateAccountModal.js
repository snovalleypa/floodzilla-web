import React, { useState, useContext } from "react";
import Alert from "react-bootstrap/Alert";
import Button from "react-bootstrap/Button";
import Form from "react-bootstrap/Form";
import Image from "react-bootstrap/Image";
import Modal from "react-bootstrap/Modal";
import LoginField from "./LoginField";
import LoginSingleRowField from "./LoginSingleRowField";
import Constants from "../constants";
import { SessionContext } from "./SessionContext";

export default function CreateAccountModal(props) {

  const session = useContext(SessionContext);

  const [ firstName, setFirstName ] = useState('');
  const [ lastName, setLastName ] = useState('');
  const [ email, setEmail ] = useState('');
  const [ password, setPassword ] = useState('');
  const [ confirmPassword, setConfirmPassword ] = useState('');
  const [ rememberMe, setRememberMe ] = useState(true);
  const [ passwordError, setPasswordError ] = useState('');
  const [ passwordMismatch, setPasswordMismatch ] = useState('');
  const [ isLoading, setIsLoading ] = useState(false);
  const [ renderedCaptcha, setRenderedCaptcha ] = useState(false);
  const [ error, setError ] = useState('');
  const [ isFormValid, setIsFormValid ] = useState(false);

  const closeModal = () => {
    setError('');
    if (props.onShowChange !== undefined) {
      props.onShowChange(false);
    }
  }

  const onCreateAccountSuccess = () => {
    window.grecaptcha.reset();
    setIsLoading(false);
    closeModal();
  }

  const onCreateAccountFail = (errorMessage) => {
    window.grecaptcha.reset();
    setIsLoading(false);
    setError(errorMessage);
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
    session.createAccount(firstName, lastName, email, /*phone*/ "", password, rememberMe, captchaToken, onCreateAccountSuccess, onCreateAccountFail);
  }

  const handleFirstNameChange = (event) => {
    setFirstName(event.target.value);
    validateForm();
  }
  const handleLastNameChange = (event) => {
    setLastName(event.target.value);
    validateForm();
  }
  const handleEmailChange = (event) => {
    setEmail(event.target.value);
    validateForm();
  }
  const handlePasswordChange = (event) => {
    setPassword(event.target.value);
  }
  const handleConfirmPasswordChange = (event) => {
    setConfirmPassword(event.target.value);
  }
  const handleRememberMeChange = (event) => {
    setRememberMe(event.target.checked);
  }
  const handlePasswordBlur = (event) => {
    validatePassword();
    validateForm();
  }
  const handleConfirmPasswordBlur = (event) => {
    validatePassword();
    validateForm();
  }

  const validateForm = () => {
    var valid = true;
    if (firstName.length === 0 ||
        lastName.length === 0 ||
        email.length === 0 ||
        password.length === 0 ||
        confirmPassword.length === 0) {
      valid = false;
    }
    setIsFormValid(valid);
  }

  const validatePassword = () => {
    if (password.length < Constants.PASSWORD_MIN_LENGTH) {
      setPasswordError('Password must be at least ' + Constants.PASSWORD_MIN_LENGTH + ' characters.');
    } else {
      setPasswordError('');
    }
    if (confirmPassword.length > 0 && password !== confirmPassword) {
      setPasswordMismatch('Passwords do not match');
    } else {
      setPasswordMismatch('');
    }
  }

  window.createrecaptcha = (response) => {
    onRecaptcha(response);
  }

  return (

<Modal show={props.show} onHide={closeModal} backdrop="static">
  <Form onSubmit={handleSubmit}>
    <Modal.Header closeButton>
      <Modal.Title>Create Account</Modal.Title>
    </Modal.Header>

    <Modal.Body>
      <div
         id='recaptchaContainer'
         className="g-recaptcha"
         data-sitekey={window.GoogleInvisibleCaptchaSiteKey}
         data-callback="createrecaptcha"
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
             label={<>First Name:</>}
             ctrl={<input spellCheck="false" type="text" name="firstName" className="form-control login-form-control" value={firstName} onChange={handleFirstNameChange} />}
           />
          <LoginField
             label={<>Last Name:</>}
             ctrl={<input spellCheck="false" type="text" name="lastName" className="form-control login-form-control" value={lastName} onChange={handleLastNameChange} />}
           />
          <LoginField
             label={<>Email:</>}
             ctrl={<input spellCheck="false" type="text" name="email" className="form-control login-form-control" value={email} onChange={handleEmailChange} />}
           />
        </div>
        <div className="form-group">
          <LoginField
             label={<>Password:</>}
             ctrl={<input type="password" name="password" className="form-control login-form-control" value={password} onChange={handlePasswordChange} onBlur={handlePasswordBlur}/>}
           />
          {passwordError && <LoginField
             label={<></>}
             ctrl={<Alert variant="danger">{passwordError}</Alert>}
           />}
          <LoginField
             label={<>Confirm Password:</>}
             ctrl={<input type="password" name="confirmPassword" className="form-control login-form-control" value={confirmPassword} onChange={handleConfirmPasswordChange} onBlur={handleConfirmPasswordBlur} />}
           />
          {passwordMismatch && <LoginField
             label={<></>}
             ctrl={<Alert variant="danger">{passwordMismatch}</Alert>}
           />}
        </div>
        <div className="form-group">
          <LoginSingleRowField
             label={<>Remember Me:&nbsp;&nbsp;</>}
             labelFor="rememberMe"
             ctrl={<input type="checkbox" id="rememberMe" className="login-form-check" checked={rememberMe} onChange={handleRememberMeChange} />}
          />
        </div>
      </div>
    </Modal.Body>
    <Modal.Footer className="justify-content-center">
      <div className="form-group">
        {isLoading
         ? <Image className="login-loading" src="/img/loading.gif" />
         : <Button disabled={(passwordError || passwordMismatch || !isFormValid)} type="submit" variant="primary">Create Account</Button>
        }
      </div>
    </Modal.Footer>
  </Form>
</Modal>

);
}
