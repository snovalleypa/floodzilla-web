import React, { useState, useContext } from "react";
import Alert from "react-bootstrap/Alert";
import Button from "react-bootstrap/Button";
import Form from "react-bootstrap/Form";
import Image from "react-bootstrap/Image";
import Modal from "react-bootstrap/Modal";
import { SessionContext } from "./SessionContext";

export default function ForgotPasswordModal(props) {

  const session = useContext(SessionContext);

  const [ email, setEmail ] = useState('');
  const [ sentEmail, setSentEmail ] = useState(false);
  const [ isLoading, setIsLoading ] = useState(false);
  const [ error, setError ] = useState('');

  var siteKey = window.GoogleInvisibleCaptchaSiteKey;

  const closeModal = () => {
    setError('');
    if (props.onShowChange !== undefined) {
      props.onShowChange(false);
    }
  }

  const onForgotPasswordSuccess = () => {
    setIsLoading(false);
    setSentEmail(true);
  }

  const onForgotPasswordFail = (errorMessage) => {
    setIsLoading(false);
    setError(errorMessage);
  }

  const handleSubmit = (event) => {
      event.preventDefault();
      window.grecaptcha.render(document.getElementById('recaptchaContainer'), {
        sitekey: siteKey,
        }, true);
      window.grecaptcha.execute();
  }

  const onRecaptcha = (captchaToken) => {
      setError('');
      setIsLoading(true);
      session.forgotPassword(email, captchaToken, onForgotPasswordSuccess, onForgotPasswordFail);
  }

  const handleEmailChange = (event) => {
    setEmail(event.target.value);
  }


  window.forgotrecaptcha = (response) => {
    onRecaptcha(response);
  }

  return (

<Modal show={props.show} onHide={closeModal} backdrop="static">
  <Form onSubmit={handleSubmit}>
    <Modal.Header closeButton>
      <Modal.Title>Forgot Password</Modal.Title>
    </Modal.Header>

    <Modal.Body>
      <div
         id='recaptchaContainer'
         className="g-recaptcha"
         data-sitekey={siteKey}
         data-callback="forgotrecaptcha"
         data-size="invisible"
      >
      </div>
      {error &&
        <div className="form-group">
          <Alert variant="danger">{error}</Alert>
        </div>
      }
      <div className="form-group">
        {sentEmail
        ?<span>An email has been sent to {email} with instructions for
               resetting your password. If there is no matching account 
               no email will be sent.</span>
        :<span>Enter your email address.  We will send you a link to allow you to reset your password.</span>
        }
      </div>
      <div className="form-group">
        <label className="text-info">Email:</label><br/>
        <input spellCheck="false" type="text" name="email" className="form-control" value={email} onChange={handleEmailChange} />
      </div>
    </Modal.Body>

    <Modal.Footer>
      <div className="form-group">
      {isLoading
       ? <Image className="login-loading" src="/img/loading.gif" />
       : <Button type="submit" variant="primary">Send Email</Button>
      }
      </div>
    </Modal.Footer>
  </Form>
</Modal>

);
}

