import React, { useContext, useState } from "react";
import Button from "react-bootstrap/Button";
import Form from "react-bootstrap/Form";
import Modal from "react-bootstrap/Modal";
import { SessionContext } from "./SessionContext";
import "../style/Login.css";

export default function VerifyEmailModal(props) {

  const session = useContext(SessionContext);

  const [ doneView, setDoneView ] = useState(false);

  const onVerify = () =>{
    session.sendVerificationEmail();
    setDoneView(true);
  }

  const closeModal = () => {
    if (props.onShowChange !== undefined) {
      props.onShowChange(false);
    }
  }

  return (

<Modal show={props.show} onHide={closeModal} backdrop="static">
  <Form>
    <Modal.Header closeButton>
      <Modal.Title>Verify Email Address</Modal.Title>
    </Modal.Header>

    <Modal.Body>
      <div className="form-group">
        { doneView
        ? <div>An email has been sent to {session.getUsername()}.  Please click on the link in that email to verify your email address.</div>
        : <div>We will send you an email with a link which will verify that {session.getUsername()} is really your email address.</div>
        }
      </div>
    </Modal.Body>
    <Modal.Footer className="justify-content-center">
      <div className="form-group">
        { doneView
         ? <Button onClick={closeModal} variant="primary" className="login-form-mainbtn">Close</Button>
         : <Button onClick={onVerify} variant="primary" className="login-form-mainbtn">Verify Email Address</Button>

        }
      </div>
    </Modal.Footer>
  </Form>
</Modal>

);
}

