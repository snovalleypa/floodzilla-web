import React, { useContext, useEffect, useState } from "react";
import Alert from "react-bootstrap/Alert";
import Button from "react-bootstrap/Button";
import Form from "react-bootstrap/Form";
import Modal from "react-bootstrap/Modal";
import LoginField from "./LoginField";
import { SessionContext } from "./SessionContext";

export default function PhoneModal(props) {

  const session = useContext(SessionContext);

  const BUTTON_SENDCODE = 'Send Verification Code';
  const BUTTON_RESENDCODE = 'Resend Verification Code';

  const [ phone, setPhone ] = useState('');
  const [ code, setCode ] = useState('');
  const [ origPhone, setOrigPhone ] = useState('');

  const [ error, setError ] = useState('');
  const [ sendCodeText, setSendCodeText ] = useState('');
  const [ sentVerify, setSentVerify ] = useState(false);
  const [ sendCodeEnabled, setSendCodeEnabled ] = useState(false);
  const [ verifyCodeEnabled, setVerifyCodeEnabled ] = useState(true);

  // on mount/unmount
  useEffect(() => {
    const orig = session.getPhone();
    setOrigPhone(orig);
    setPhone(orig);
    setSendCodeEnabled(orig && orig.length > 0);
    setSendCodeText(BUTTON_SENDCODE);
  }, [session]);

  const closeModal = () => {
    if (props.onShowChange !== undefined) {
      props.onShowChange(false);
    }
  }

  const handlePhoneChange = (event) => {
    setPhone(event.target.value);
    setSendCodeEnabled(event.target.value && event.target.value.length > 0);
  }
  const handleCodeChange = (event) => {
    setCode(event.target.value);
    setVerifyCodeEnabled(event.target.value && event.target.value.length === 6);
  }

  const onSendVerifySuccess = () => {
    setSentVerify(true);
    setSendCodeText(BUTTON_RESENDCODE);
  }

  const onSendVerifyFailure = (status, message) => {
    setSentVerify(false);
    setSendCodeText(BUTTON_SENDCODE);
    if (status === 400) {
      setSendCodeEnabled(false);
      setError('This phone number is invalid.  Please enter a valid number');
    }
    else {
      setError(message);
    }
  }

  const onSendCodeClick = (event) => {
    session.sendPhoneVerificationSms(phone, onSendVerifySuccess, onSendVerifyFailure);
  }

  const onVerifyCodeClick = (event) => {
    session.verifyPhoneCode(phone,
                            code,
                            () => {
                              closeModal();
                            },
                            (status, message) => {
                              setError(message);
                            });
  }

  window.phonerecaptcha = (response) => {
//    onRecaptcha(response);
  }

  return (

<Modal show={props.show} onHide={closeModal} backdrop="static">
  <Form>
    <Modal.Header closeButton>
      <Modal.Title>{((origPhone && origPhone.length > 0) ? "Change " : "Verify ")}Phone Number</Modal.Title>
    </Modal.Header>

    <Modal.Body>
      <div
         id='recaptchaContainer'
         className="g-recaptcha"
         data-sitekey={window.GoogleInvisibleCaptchaSiteKey}
         data-callback="phonerecaptcha"
         data-size="invisible"
      >
      </div>
      <div className="container">
        <div className="form-group">
          <div className="login-form-text">
            Please enter a phone number where Floodzilla can
            send SMS Alerts.  Floodzilla will send you an SMS
            with a verification code.
          </div>
          {error &&
            <div className="form-group">
              <Alert variant="danger">{error}</Alert>
            </div>
          }
          <LoginField
             label={<>Phone Number:</>}
             ctrl={<input spellCheck="false" type="text" name="phone" className="form-control login-form-control" value={phone} onChange={handlePhoneChange} />}
             />
          <div className="row justify-content-center">
            <Button variant="primary" id="sendCode" disabled={!sendCodeEnabled} onClick={onSendCodeClick}>{sendCodeText}</Button>
          </div>
          {sentVerify && <>
            <div className="login-form-text">
              A 6-digit verification code has been sent to {phone}.  Please enter that code:
            </div>
            <LoginField
               label={<>Verification Code:</>}
               ctrl={<input spellCheck="false" type="text" name="code" className="form-control login-form-control" value={code} onChange={handleCodeChange} />}
               />
            <div className="row justify-content-center">
              <Button variant="primary" id="verifyCode" disabled={!verifyCodeEnabled} onClick={onVerifyCodeClick}>Verify Phone Number</Button>
            </div>
          </>}
        </div>
      </div>
    </Modal.Body>
  </Form>
</Modal>

);
}

